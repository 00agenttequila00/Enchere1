using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchService : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<Item>>>SearchItem([FromQuery]SearchParams searchParams)
        {
            var query = DB.PagedSearch<Item, Item>();
            
            if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }
            
            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(x => x.Ascending(a => a.Make)),
                "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd)),
            };
            query = searchParams.FilterBy switch
            {
                "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
                "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd>DateTime.UtcNow),
                "live" => query.Match(x => x.AuctionEnd > DateTime.UtcNow),
                _ => query // No filter by default - show all auctions
            };
            if (!string.IsNullOrWhiteSpace(searchParams.Seller))query.Match(x=> x.Seller ==searchParams.Seller);
            if (!string.IsNullOrWhiteSpace(searchParams.Winner)) query.Match(x => x.Winner == searchParams.Winner);

            query.PageNumber(searchParams.PageNumber);
            query.PageSize(searchParams.PageSize);
            var result = await query.ExecuteAsync();
            return Ok(new
            {
                results = result.Results,
                pageCount = result.PageCount,
                totalCount = result.TotalCount
            
            });
        }

    }
}
