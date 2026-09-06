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
        private readonly string _csvPath;

        public TransactionService(string csvPath)
        {
            _csvPath = csvPath;
        }

        public List<Transaction> ImportTransactions()
        {
            using (var reader = new StreamReader(_csvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<TransactionMap>();
                return csv.GetRecords<Transaction>().ToList();
            }
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
        IEnumerable<CategoryRule>? rules = null)
        {
            var summary = new AnalysisSummary();
            var categoryRules = rules?.ToList() ?? new List<CategoryRule>();

            foreach (var transaction in ApplyCustomCategoryRules(transactions, categoryRules).Where(t => t.Debit > 0))
            {
                var monthKey = transaction.TransactionDate.ToString("yyyy-MM");
                var categoryKey = string.IsNullOrWhiteSpace(transaction.EffectiveCategory)
                    ? transaction.Category
                    : transaction.EffectiveCategory;

                AddToGroup(summary.MonthlyTotals, monthKey, transaction);
                AddToGroup(summary.ByMonth, monthKey, transaction);
                AddToNestedGroup(summary.ByMonthAndCategory, monthKey, categoryKey, transaction);
                AddToNestedGroup(summary.ByMonthAndMerchant, monthKey, transaction.Description, transaction);
                AddToGroup(summary.ByCategory, categoryKey, transaction);
                AddToGroup(summary.ByMerchant, transaction.Description, transaction);
            }

            return summary;
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
