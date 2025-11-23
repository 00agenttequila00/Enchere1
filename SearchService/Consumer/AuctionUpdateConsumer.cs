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
            Console.WriteLine("----> Consuming Update Message");
            var item = _mapper.Map<Item>(context.Message);
            
            var result = await DB.Update<Item>()
                .Match(a => a.ID == context.Message.Id)
                .ModifyOnly(x => new
                {
                    x.Make,
                    x.Model,
                    x.Mileage,
                    x.Color,
                    x.Year
                }, item)
                .ExecuteAsync();
                
            if (!result.IsAcknowledged)
            {
                throw new MessageException(typeof(AuctionUpdated), "Problem updating MongoDB");
            }
        }
    }
}
