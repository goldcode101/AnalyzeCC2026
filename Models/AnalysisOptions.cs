namespace CreditCardAnalyzer.Models
{
    public class AnalysisOptions
    {
        public List<CategoryRule> CustomCategoryRules { get; set; } = new();
        public List<string> ExcludedCreditKeywords { get; set; } = new();
        public decimal TransactionHighlightThreshold { get; set; } = 50m;
    }
}