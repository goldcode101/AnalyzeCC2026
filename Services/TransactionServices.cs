using CsvHelper;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using CreditCardAnalyzer.Models;
using CsvHelper.Configuration;

namespace CreditCardAnalyzer.Services
{
    public class TransactionService
    {
        private readonly string _csvPath;

        public TransactionService(string csvPath)
        {
            _csvPath = csvPath;
        }

        public List<Transaction> ImportTransactions()
        {
            using (var reader = new StreamReader(_csvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<TransactionMap>();
                return csv.GetRecords<Transaction>().ToList();
            }
        }

        private class TransactionMap : ClassMap<Transaction>
        {
            public TransactionMap()
            {
                Map(m => m.TransactionDate).Name("Transaction Date");
                Map(m => m.PostedDate).Name("Posted Date");
                Map(m => m.CardNo).Name("Card No.");
                Map(m => m.Description).Name("Description");
                Map(m => m.Category).Name("Category");

                // Treat empty fields as 0 using Convert to handle nulls
                Map(m => m.Debit)
                    .Name("Debit")
                    .Convert(args => args.Row.GetField<decimal?>("Debit") ?? 0m);

                Map(m => m.Credit)
                    .Name("Credit")
                    .Convert(args => args.Row.GetField<decimal?>("Credit") ?? 0m);
            }
        }
    }
}
