using System.Net.Http;
using System.Net.Http.Headers;

namespace Infrastructure.Utils
{
    public static class MeterApiClientHelper
    {
        public static HttpClient CreateAuthorizedMeterClient(IHttpClientFactory httpClientFactory, string accessToken)
        {
            var meterClient = httpClientFactory.CreateClient("meter-api");
            meterClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return meterClient;
        }
    }
}
