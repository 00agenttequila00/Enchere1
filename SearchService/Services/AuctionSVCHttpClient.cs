using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services
{
    public class AuctionSVCHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AuctionSVCHttpClient(HttpClient httpclient , IConfiguration config)
        {
            _httpClient = httpclient;
            _config = config;
        }
        public async Task<List<Item>> GetItemForSearchDB()
        {
            var lastupdated = await DB.Find<Item, String>().Sort(x=>x.Descending(x=>x.UpdatedAt)).Project(x=>x.UpdatedAt.ToString()).ExecuteFirstAsync();
            return await _httpClient.GetFromJsonAsync<List<Item>>(_config["AuctionServiceURL"] + "/api/auctions?date=" + lastupdated);
        }
    }
}
