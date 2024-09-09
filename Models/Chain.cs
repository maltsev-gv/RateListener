using RateListener.ExtensionMethods;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;

namespace RateListener.Models
{
    public class Chain
    {
        public Chain(List<ChainLink> links)
        {
            Links = links;
        }

        public List<ChainLink> Links { get; set; }
        public double EffectiveRate
        {
            get
            {
                var rate = 1.0;
                for (var i = 0; i < Links.Count; i++)
                {
                    rate *= Links[i].Fx;
                }
                return rate;
            }
        }

        public string Presentation
        {
            get
            {
                var ci = CultureInfo.CurrentCulture;
                var sb = new StringBuilder($"{EffectiveRate.RateToDisplay()}: ");
                sb.Append($"{Links[0].From}");
                foreach (var link in Links)
                {
                    sb.Append($" -> {link.To} ({link.FxToDisplay})");
                }
                return sb.ToString();
            }
        }
    }
}
