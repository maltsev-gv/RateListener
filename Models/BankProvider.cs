using System;
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
    }
}
