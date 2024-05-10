using PortfolioCheck.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PortfolioCheck.Investment;
using System.Transactions;

namespace PortfolioCheck
{
    public class Share
    {
        // Setup Logging for this class
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Class Params
        private string _isin;
        private Dictionary<DateTime, double> _priceRecords;

        public Share(string isin)
        {
            _isin = isin;
            _priceRecords = new Dictionary<DateTime, double>();
        }

        public Share(string isin, DateTime firstRecordDate, double firstRecordPrice) : this(isin)
        {
            _priceRecords.Add(firstRecordDate, firstRecordPrice);
        }

        public Share(string isin, Dictionary<DateTime, double> priceRecord) : this(isin)
        {
            _priceRecords = priceRecord;
        }

        public void AddPriceRecord(DateTime datetime, double price)
        {
            if(_priceRecords.ContainsKey(datetime))
            {
                // We may want to change this behaviour later
                _priceRecords[datetime] = price;
            }
            else
            {
                _priceRecords.Add(datetime, price);
            }            
        }

        public string GetISIN()
        {
            return _isin;
        }

        public double GetPriceAt(DateTime datetime)
        {
            // Check whether a price exists at that moment in time.
            // If there is no price, or no price data at all, return -1.0, so caller is aware that share is not valid for given moment.
            if (_priceRecords.Keys.Count == 0 || _priceRecords.First().Key > datetime)
            {
                return -1.0;
            }
            // Get previous value closest to given datetime, or exact match
            else
            {
                double lastPreviousPrice = _priceRecords.Where(kvp => kvp.Key <= datetime).MaxBy(kvp => kvp.Key).Value;
                return lastPreviousPrice;
            }
        }
    }

    public struct InvestmentValueComposition
    {
        private Dictionary<string, double> _sharesIsinToAmount;
        private Dictionary<string, double> _fondsIdToOwnedPercentage;
        private Dictionary<string, double> _realEstateCityToAmount;

        public Dictionary<string, double> SharesIsinToAmount { get => _sharesIsinToAmount; }
        public Dictionary<string, double> FondsIdToOwnedPercentage { get => _fondsIdToOwnedPercentage; }
        public Dictionary<string, double> RealEstateCityToAmount { get => _realEstateCityToAmount; }

        public InvestmentValueComposition()
        {
            _sharesIsinToAmount = new Dictionary<string, double>();
            _fondsIdToOwnedPercentage = new Dictionary<string, double>();
            _realEstateCityToAmount = new Dictionary<string, double>();
        }
        public InvestmentValueComposition(Dictionary<string, double> sharesIsinToAmount, Dictionary<string, double> fondsIdToOwnedPercentage, Dictionary<string, double> realEstateCityToAmount)
        {
            _sharesIsinToAmount = sharesIsinToAmount;
            _fondsIdToOwnedPercentage = fondsIdToOwnedPercentage;
            _realEstateCityToAmount = realEstateCityToAmount;
        }

        public void Combine(InvestmentValueComposition combineSource)
        {
            foreach(KeyValuePair<string, double> kvp in combineSource.SharesIsinToAmount)
            {
                if (!_sharesIsinToAmount.ContainsKey(kvp.Key))
                {
                    _sharesIsinToAmount.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    _sharesIsinToAmount[kvp.Key] += kvp.Value;
                }
            }

            foreach (KeyValuePair<string, double> kvp in combineSource.FondsIdToOwnedPercentage)
            {
                if (!_fondsIdToOwnedPercentage.ContainsKey(kvp.Key))
                {
                    _fondsIdToOwnedPercentage.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    _fondsIdToOwnedPercentage[kvp.Key] += kvp.Value;
                }
            }

            foreach (KeyValuePair<string, double> kvp in combineSource.RealEstateCityToAmount)
            {
                if (!_realEstateCityToAmount.ContainsKey(kvp.Key))
                {
                    _realEstateCityToAmount.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    _realEstateCityToAmount[kvp.Key] += kvp.Value;
                }
            }
        }
    }

    public class Investment
    {
        // Setup Logging for this class
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Investment Types
        public enum EInvestmentType
        {
            Fonds,
            RealEstate,
            Stock
        }

        // Class Params
        private string _investmentId;
        private EInvestmentType _investmentType;
        private string _investmentTypeId;
        // There might be multiple transactions at the same time
        private SortedDictionary<DateTime, List<Transaction>> _transactions;

        public string InvestmentId { get => _investmentId; }
        public EInvestmentType InvestmentType { get => _investmentType; }
        public string InvestmentTypeId { get => _investmentTypeId; }
        public SortedDictionary<DateTime, List<Transaction>> Transactions { get => _transactions; }

        public Investment(string investmentId, EInvestmentType investmentType, string investmentTypeId)
        {
            _investmentId = investmentId;
            _investmentType = investmentType;
            _investmentTypeId = investmentTypeId;
            _transactions = new SortedDictionary<DateTime, List<Transaction>>();
        }

        public void AddTransaction(DateTime transactionDate, Transaction transaction)
        {
            if(!_transactions.ContainsKey(transactionDate))
            {
                _transactions.Add(transactionDate, new List<Transaction>());                
            }
            _transactions[transactionDate].Add(transaction);
        }

        public InvestmentValueComposition GetValueCompositionAt(DateTime datetime)
        {
            Dictionary<string, double> sharesIsinToAmount = new Dictionary<string, double>();
            Dictionary<string, double> fondsIdToOwnedPercentage = new Dictionary<string, double>();
            Dictionary<string, double> realEstateCityToAmount = new Dictionary<string, double>();

            foreach(KeyValuePair<DateTime, List<Transaction>> kvp in _transactions)
            {
                // Early break if we exceeded query time frame
                if (kvp.Key > datetime)
                    break;
                
                // Go through all transactions and 
                foreach(Transaction t in kvp.Value)
                {
                    switch (t.TransactionType)
                    {
                        case Transaction.ETransactionType.Share:
                            if(!sharesIsinToAmount.ContainsKey(t.Investment.InvestmentTypeId))
                            {
                                sharesIsinToAmount.Add(t.Investment.InvestmentTypeId, t.Value);
                            }
                            else
                            {
                                sharesIsinToAmount[t.Investment.InvestmentTypeId] += t.Value;
                            }
                            break;
                        case Transaction.ETransactionType.Estate:
                            if (!realEstateCityToAmount.ContainsKey(t.Investment.InvestmentTypeId))
                            {
                                realEstateCityToAmount.Add(t.Investment.InvestmentTypeId, t.Value);
                            }
                            else
                            {
                                realEstateCityToAmount[t.Investment.InvestmentTypeId] += t.Value;
                            }
                            break;
                        case Transaction.ETransactionType.Building:
                            if (!realEstateCityToAmount.ContainsKey(t.Investment.InvestmentTypeId))
                            {
                                realEstateCityToAmount.Add(t.Investment.InvestmentTypeId, t.Value);
                            }
                            else
                            {
                                realEstateCityToAmount[t.Investment.InvestmentTypeId] += t.Value;
                            }
                            break;
                        case Transaction.ETransactionType.Percentage:
                            if (!fondsIdToOwnedPercentage.ContainsKey(t.Investment.InvestmentTypeId))
                            {
                                fondsIdToOwnedPercentage.Add(t.Investment.InvestmentTypeId, t.Value);
                            }
                            else
                            {
                                fondsIdToOwnedPercentage[t.Investment.InvestmentTypeId] += t.Value;
                            }
                            break;
                    }
                }    
            }

            // Return an InvestmentComposition object holding all the info for the selected datetime
            return new InvestmentValueComposition(sharesIsinToAmount, fondsIdToOwnedPercentage, realEstateCityToAmount);
        }

    }

    public class Investor
    {
        // Setup Logging for this class
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Class Params
        private string _investorId;
        private Dictionary<string, Investment> _investments;

        public string InvestorId { get => _investorId; }
        public Dictionary<string, Investment> Investments { get => _investments; }

        public Investor(string investorId)
        {
            _investorId = investorId;
            _investments = new Dictionary<string, Investment>();
        }

        public void AddInvestment(Investment investment)
        {
            if(_investments.ContainsKey(investment.InvestmentId))
            {
                logger.Warn("Investor '{0}': Overwriting Data for Investment ID '{1}'.", _investorId, investment.InvestmentId);
                _investments[investment.InvestmentId] = investment;
            }
            else
            {
                _investments.Add(investment.InvestmentId, investment);
            }
        }

        public InvestmentValueComposition GetInvestmentValueAt(string investmentId, DateTime datetime)
        {
            if (!_investments.ContainsKey(investmentId))
            {
                return new InvestmentValueComposition();
            }
            return _investments[investmentId].GetValueCompositionAt(datetime);
        }

        public InvestmentValueComposition GetAllInvestmentsValueAt(DateTime datetime)
        {
            InvestmentValueComposition investmentValueComposition = new InvestmentValueComposition();
            foreach(Investment i in _investments.Values)
            {
                investmentValueComposition.Combine(i.GetValueCompositionAt(datetime));
            }
            return investmentValueComposition;
        }
    }

    public class Transaction
    {
        // Setup Logging for this class
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Transaction Types
        public enum ETransactionType
        {
            Percentage,
            Building,
            Estate,
            Share
        }

        // Class Params
        private Investment _investment;
        private ETransactionType _transactionType;
        private DateTime _date;
        private double _value;

        public Investment Investment { get => _investment; }
        public ETransactionType TransactionType { get => _transactionType; }
        public DateTime Date{ get => _date; }
        public double Value { get => _value; }

        public Transaction(Investment investment, ETransactionType transactionType, DateTime date, double value)
        {
            _investment = investment;
            _transactionType = transactionType;
            _date = date;
            _value = value;
        }
    }
}
