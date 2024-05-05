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
        }

        static void RunOptions(Options opts)
        {
            // Just map 1:1 here as long as there's no special init required
            _options = opts;
        }
        static void HandleParseError(IEnumerable<Error> errs)
        {
            // Print errors; TODO: evaluate whether they're critical
            foreach(Error e in errs)
            {
                logger.Warn("Error during application startup: {0}", e.ToString());
            }
        }

        // Actual class Params start here
        private static List<Investment> rawInvestments;
        private static List<Quote> rawQuotes;
        private static List<Transaction> rawTransactions;

        static void Main(string[] args)
        {
            // Parse arguments
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);

            // Load Data and prepare it for querying
            try
            {
                loadRawData();
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

                    // TODO: Actual lookup algorithm
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

        // load raw data from files in data directory
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
                rawInvestments = csv.GetRecords<Investment>().ToList();
                logger.Info("Loaded {0} raw Investment entries.", rawInvestments.Count);
            }

            // Load raw quotes
            using (StreamReader reader = new StreamReader(_options.DataFolder + "/" + PortfolioCheckConstants.QuotesFile))
            using (CsvReader csv = new CsvReader(reader, config))
            {
                rawQuotes = csv.GetRecords<Quote>().ToList();
                logger.Info("Loaded {0} raw Quote entries.", rawQuotes.Count);
            }

            // Load raw transactions
            using (StreamReader reader = new StreamReader(_options.DataFolder + "/" + PortfolioCheckConstants.TransactionsFile))
            using (CsvReader csv = new CsvReader(reader, config))
            {
                rawTransactions = csv.GetRecords<Transaction>().ToList();
                logger.Info("Loaded {0} raw Transaction entries.", rawTransactions.Count);
            }
        }

    }
}