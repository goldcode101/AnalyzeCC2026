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
                Transactions = _analysisDataService.Transactions
                    .Where(transaction => !transaction.IsDuplicate)
                    .ToList(),
                Summary = _analysisDataService.Summary,
                CategoryRules = _analysisDataService.CategoryRules,
                TransactionHighlightThreshold = _analysisDataService.TransactionHighlightThreshold
            };

            return View(viewModel);
        }

        public IActionResult Merchants()
        {
            return View(new MerchantExplorerViewModel
            {
                Transactions = _analysisDataService.Transactions
                    .Where(transaction => !transaction.IsDuplicate)
                    .ToList(),
                Summary = _analysisDataService.Summary,
                TransactionHighlightThreshold = _analysisDataService.TransactionHighlightThreshold
            });
        }

        public IActionResult Raw()
        {
            return View(new RawTransactionsViewModel
            {
                Transactions = _analysisDataService.Transactions,
                ExcludedCreditKeywords = _analysisDataService.ExcludedCreditKeywords,
                TransactionHighlightThreshold = _analysisDataService.TransactionHighlightThreshold
            });
        }

        public IActionResult Month(string month)
        {
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return NotFound();
            }

            var monthlySummary = _analysisDataService.GetMonthlySummary(month);
            if (monthlySummary is null)
            {
                return NotFound();
            }

            monthlySummary.TransactionHighlightThreshold = _analysisDataService.TransactionHighlightThreshold;
            return View(monthlySummary);
        }

    }
}
