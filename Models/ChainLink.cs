namespace RateListener.Models
{
    public class ChainLink
    {
        public string From { get; set; }
        
        public string To { get; set; }

        public double Fx { get; set; }
        public string FxToDisplay => Fx < 1 ? $"{1.0 / Fx:N2}" : $"{Fx:N2}";

        public int Order { get; set; }

        public Rate Rate { get; set; }

        public override string ToString() => $"{From} -> {To} ({FxToDisplay})";
    }
}
