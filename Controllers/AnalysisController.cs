using CreditCardAnalyzer.Models;
using CreditCardAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;

namespace CreditCardAnalyzer.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly TransactionService _transactionService;
        private readonly IConfiguration _configuration;

        public AnalysisController(TransactionService transactionService, IConfiguration configuration)
        {
            _transactionService = transactionService;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var categoryRules = GetCategoryRules();
            var (enrichedTransactions, summary) = LoadAnalysis(categoryRules);

            var viewModel = new AnalysisViewModel
            {
                Transactions = enrichedTransactions,
                Summary = summary,
                CategoryRules = categoryRules
            };

            return View(viewModel);
        }

        public IActionResult Merchants()
        {
            var (transactions, summary) = LoadAnalysis();

            return View(new MerchantExplorerViewModel
            {
                Transactions = transactions,
                Summary = summary
            });
        }

        public IActionResult Raw()
        {
            var categoryRules = GetCategoryRules();
            var transactions = TransactionService.ApplyCustomCategoryRules(
                _transactionService.ImportTransactions(),
                categoryRules);

            return View(new RawTransactionsViewModel
            {
                Transactions = transactions,
                ExcludedCreditKeywords = GetExcludedCreditKeywords()
            });
        }

        public IActionResult Month(string month)
        {
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return NotFound();
            }

            var categoryRules = GetCategoryRules();
            var excludedCreditKeywords = GetExcludedCreditKeywords();
            var transactions = TransactionService.ApplyCustomCategoryRules(
                _transactionService.ImportTransactions(),
                categoryRules);
            var summary = TransactionService.BuildAnalysisSummary(
                transactions,
                categoryRules,
                excludedCreditKeywords);

            if (!summary.ByMonthAndMerchant.ContainsKey(month))
            {
                return NotFound();
            }

            var spending = summary.MonthlyTotals.TryGetValue(month, out var monthlySpending)
                ? monthlySpending
                : new TransactionGroup { Name = month };

            var monthTransactions = transactions
                .Where(transaction => transaction.TransactionDate.ToString("yyyy-MM") == month)
                .ToList();
            var excludedCredits = monthTransactions
                .Where(transaction => TransactionService.IsExcludedCreditPayment(transaction, excludedCreditKeywords))
                .Sum(transaction => transaction.Credit);
            var qualifyingCredits = monthTransactions
                .Where(transaction => transaction.Credit > 0 &&
                    !TransactionService.IsExcludedCreditPayment(transaction, excludedCreditKeywords))
                .Sum(transaction => transaction.Credit);
            var categories = summary.ByMonthAndCategory.TryGetValue(month, out var categoryGroups)
                ? categoryGroups
                : new Dictionary<string, TransactionGroup>();

            return View(new MonthlySummaryViewModel
            {
                MonthKey = month,
                Spending = spending,
                Categories = categories,
                Transactions = monthTransactions,
                QualifyingCredits = qualifyingCredits,
                ExcludedCredits = excludedCredits
            });
        }

        private (List<Transaction> Transactions, AnalysisSummary Summary) LoadAnalysis(
            List<CategoryRule>? categoryRules = null)
        {
            categoryRules ??= GetCategoryRules();
            var transactions = TransactionService.ApplyCustomCategoryRules(
                _transactionService.ImportTransactions(),
                categoryRules);
            var summary = TransactionService.BuildAnalysisSummary(
                transactions,
                categoryRules,
                GetExcludedCreditKeywords());

            return (transactions, summary);
        }

        private List<CategoryRule> GetCategoryRules()
        {
            return _configuration
                .GetSection("CustomCategoryRules")
                .Get<List<CategoryRule>>() ?? new List<CategoryRule>();
        }

        private List<string> GetExcludedCreditKeywords()
        {
            return _configuration
                .GetSection("ExcludedCreditKeywords")
                .Get<List<string>>() ?? new List<string>();
        }
    }
}
