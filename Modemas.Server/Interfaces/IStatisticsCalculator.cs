namespace Modemas.Server.Interfaces;

public interface IStatisticsCalculator<T, TResult>
    where T : class
    where TResult : struct
{
    TResult CalculateAverage(List<T> items, Func<T, TResult> selector);
    TResult CalculateTotal(List<T> items, Func<T, TResult> selector);
    T? FindTopPerformer(List<T> items, Func<T, TResult> selector);
}
