namespace PortfolioCheck.Tests;

[TestFixture]
public class ShareTestEmptyRecordList
{
    private Share _share;

    [SetUp]
    public void Setup()
    {
        _share = new Share("ISIN0");
    }

    [Test]
    public void TestShareEmptyRecordListReturnMinus1()
    {
        double currentPrice = _share.GetPriceAt(DateTime.Now);
        Assert.That(currentPrice, Is.EqualTo(-1.0));
    }

    [Test]
    public void TestShareNewerRecordListReturnMinus1()
    {
        _share.AddPriceRecord(DateTime.Now, 200);
        double currentPrice = _share.GetPriceAt(DateTime.Now.AddMinutes(-60));
        Assert.That(currentPrice, Is.EqualTo(-1.0));
    }
}

[TestFixture]
public class ShareTestExampleRecordList
{
    private Share _share;

    [SetUp]
    public void Setup()
    {
        var exampleRecords = new Dictionary<DateTime, double>();
        exampleRecords.Add(DateTime.Now.AddDays(-3), 200);
        exampleRecords.Add(DateTime.Now.AddDays(-10), 150);
        exampleRecords.Add(DateTime.Now.AddDays(-1), 220);
        exampleRecords.Add(DateTime.Now.AddDays(-5), 250);
        _share = new Share("ISIN0", exampleRecords);
    }

    [Test]
    public void TestShareValueToday()
    {
        double currentPrice = _share.GetPriceAt(DateTime.Now);
        Assert.That(currentPrice, Is.EqualTo(220.0));
    }

    [Test]
    public void TestShareValueMinus1Day()
    {
        double currentPrice = _share.GetPriceAt(DateTime.Now.AddDays(-1.5));
        Assert.That(currentPrice, Is.EqualTo(200.0));
    }

    [Test]
    public void TestShareValueMinus2Day()
    {
        double currentPrice = _share.GetPriceAt(DateTime.Now.AddDays(-2.5));
        Assert.That(currentPrice, Is.EqualTo(200.0));
    }

    [Test]
    public void TestShareValueMinus3Day()
    {
        double currentPrice = _share.GetPriceAt(DateTime.Now.AddDays(-3.5));
        Assert.That(currentPrice, Is.EqualTo(250.0));
    }

    [Test]
    public void TestShareValueMinus7Day()
    {
        double currentPrice = _share.GetPriceAt(DateTime.Now.AddDays(-7.5));
        Assert.That(currentPrice, Is.EqualTo(150.0));
    }
}