using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace RateListener;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
#nullable enable
    protected override void OnStartup(StartupEventArgs e)
    {
        //TestMethod("OnStartup", this);
        base.OnStartup(e);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    private void TestMethod(string? text, object? testArg)
    {
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            ThrowIfNull.Validate((nameof(text), text), (nameof(testArg), testArg));
        }
        stopwatch.Stop();
        Console.WriteLine($"ThrowIfNull.Validate: {stopwatch.Elapsed.TotalMilliseconds} ms");
        
        stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            if (testArg is null)
                throw new ArgumentNullException(nameof(testArg));
        }
        stopwatch.Stop();
        Console.WriteLine($"if (text is null): {stopwatch.Elapsed.TotalMilliseconds} ms");
        
        var trimmed = text!.Trim();
    }
}

public static class ThrowIfNull
{
    public static void Validate(params (string, object?)[] args)
    {
        foreach (var tuple in args)
            if (tuple.Item2 is null)
            {
                var stackTrace = new StackTrace();
                var callerMethod = stackTrace.GetFrame(1)!.GetMethod()!;
                var callerType = callerMethod.DeclaringType!;
                throw new ArgumentNullException($"Argument {tuple.Item1} cannot be null (in {callerType.FullName}.{callerMethod.Name}()).");
            }
    }
}