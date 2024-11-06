using System;
using System.IO;
using System.Reflection;

namespace RateListener.Helpers
{
    public class Logger
    {
        private static readonly string Location;

        static Logger()
        {
            Location = Assembly.GetExecutingAssembly().Location;
            Location = Path.Combine(Path.GetDirectoryName(Location)!, "Rates.log");
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
