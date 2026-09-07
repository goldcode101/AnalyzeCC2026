using CreditCardAnalyzer.Models;

namespace CreditCardAnalyzer.Services
{
    public class AnalysisDataService
    {
        private readonly TransactionService _transactionService;
        private readonly IConfiguration _configuration;
        private bool _isLoaded;
        private List<Transaction> _transactions = new();
        private AnalysisSummary _summary = new();
        private List<CategoryRule> _categoryRules = new();
        private List<string> _excludedCreditKeywords = new();

        public AnalysisDataService(
            TransactionService transactionService,
            IConfiguration configuration)
        {
            _transactionService = transactionService;
            _configuration = configuration;
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

            _categoryRules = _configuration
                .GetSection("CustomCategoryRules")
                .Get<List<CategoryRule>>() ?? new List<CategoryRule>();
            _excludedCreditKeywords = _configuration
                .GetSection("ExcludedCreditKeywords")
                .Get<List<string>>() ?? new List<string>();
            _transactions = TransactionService.ApplyCustomCategoryRules(
                _transactionService.ImportTransactions(),
                _categoryRules);
            _summary = TransactionService.BuildAnalysisSummary(
                _transactions,
                _categoryRules,
                _excludedCreditKeywords);
            _isLoaded = true;
        }
    }
}