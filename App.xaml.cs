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