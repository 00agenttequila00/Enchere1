using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumer
{
    public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
    {
        private readonly IMapper _mapper;

        
        public async Task Consume(ConsumeContext<AuctionDeleted> context)
        {
            var result = await DB.DeleteAsync<Item>(context.Message.ID);
            if(!result.IsAcknowledged)
            {
                throw new MessageException(typeof(AuctionDeleted), "Problem deleting from MongoDB");
            }
        }
    }
}
