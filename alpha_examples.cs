using System;

public class Program
{
    public static void Main()
    {
        int number = 42;
        object boxed = number;

        int unboxed = (int)boxed;

        number = 100;
        Console.WriteLine($"Original: {number}"); //100
        Console.WriteLine($"Boxed: {boxed}"); // 42
    }
}

// ================================================================================
// ================================================================================

using System;

public static class MyExtensions
{
    public static int WordCount(this string str)
		=> str.Split([' ', '.', '?'], StringSplitOptions.RemoveEmptyEntries).Length;
}

public class Program
{
    public static void Main()
    {
        string s = "Hello Extension Methods";
        int i = s.WordCount();
        Console.WriteLine($"WordCount: {i}"); // 3
    }
}
