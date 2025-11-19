using Modemas.Server.Interfaces;

namespace Modemas.Server.Services;

public class StatisticsCalculator<T, TResult> : IStatisticsCalculator<T, TResult>
    where T : class
    where TResult : struct
{
    public TResult CalculateAverage(List<T> items, Func<T, TResult> selector)
    {
        if (items == null || !items.Any())
            return default;

        var values = items.Select(selector).ToList();

        if (typeof(TResult) == typeof(int))
        {
            var sum = values.Cast<int>().Sum();
            var avg = sum / values.Count;
            return (TResult)(object)avg;
        }
        else if (typeof(TResult) == typeof(double))
        {
            var sum = values.Cast<double>().Sum();
            var avg = sum / values.Count;
            return (TResult)(object)avg;
        }
        else if (typeof(TResult) == typeof(decimal))
        {
            var sum = values.Cast<decimal>().Sum();
            var avg = sum / values.Count;
            return (TResult)(object)avg;
        }

        return default;
    }

    public TResult CalculateTotal(List<T> items, Func<T, TResult> selector)
    {
        if (items == null || !items.Any())
            return default;

        var values = items.Select(selector).ToList();

        if (typeof(TResult) == typeof(int))
            return (TResult)(object)values.Cast<int>().Sum();
        if (typeof(TResult) == typeof(double))
            return (TResult)(object)values.Cast<double>().Sum();
        if (typeof(TResult) == typeof(decimal))
            return (TResult)(object)values.Cast<decimal>().Sum();

        return default;
    }

    public T? FindTopPerformer(List<T> items, Func<T, TResult> selector)
    {
        if (items == null || !items.Any())
            return default;

        return items.OrderByDescending(selector).FirstOrDefault();
    }
}
