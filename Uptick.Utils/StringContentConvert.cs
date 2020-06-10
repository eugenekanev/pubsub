using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Uptick.Utils
{
    public class StringContentConvert
    {
        public static StringContent Serialize(object content)
        {
            return new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        }
    }
}