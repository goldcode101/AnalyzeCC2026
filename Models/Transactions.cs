using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CreditCardAnalyzer.Models
{
    public class Transaction
    {
        public DateTime TransactionDate { get; set; }
        public DateTime PostedDate { get; set; }
        public string CardNo { get; set; } // Mask this for security!
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        // Calculate net amount (Debit - Credit)
        public decimal NetAmount => Debit - Credit;
    }

    public class TransactionGroup : List<Transaction>
    {
        public string Name { get; set; } = string.Empty;

        public decimal TotalSpent => this.Sum(t => t.Debit);
        public decimal TotalCredits => this.Sum(t => t.Credit);
    }

    public class AnalysisSummary
    {
        public Dictionary<string, TransactionGroup> MonthlyTotals { get; } = new();
        public Dictionary<string, TransactionGroup> ByMonth { get; } = new();
        public Dictionary<string, TransactionGroup> ByCategory { get; } = new();
        public Dictionary<string, TransactionGroup> ByMerchant { get; } = new();

        public decimal TotalSpent => MonthlyTotals.Values.Sum(group => group.TotalSpent);
    }

    public class AnalysisViewModel
    {
        public List<Transaction> Transactions { get; set; } = new();
        public AnalysisSummary Summary { get; set; } = new();
    }
}

