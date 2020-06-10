using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Uptick.Utils
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<TResult> ReadContentAsync<TResult>(this HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<TResult>(await response.Content.ReadAsStringAsync());
        }
    }
}