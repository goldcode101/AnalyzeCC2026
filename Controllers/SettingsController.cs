using CreditCardAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace CreditCardAnalyzer.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IConfiguration _configuration;

        public SettingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var categoryRules = _configuration
                .GetSection("CustomCategoryRules")
                .Get<List<CategoryRule>>() ?? new List<CategoryRule>();

            return View(categoryRules);
        }
    }
}
