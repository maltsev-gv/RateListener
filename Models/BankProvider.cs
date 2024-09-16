using System.Collections.Generic;
using RateListener.Providers;

namespace RateListener.Models
{
    public class BankProvider
    {
        public BankProvider(IRatesProvider ratesProvider)
        {
            RatesProvider = ratesProvider;
        }

        public string Name => RatesProvider.Name;

        public IRatesProvider RatesProvider { get; set; }
        
        public static List<BankProvider> SupportedBankProviders { get; } = new()
        {
            new BankProvider(new FfinProvider()),
            new BankProvider(new BccFxProvider()),
            new BankProvider(new BccStableProvider()),
        };
    }
}
