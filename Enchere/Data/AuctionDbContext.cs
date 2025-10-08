using Enchere.Models;
using Microsoft.EntityFrameworkCore;

namespace Enchere.Data
{
    public class AuctionDbContext : DbContext
    {
        public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
        {
        }
        public DbSet<Auction> Auctions { get; set; }
        DbSet<Item> Items { get; set; }


    }
}
