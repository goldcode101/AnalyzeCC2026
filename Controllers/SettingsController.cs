using CreditCardAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CreditCardAnalyzer.Controllers
{
    public class SettingsController : Controller
    {
        private readonly AnalysisOptions _analysisOptions;

        public SettingsController(IOptions<AnalysisOptions> analysisOptions)
        {
            _analysisOptions = analysisOptions.Value;
        }

        public IActionResult Index()
        {
            return View(_analysisOptions.CustomCategoryRules);
        }
    }
}
