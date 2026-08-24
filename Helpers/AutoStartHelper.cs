using System;
using Microsoft.Win32;

namespace RateListener.Helpers;

public static class AutoStartHelper
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AppName = "RateListener";

    public static bool IsEnabled() =>
        GetRunKey()?.GetValue(AppName) != null;

    public static void SetEnabled(bool enabled)
    {
        using var key = GetRunKey(true);
        if (key == null)
            return;
        if (enabled)
            key.SetValue(AppName, $"\"{Environment.ProcessPath}\" /background");
        else
            key.DeleteValue(AppName, false);
    }

    private static RegistryKey GetRunKey(bool writable = false) =>
        Registry.CurrentUser.OpenSubKey(RunKeyPath, writable) ??
        (writable ? Registry.CurrentUser.CreateSubKey(RunKeyPath) : null);
}
