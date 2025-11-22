using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace SearchService.Consumer
{
    public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
    {
        private readonly IMapper _mapper;

        public AuctionDeletedConsumer(IMapper mapper)
        {
            _mapper = mapper;
            
        }
        public async Task Consume(ConsumeContext<AuctionDeleted> context)
        {
            var it = _mapper.Map<Models.Item>(context.Message); 
            await it.DeleteAsync();
        }
    }
}
