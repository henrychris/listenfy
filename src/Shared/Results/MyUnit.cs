namespace Listenfy.Shared.Results;

/// <summary>
/// This is an empty class which should be used when you don't intend to return a value from a method.
/// If a method is not to return a value, return Result&lt;MyUnit&gt; instead of just Result.
/// </summary>
public class MyUnit
{
    public static readonly MyUnit Value = new();
}
