using CreditCardAnalyzer.Models;
using CreditCardAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CreditCardAnalyzer.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly TransactionService _transactionService;

        public AnalysisController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        public IActionResult Index()
        {
            var transactions = _transactionService.ImportTransactions();
            var summary = TransactionService.BuildAnalysisSummary(transactions);

            var viewModel = new AnalysisViewModel
            {
                Transactions = transactions,
                Summary = summary
            };

            return View(viewModel);
        }
    }
}
