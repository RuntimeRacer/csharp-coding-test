using CsvHelper.Configuration.Attributes;

namespace PortfolioCheck.model
{
    public class Quote
    {
        private string _isin;
        private string _date;
        private double _privePerShare;

        [Name("ISIN")]
        public string Isin { get => _isin; set => _isin = value; }
        [Name("Date")]
        public string Date { get => _date; set => _date = value; }        
        [Name("PricePerShare")]
        public double PrivePerShare { get => _privePerShare; set => _privePerShare = value; }
    }
}
