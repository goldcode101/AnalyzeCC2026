using CsvHelper;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using CreditCardAnalyzer.Models;
using CsvHelper.Configuration;

namespace CreditCardAnalyzer.Services
{
    public class TransactionService
    {
        private readonly List<string> _csvPaths;

        public TransactionService(IEnumerable<string> csvPaths)
        {
            _csvPaths = csvPaths.ToList();
            if (_csvPaths.Count == 0)
            {
                throw new ArgumentException("At least one transaction CSV file must be available.", nameof(csvPaths));
            }
        }

        public static List<string> DiscoverTransactionFiles(
            string dataDirectory,
            IEnumerable<string>? excludedFileNames = null)
        {
            var excludedFiles = new HashSet<string>(
                excludedFileNames ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            return Directory.GetFiles(dataDirectory, "*.csv", SearchOption.TopDirectoryOnly)
                .Where(path => !excludedFiles.Contains(Path.GetFileName(path)))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<Transaction> ImportTransactions()
        {
            var transactions = new List<Transaction>();
            var seenTransactions = new HashSet<TransactionIdentity>();

            foreach (var csvPath in _csvPaths)
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TransactionMap>();

                foreach (var transaction in csv.GetRecords<Transaction>())
                {
                    transaction.SourceFile = Path.GetFileName(csvPath);
                    transaction.IsDuplicate = !seenTransactions.Add(TransactionIdentity.From(transaction));
                    transactions.Add(transaction);
                }
            }

            return transactions;
        }

        public static List<Transaction> ApplyCustomCategoryRules(IEnumerable<Transaction> transactions,
        IEnumerable<CategoryRule>? rules = null)
        {
            var categoryRules = rules?.ToList() ?? new List<CategoryRule>();
            var results = new List<Transaction>();

            foreach (var transaction in transactions)
            {
                var effectiveCategory = ResolveCustomCategory(transaction.Description, categoryRules);
                transaction.EffectiveCategory = string.IsNullOrWhiteSpace(effectiveCategory)
                    ? transaction.Category
                    : effectiveCategory;
                results.Add(transaction);
            }

            return results;
        }

        public static AnalysisSummary BuildAnalysisSummary(IEnumerable<Transaction> transactions,
        IEnumerable<CategoryRule>? rules = null,
        IEnumerable<string>? excludedCreditKeywords = null)
        {
            var summary = new AnalysisSummary();
            var categoryRules = rules?.ToList() ?? new List<CategoryRule>();
            var excludedKeywords = excludedCreditKeywords?.ToList() ?? new List<string>();

            var enrichedTransactions = ApplyCustomCategoryRules(transactions, categoryRules).ToList();

            foreach (var transaction in enrichedTransactions.Where(t => IsIncludedInSpendingTotals(t, excludedKeywords)))
            {
                var monthKey = transaction.TransactionDate.ToString("yyyy-MM");
                var categoryKey = string.IsNullOrWhiteSpace(transaction.EffectiveCategory)
                    ? transaction.Category
                    : transaction.EffectiveCategory;

                AddToGroup(summary.MonthlyTotals, monthKey, transaction);
                AddToGroup(summary.ByMonth, monthKey, transaction);
                AddToNestedGroup(summary.ByMonthAndCategory, monthKey, categoryKey, transaction);
                AddToGroup(summary.ByCategory, categoryKey, transaction);
            }

            foreach (var transaction in enrichedTransactions.Where(t => t.Debit > 0 || t.Credit > 0))
            {
                var monthKey = transaction.TransactionDate.ToString("yyyy-MM");
                AddToNestedGroup(summary.ByMonthAndMerchant, monthKey, transaction.Description, transaction);
                AddToGroup(summary.ByMerchant, transaction.Description, transaction);
            }

            return summary;
        }

        private static bool IsIncludedInSpendingTotals(
            Transaction transaction,
            IEnumerable<string> excludedCreditKeywords)
        {
            if (transaction.Debit > 0)
            {
                return true;
            }

            if (transaction.Credit <= 0)
            {
                return false;
            }

            return !IsExcludedCreditPayment(transaction, excludedCreditKeywords);
        }

        public static bool IsExcludedCreditPayment(
            Transaction transaction,
            IEnumerable<string> excludedCreditKeywords)
        {
            return transaction.Credit > 0 && excludedCreditKeywords.Any(keyword =>
                !string.IsNullOrWhiteSpace(keyword) &&
                (transaction.Description ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolveCustomCategory(string merchantName,
        IEnumerable<CategoryRule> categoryRules)
        {
            var normalizedMerchant = merchantName ?? string.Empty;

            foreach (var rule in categoryRules)
            {
                foreach (var keyword in rule.Keywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword) &&
                        normalizedMerchant.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return rule.Name;
                    }
                }
            }

            return string.Empty;
        }

        private static void AddToGroup(Dictionary<string,
        TransactionGroup> groups,
        string key,
        Transaction transaction)
        {
            if (!groups.TryGetValue(key, out var group))
            {
                group = new TransactionGroup { Name = key };
                groups[key] = group;
            }

            group.Add(transaction);
        }

        private static void AddToNestedGroup(
            Dictionary<string, Dictionary<string, TransactionGroup>> groups,
            string outerKey,
            string innerKey,
            Transaction transaction)
        {
            if (!groups.TryGetValue(outerKey, out var categoryGroups))
            {
                categoryGroups = new Dictionary<string, TransactionGroup>();
                groups[outerKey] = categoryGroups;
            }

            AddToGroup(categoryGroups, innerKey, transaction);
        }

        private readonly record struct TransactionIdentity(
            DateTime TransactionDate,
            DateTime PostedDate,
            string CardNo,
            string Description,
            string Category,
            decimal Debit,
            decimal Credit)
        {
            public static TransactionIdentity From(Transaction transaction) => new(
                transaction.TransactionDate,
                transaction.PostedDate,
                transaction.CardNo,
                transaction.Description,
                transaction.Category,
                transaction.Debit,
                transaction.Credit);
        }

        private class TransactionMap : ClassMap<Transaction>
        {
            public TransactionMap()
            {
                Map(m => m.TransactionDate).Name("Transaction Date");
                Map(m => m.PostedDate).Name("Posted Date");
                Map(m => m.CardNo).Name("Card No.");
                Map(m => m.Description).Name("Description");
                Map(m => m.Category).Name("Category");

                // Treat empty fields as 0 using Convert to handle nulls
                Map(m => m.Debit)
                    .Name("Debit")
                    .Convert(args => args.Row.GetField<decimal?>("Debit") ?? 0m);

                Map(m => m.Credit)
                    .Name("Credit")
                    .Convert(args => args.Row.GetField<decimal?>("Credit") ?? 0m);
            }
        }
    }
}
