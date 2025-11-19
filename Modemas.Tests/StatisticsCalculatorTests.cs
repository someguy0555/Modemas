using Modemas.Server.Services;

public class StatisticsCalculatorTests
{
    private class Sample
    {
        public int ScoreInt { get; set; }
        public double ScoreDouble { get; set; }
        public decimal ScoreDecimal { get; set; }
    }

    private readonly StatisticsCalculator<Sample, int> _intCalc = new();
    private readonly StatisticsCalculator<Sample, double> _doubleCalc = new();
    private readonly StatisticsCalculator<Sample, decimal> _decimalCalc = new();

    [Fact]
    public void CalculateAverage_Int_ReturnsCorrectAverage()
    {
        var items = new List<Sample>
        {
            new() { ScoreInt = 10 },
            new() { ScoreInt = 20 },
            new() { ScoreInt = 30 }
        };

        var result = _intCalc.CalculateAverage(items, x => x.ScoreInt);

        Assert.Equal(20, result);
    }

    [Fact]
    public void CalculateAverage_Double_ReturnsCorrectAverage()
    {
        var items = new List<Sample>
        {
            new() { ScoreDouble = 5.5 },
            new() { ScoreDouble = 6.5 }
        };

        var result = _doubleCalc.CalculateAverage(items, x => x.ScoreDouble);

        Assert.Equal(6.0, result, 5);
    }

    [Fact]
    public void CalculateAverage_Decimal_ReturnsCorrectAverage()
    {
        var items = new List<Sample>
        {
            new() { ScoreDecimal = 2.0m },
            new() { ScoreDecimal = 4.0m }
        };

        var result = _decimalCalc.CalculateAverage(items, x => x.ScoreDecimal);

        Assert.Equal(3.0m, result);
    }

    [Fact]
    public void CalculateAverage_EmptyList_ReturnsDefault()
    {
        var result = _intCalc.CalculateAverage(new List<Sample>(), x => x.ScoreInt);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateTotal_Int_ReturnsCorrectTotal()
    {
        var items = new List<Sample>
        {
            new() { ScoreInt = 10 },
            new() { ScoreInt = 5 }
        };

        var result = _intCalc.CalculateTotal(items, x => x.ScoreInt);

        Assert.Equal(15, result);
    }

    [Fact]
    public void CalculateTotal_Double_ReturnsCorrectTotal()
    {
        var items = new List<Sample>
        {
            new() { ScoreDouble = 1.5 },
            new() { ScoreDouble = 2.5 }
        };

        var result = _doubleCalc.CalculateTotal(items, x => x.ScoreDouble);

        Assert.Equal(4.0, result, 5);
    }

    [Fact]
    public void CalculateTotal_Decimal_ReturnsCorrectTotal()
    {
        var items = new List<Sample>
        {
            new() { ScoreDecimal = 1.0m },
            new() { ScoreDecimal = 2.5m }
        };

        var result = _decimalCalc.CalculateTotal(items, x => x.ScoreDecimal);

        Assert.Equal(3.5m, result);
    }

    [Fact]
    public void CalculateTotal_EmptyList_ReturnsDefault()
    {
        var result = _doubleCalc.CalculateTotal(new List<Sample>(), x => x.ScoreDouble);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void FindTopPerformer_ReturnsHighestItem()
    {
        var a = new Sample { ScoreInt = 10 };
        var b = new Sample { ScoreInt = 20 };
        var c = new Sample { ScoreInt = 5 };

        var items = new List<Sample> { a, b, c };

        var result = _intCalc.FindTopPerformer(items, x => x.ScoreInt);

        Assert.Equal(b, result);
    }

    [Fact]
    public void FindTopPerformer_EmptyList_ReturnsNull()
    {
        var result = _intCalc.FindTopPerformer(new List<Sample>(), x => x.ScoreInt);

        Assert.Null(result);
    }

    [Fact]
    public void FindTopPerformer_NullList_ReturnsNull()
    {
        List<Sample>? items = null;

        var result = _intCalc.FindTopPerformer(items, x => x.ScoreInt);

        Assert.Null(result);
    }
}
