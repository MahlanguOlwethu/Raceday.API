using System.Net.Http;

namespace RaceDay.MVC.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public HttpClient GetClient()
        {
            // Because we configured the 'RaceDayAPI' client in Program.cs, 
            // this will return a client with the correct BaseAddress and CookieContainer.
            return _httpClientFactory.CreateClient("RaceDayAPI");
        }
    }
}