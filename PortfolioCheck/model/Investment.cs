using CsvHelper.Configuration.Attributes;

namespace PortfolioCheck.model
{
    public class Investment
    {
        private string _investorId;
        private string _investmentId;
        private string _investmentType;
        private string _isin;
        private string _city;
        private string _fondsInvestor;

        [Name("InvestorId")]
        public string InvestorId { get => _investorId; set => _investorId = value; }
        [Name("InvestmentId")]
        public string InvestmentId { get => _investmentId; set => _investmentId = value; }
        [Name("InvestmentType")]
        public string InvestmentType { get => _investmentType; set => _investmentType = value; }
        [Name("ISIN")]
        public string ISIN { get => _isin; set => _isin = value; }
        [Name("City")]
        public string City { get => _city; set => _city = value; }
        [Name("FondsInvestor")]
        public string FondsInvestor { get => _fondsInvestor; set => _fondsInvestor = value; }
    }
}
