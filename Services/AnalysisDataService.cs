using CreditCardAnalyzer.Models;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CreditCardAnalyzer.Services
{
    public class AnalysisDataService
    {
        private readonly TransactionService _transactionService;
        private readonly AnalysisOptions _options;
        private bool _isLoaded;
        private List<Transaction> _transactions = new();
        private AnalysisSummary _summary = new();
        private List<CategoryRule> _categoryRules = new();
        private List<string> _excludedCreditKeywords = new();

        public AnalysisDataService(
            TransactionService transactionService,
            IOptions<AnalysisOptions> options)
        {
            _transactionService = transactionService;
            _options = options.Value;
        }

        public List<Transaction> Transactions
        {
            get
            {
                EnsureLoaded();
                return _transactions;
            }
        }

        public AnalysisSummary Summary
        {
            get
            {
                EnsureLoaded();
                return _summary;
            }
        }

        public List<CategoryRule> CategoryRules
        {
            get
            {
                EnsureLoaded();
                return _categoryRules;
            }
        }

        public List<string> ExcludedCreditKeywords
        {
            get
            {
                EnsureLoaded();
                return _excludedCreditKeywords;
            }
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            _categoryRules = _options.CustomCategoryRules;
            _excludedCreditKeywords = _options.ExcludedCreditKeywords;
            _transactions = TransactionService.ApplyCustomCategoryRules(
                _transactionService.ImportTransactions(),
                _categoryRules);
            _summary = TransactionService.BuildAnalysisSummary(
                _transactions,
                _categoryRules,
                _excludedCreditKeywords);
            _isLoaded = true;
        }

        public MonthlySummaryViewModel? GetMonthlySummary(string month)
        {
            EnsureLoaded();

            if (!Summary.ByMonthAndMerchant.ContainsKey(month))
            {
                return null;
            }

            var spending = Summary.MonthlyTotals.TryGetValue(month, out var monthlySpending)
                ? monthlySpending
                : new TransactionGroup { Name = month };
            var monthTransactions = Transactions
                .Where(transaction => transaction.TransactionDate.ToString("yyyy-MM") == month)
                .ToList();
            var excludedCredits = monthTransactions
                .Where(transaction => TransactionService.IsExcludedCreditPayment(transaction, ExcludedCreditKeywords))
                .Sum(transaction => transaction.Credit);
            var qualifyingCredits = monthTransactions
                .Where(transaction => transaction.Credit > 0 &&
                    !TransactionService.IsExcludedCreditPayment(transaction, ExcludedCreditKeywords))
                .Sum(transaction => transaction.Credit);
            var categories = Summary.ByMonthAndCategory.TryGetValue(month, out var categoryGroups)
                ? categoryGroups
                : new Dictionary<string, TransactionGroup>();

            return new MonthlySummaryViewModel
            {
                MonthKey = month,
                Spending = spending,
                Categories = categories,
                Transactions = monthTransactions,
                QualifyingCredits = qualifyingCredits,
                ExcludedCredits = excludedCredits
            };
        }
    }
}