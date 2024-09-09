namespace RateListener.Models
{
    public class RatesResponse
    {
        public bool Success { get; set; }
        
        public string Message { get; set; }

        public RateContainer Data { get; set; }
    }

    public class RateContainer
    { 
        public Rate[] Cash { get; set; }
        public Rate[] Mobile { get; set; }
        public Rate[] Non_Cash { get; set; }
    }

    public class Rate
    {
        public string BuyCode { get; set; }
        public string SellCode { get; set; }
        public double BuyRate { get; set; }
        public double SellRate { get; set; }

        public bool IsCurrUsed(string currency) => BuyCode == currency || SellCode == currency;

        public override string ToString() => $"{BuyCode} / {SellCode} ({BuyRate:N2} / {SellRate:N2})";
    }
}
