using System.IO;

namespace RateListener.Helpers;

public static class ParseDiagnostics
{
    public static void SaveFailedSource(string providerName, string source)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "RateListener");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"{providerName}_last_failed.html"), source);
        }
        catch
        {
        }
    }
}
