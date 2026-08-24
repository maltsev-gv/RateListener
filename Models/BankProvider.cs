using System.Collections.Generic;
using RateListener.Providers;

namespace RateListener.Models
{
    public class BankProvider
    {
        private BankProvider(IRatesProvider ratesProvider)
        {
            RatesProvider = ratesProvider;
        }

        public string Name => RatesProvider.Name;
        
        public IRatesProvider RatesProvider { get; set; }
        
        public static List<BankProvider> SupportedBankProviders { get; } =
        [
            new(new FfinProvider()),
            new(new BccFxProvider()),
            new(new BccStableProvider()),
            new(new CifraBankProvider())
        ];

        public static double DefaultPollIntervalSec(string providerName) =>
            providerName != null && providerName.Contains("Center Credit") ? 30 : 600;
    }
}
