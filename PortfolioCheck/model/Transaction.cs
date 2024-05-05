using CsvHelper.Configuration.Attributes;

namespace PortfolioCheck.model
{
    public class Transaction
    {
        private string _investmentId;
        private string _type;
        private string _date;
        private double _value;

        [Name("InvestmentId")]
        public string InvestmentId { get => _investmentId; set => _investmentId = value; }
        [Name("Type")]
        public string Type { get => _type; set => _type = value; }
        [Name("Date")]
        public string Date { get => _date; set => _date = value; }
        [Name("Value")]
        public double Value { get => _value; set => _value = value; }
    }
}
