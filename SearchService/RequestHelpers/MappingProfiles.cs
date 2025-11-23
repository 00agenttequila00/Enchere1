using SearchService.Models;
using AutoMapper;
using Contracts;

namespace SearchService.RequestHelpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<AuctionCreated, Item>()
                .ForMember(d => d.ID, o => o.MapFrom(s => s.Id.ToString()));
            CreateMap<AuctionUpdated, Item>()
                .ForMember(d => d.ID, o => o.MapFrom(s => s.Id));
        }
    }
}
