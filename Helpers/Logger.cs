using System;
using System.IO;

namespace RateListener.Helpers
{
    public class Logger
    {
        private static readonly string Location;

        static Logger()
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RateListener");
            Directory.CreateDirectory(dataDirectory);
            Location = Path.Combine(dataDirectory, "Rates.log");
            File.AppendAllText(Location, $"{Environment.NewLine}{DateTime.Now:dd MMM yy H:mm:ss}: {nameof(RateListener)} started{Environment.NewLine}");
        }

        public static void Log(string message)
        {
            lock (Location)
            {
                File.AppendAllText(Location, $"{DateTime.Now:dd MMM yy H:mm:ss}: {message}{Environment.NewLine}");
            }
        }
    }
}
