using System;
using System.IO;
using System.Reflection;

namespace RateListener.Helpers
{
    public class Logger
    {
        private static string _location;

        static Logger()
        {
            _location = Assembly.GetExecutingAssembly().Location;
            _location = Path.Combine(Path.GetDirectoryName(_location), "Rates.log");
            File.AppendAllText(_location, $"{Environment.NewLine}{DateTime.Now:dd MMM yy H:mm:ss}: {nameof(RateListener)} started{Environment.NewLine}");
        }

        public static void Log(string message)
        {
            File.AppendAllText(_location, $"{DateTime.Now:dd MMM yy H:mm:ss}: {message}{Environment.NewLine}");
        }
    }
}
