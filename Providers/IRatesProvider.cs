using System.Threading.Tasks;
using RateListener.Models;

namespace RateListener.Providers
{
    public interface IRatesProvider
    {
        string Name {  get; }
        string Url { get; }
        Task<RatesResponse> GetRatesResponse();
    }
}
