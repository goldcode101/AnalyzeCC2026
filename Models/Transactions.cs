using System;
using System.Globalization;

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
}
