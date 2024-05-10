/*
 * Base class for a small console application to determine Portfolio values at a given point in time using CSV data.
 * This program was part of a coding test for a job interview.
 * 
 * Since this is the first time for me coding in C# for a long time, it may contain outdated concepts or non-SOTA procedures.
 */
using CommandLine.Text;
using CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using PortfolioCheck.model;
using System.Globalization;
using NLog.Config;
using System.Text;

namespace PortfolioCheck
{
    public class PortfolioCheckConstants
    {
        public const string InvestmentsFile = "Investments.csv";
        public const string QuotesFile = "Quotes.csv";
        public const string TransactionsFile = "Transactions.csv";
    }

    public class Program
    {
        // Setup Logging for this class
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        // Options from parser
        private static Options _options;

        // Program arguments used for customizing the appplication behaviour
        public class Options
        {
            [Option('d', "data-folder", Required = false, Default = "data", HelpText = "Specify the folder to look for files")]
            public string DataFolder { get; set; }
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('c', "culture-info", Required = false, Default = "de-DE", HelpText = "Set culture Info for displaying values and currencies. Default is 'de-DE'.")]
            public string CultureInfo { get; set; }
        }

        static void RunOptions(Options opts)
        {
            // Just map 1:1 here as long as there's no special init required
            _options = opts;
            // Print option values
            logger.Info("Using Data folder: {0}", _options.DataFolder);
            logger.Info("Using Culture Info: {0}", _options.CultureInfo);
            if (_options.Verbose)
            {
                logger.Info("Verbose output mode enabled.");
            }            
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            // Print errors; TODO: evaluate whether they're critical
            foreach(Error e in errs)
            {
                logger.Warn("Error during application startup: {0}", e.ToString());
            }
        }

        // Lists for holding Raw Data on startup
        private static List<model.Investment> rawInvestments;
        private static List<model.Quote> rawQuotes;
        private static List<model.Transaction> rawTransactions;

        // Lists containing processed Data
        private static Dictionary<string, Share> shares = new Dictionary<string, Share>();
        private static Dictionary<string, Investment> investments = new Dictionary<string, Investment>();
        private static Dictionary<string, Investor> investors = new Dictionary<string, Investor>();

        static void Main(string[] args)
        {
            // Set console encoding to UTF-8
            Console.OutputEncoding = Encoding.UTF8;

            // Parse arguments
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);

            // Load Data and prepare it for querying
            try
            {
                loadRawData();
                buildLinkedDataModel();
            }
            catch (Exception e)
            {
                logger.Error("Error during data loading: {0}", _options.Verbose ? e.ToString() : e.Message);
                logger.Error("Exiting...");
                return;
            }
            logger.Info("Application data loaded successfully.");

            logger.Info("Please enter your lookup query in the format 'd-m-y hh:mm:ss;Investor$id|Fonds$id' below:");
            Console.WriteLine("");
            string line = Console.ReadLine();

            // Begin main query loop of the application
            while (!string.IsNullOrWhiteSpace(line))
            {
                logger.Info("Input query received: '{0}'", line);
                try
                {
                    string[] input = line.Split(";");
                    if (input.Length != 2 || input[0].Length == 0 || input[1].Length == 0)
                    {
                        throw new ArgumentException("Input query did not contain a semicolon separator ';', or the input elements left or right were empty.");
                    }

                    DateTime date = DateTime.Parse(input[0]);
                    string investorId = input[1];

                    // Find Investor in the data if it exists
                    if(!investors.ContainsKey(investorId))
                    {
                        throw new KeyNotFoundException("The provided Investor ID does not exist in the data.");
                    }
                    Investor investor = investors[investorId];

                    // Get values for all Investment types
                    InvestmentValueComposition investmentAtDate = investor.GetAllInvestmentCompositionsAt(date);
                    double totalShareValue = investmentAtDate.GetSharesValue(date, shares);
                    double totalFondsValue = investmentAtDate.GetFondsValue(date, investors, shares);
                    double totalEstateValue = investmentAtDate.GetEstateValue();
                    double combinedValue = totalShareValue + totalFondsValue + totalEstateValue;

                    // Culture Info for output
                    CultureInfo ci = new CultureInfo(_options.CultureInfo);

                    Console.WriteLine(String.Format(ci, "Total Value for Investor '{0}' at date '{1}': {2:C}", investorId, date, Math.Round(combinedValue, 2)));
                    if (_options.Verbose)
                    {
                        Console.WriteLine(String.Format("From Shares: {0:C} ({1:P2})", totalShareValue, totalShareValue / combinedValue));
                        Console.WriteLine(String.Format("From Fonds: {0:C} ({1:P2})", totalFondsValue, totalFondsValue / combinedValue));
                        Console.WriteLine(String.Format("From Real Estate: {0:C} ({1:P2})", totalEstateValue, totalEstateValue / combinedValue));
                    }
                }
                catch (Exception e)
                {
                    logger.Warn("An Exception occured while processing your query: {0}", _options.Verbose ? e.ToString() : e.Message);
                    logger.Warn("Returning to start.");    
                }
                finally
                {
                    Console.WriteLine("");
                    logger.Info("Please enter your lookup query in the format 'd-m-y hh:mm:ss;Investor$id|Fonds$id' below:");
                    Console.WriteLine("");
                    line = Console.ReadLine();
                }
            }


        }

        // loadRawData loads raw data from files in data directory
        private static void loadRawData()
        {
            // Check if required files exist
            if(!Directory.Exists(_options.DataFolder))
            {
                throw new DirectoryNotFoundException(string.Format("Specified data directory '{0}' does not exist. Please verify it's specified correctly.", _options.DataFolder));
            }
            if (!File.Exists(_options.DataFolder + "/" + PortfolioCheckConstants.InvestmentsFile))
            {
                throw new FileNotFoundException(string.Format("Required file '{0}' does not exist in specified data directory '{1}'. Please verify you're pointing to the correct data directory.", PortfolioCheckConstants.InvestmentsFile, _options.DataFolder));
            }
            if (!File.Exists(_options.DataFolder + "/" + PortfolioCheckConstants.QuotesFile))
            {
                throw new FileNotFoundException(string.Format("Required file '{0}' does not exist in specified data directory '{1}'. Please verify you're pointing to the correct data directory.", PortfolioCheckConstants.QuotesFile, _options.DataFolder));
            }
            if (!File.Exists(_options.DataFolder + "/" + PortfolioCheckConstants.TransactionsFile))
            {
                throw new FileNotFoundException(string.Format("Required file '{0}' does not exist in specified data directory '{1}'. Please verify you're pointing to the correct data directory.", PortfolioCheckConstants.TransactionsFile, _options.DataFolder));
            }

            // Configure CSV Reader - Likely requires adaption for different format
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                NewLine = Environment.NewLine,
                Delimiter = ";",
            };

            // Load raw investments
            using (StreamReader reader = new StreamReader(_options.DataFolder + "/" + PortfolioCheckConstants.InvestmentsFile))
            using (CsvReader csv = new CsvReader(reader, config))
            {
                rawInvestments = csv.GetRecords<model.Investment>().ToList();
                logger.Info("Loaded {0} raw Investment entries.", rawInvestments.Count);
            }

            // Load raw quotes
            using (StreamReader reader = new StreamReader(_options.DataFolder + "/" + PortfolioCheckConstants.QuotesFile))
            using (CsvReader csv = new CsvReader(reader, config))
            {
                rawQuotes = csv.GetRecords<model.Quote>().ToList();
                logger.Info("Loaded {0} raw Quote entries.", rawQuotes.Count);
            }

            // Load raw transactions
            using (StreamReader reader = new StreamReader(_options.DataFolder + "/" + PortfolioCheckConstants.TransactionsFile))
            using (CsvReader csv = new CsvReader(reader, config))
            {
                rawTransactions = csv.GetRecords<model.Transaction>().ToList();
                logger.Info("Loaded {0} raw Transaction entries.", rawTransactions.Count);
            }
        }

        // buildLinkedDataModel processes the raw data stored in memory and creates necessary index information
        private static void buildLinkedDataModel()
        {
            // Process shares using quote list
            foreach(model.Quote q in rawQuotes)
            {
                // Parse Date
                DateTime quoteDate;
                try
                {
                    quoteDate = DateTime.Parse(q.Date);
                }
                catch (FormatException)
                {
                    logger.Warn("Invalid Date format provided for ISIN {0} and date {1}. Skipping...", q.Isin, q.Date);
                    continue;
                }

                if (shares.ContainsKey(q.Isin))
                {
                    shares[q.Isin].AddPriceRecord(quoteDate, q.PrivePerShare);
                }
                else
                {
                    shares.Add(q.Isin, new Share(q.Isin, quoteDate, q.PrivePerShare));
                }
            }

            // Process Investments
            foreach(model.Investment i in rawInvestments)
            {
                // Find or create investor reference
                Investor investor;
                if(investors.ContainsKey(i.InvestorId))
                {
                    investor = investors[i.InvestorId];
                }
                else
                {
                    investor = new Investor(i.InvestorId);
                    investors.Add(investor.InvestorId, investor);
                }

                // Create Investment object if data is valid
                Investment investment;
                switch(i.InvestmentType)
                {
                    case "Fonds":
                        investment = new Investment(i.InvestmentId, Investment.EInvestmentType.Fonds, i.FondsInvestor);
                        break;
                    case "RealEstate":
                        investment = new Investment(i.InvestmentId, Investment.EInvestmentType.RealEstate, i.City);
                        break;
                    case "Stock":
                        investment = new Investment(i.InvestmentId, Investment.EInvestmentType.Stock, i.ISIN);
                        break;
                    default:
                        logger.Warn("Unknown investment type '{0}' specified for Investment ID '{1}'. Skipping...", i.InvestmentType, i.InvestmentId);
                        continue;
                }

                // Link with reference map and investor
                investments.Add(investment.InvestmentId, investment);
                investor.AddInvestment(investment);
            }

            // Process Transactions
            foreach(model.Transaction t in rawTransactions)
            {
                // Parse Date
                DateTime transactionDate;
                try
                {
                    transactionDate = DateTime.Parse(t.Date);
                }
                catch (FormatException)
                {
                    logger.Warn("Invalid Date format provided for transaction of Investment ID '{0}' and date {1}. Skipping...", t.InvestmentId, t.Date);
                    continue;
                }

                // Find Investment Reference
                if (!investments.ContainsKey(t.InvestmentId))
                {
                    logger.Warn("Investment ID '{0}' of Transaction Type {1} at {2} is invalid. Skipping...", t.InvestmentId, t.Type, t.Date);
                    continue;
                }
                Investment investment = investments[t.InvestmentId];

                // Create Transaction object if data is valid
                Transaction transaction;
                switch(t.Type)
                {
                    case "Percentage":
                        transaction = new Transaction(investment, Transaction.ETransactionType.Percentage, transactionDate, t.Value);
                        break;
                    case "Building":
                        transaction = new Transaction(investment, Transaction.ETransactionType.Building, transactionDate, t.Value);
                        break;
                    case "Estate":
                        transaction = new Transaction(investment, Transaction.ETransactionType.Estate, transactionDate, t.Value);
                        break;
                    case "Shares":
                        transaction = new Transaction(investment, Transaction.ETransactionType.Share, transactionDate, t.Value);
                        break;
                    default:
                        logger.Warn("Unknown transaction type '{0}' specified for Investment ID '{1}' at {2}. Skipping...", t.Type, investment.InvestmentId, t.Date);
                        continue;
                }

                // Link with investment
                investment.AddTransaction(transactionDate, transaction);
            }
        }
    }
}