using CreditCardAnalyzer.Models;
using CreditCardAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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
            var transactions = _transactionService.ImportTransactions();
            var categoryRules = _configuration
                .GetSection("CustomCategoryRules")
                .Get<List<CategoryRule>>() ?? new List<CategoryRule>();
            var excludedCreditKeywords = _configuration
                .GetSection("ExcludedCreditKeywords")
                .Get<List<string>>() ?? new List<string>();

            var enrichedTransactions = TransactionService.ApplyCustomCategoryRules(transactions, categoryRules);
            var summary = TransactionService.BuildAnalysisSummary(
                enrichedTransactions,
                categoryRules,
                excludedCreditKeywords);

            var viewModel = new AnalysisViewModel
            {
                Transactions = enrichedTransactions,
                Summary = summary,
                CategoryRules = categoryRules
            };

            return View(viewModel);
        }
    }
}
