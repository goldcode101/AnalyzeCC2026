using CreditCardAnalyzer.Models;
using CreditCardAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;

namespace CreditCardAnalyzer.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly AnalysisDataService _analysisDataService;

        public AnalysisController(
            AnalysisDataService analysisDataService)
        {
            _analysisDataService = analysisDataService;
        }

        public IActionResult Index()
        {
            var viewModel = new AnalysisViewModel
            {
                Transactions = _analysisDataService.Transactions,
                Summary = _analysisDataService.Summary,
                CategoryRules = _analysisDataService.CategoryRules
            };

            return View(viewModel);
        }

        public IActionResult Merchants()
        {
            return View(new MerchantExplorerViewModel
            {
                Transactions = _analysisDataService.Transactions,
                Summary = _analysisDataService.Summary
            });
        }

        public IActionResult Raw()
        {
            return View(new RawTransactionsViewModel
            {
                Transactions = _analysisDataService.Transactions,
                ExcludedCreditKeywords = _analysisDataService.ExcludedCreditKeywords
            });
        }

        public IActionResult Month(string month)
        {
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return NotFound();
            }

            var transactions = _analysisDataService.Transactions;
            var summary = _analysisDataService.Summary;
            var excludedCreditKeywords = _analysisDataService.ExcludedCreditKeywords;

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

    }
}
