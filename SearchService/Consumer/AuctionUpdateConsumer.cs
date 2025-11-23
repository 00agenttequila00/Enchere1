using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumer
{
    public class AuctionUpdateConsumer : IConsumer<AuctionUpdated>
    {
        private readonly IMapper _mapper;

        public AuctionUpdateConsumer(IMapper mapper)
        {
            _mapper = mapper;
        }
        
        public async Task Consume(ConsumeContext<AuctionUpdated> context)
        {
            Console.WriteLine("----> Consuming Update Message for auction: " + context.Message.Id);
            Console.WriteLine($"----> New Model: {context.Message.Model}");
            
            // Check if item exists first
            var existingItem = await DB.Find<Item>().OneAsync(context.Message.Id);
            if (existingItem == null)
            {
                Console.WriteLine($"----> Item NOT FOUND with ID: {context.Message.Id}");
                // List all items to debug
                var allItems = await DB.Find<Item>().ExecuteAsync();
                Console.WriteLine($"----> Total items in DB: {allItems.Count}");
                if (allItems.Count > 0)
                {
                    Console.WriteLine($"----> First item ID in DB: {allItems[0].ID}");
                }
                throw new MessageException(typeof(AuctionUpdated), $"Item with ID {context.Message.Id} not found in MongoDB");
            }
            
            Console.WriteLine($"----> Found existing item with model: {existingItem.Model}");
            
            var result = await DB.Update<Item>()
                .MatchID(context.Message.Id)
                .Modify(x => x.Make, context.Message.Make)
                .Modify(x => x.Model, context.Message.Model)
                .Modify(x => x.Year, context.Message.Year)
                .Modify(x => x.Color, context.Message.Color)
                .Modify(x => x.Mileage, context.Message.Mileage)
                .ExecuteAsync();
            
            Console.WriteLine($"----> Updated model from '{existingItem.Model}' to '{context.Message.Model}'");
                
            Console.WriteLine($"----> Update acknowledged: {result.IsAcknowledged}, Modified count: {result.ModifiedCount}");
                
            if (!result.IsAcknowledged)
            {
                throw new MessageException(typeof(AuctionUpdated), "Problem updating MongoDB");
            }
            
            Console.WriteLine($"----> Successfully processed update for auction {context.Message.Id}");
        }
    }
}
