using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService.Consumer
{
    public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
    {
        private readonly IMapper _mapper;

        public AuctionCreatedConsumer(IMapper mapper)
        {
            _mapper = mapper;
            
        }
        public async Task Consume(ConsumeContext<AuctionCreated> context)
        {
            Console.WriteLine($"----> Consuming AuctionCreated for auction: {context.Message.Id}");
            Console.WriteLine($"----> Model: {context.Message.Model}, Make: {context.Message.Make}");
            
            var item = _mapper.Map<Models.Item>(context.Message);
            
            Console.WriteLine($"----> Mapped Item ID: {item.ID}");
            
            await item.SaveAsync();
            
            Console.WriteLine($"----> Successfully saved item to MongoDB with ID: {item.ID}");
        }
    }
}
