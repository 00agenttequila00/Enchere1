using AuctionServices.DTO;
using AuctionServices.Models;
using AutoMapper;
using Contracts;
namespace AuctionServices.RequestHelpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            CreateMap<Auction, AuctionDTO>().IncludeMembers(x => x.Item);
            CreateMap<Item, AuctionDTO>();
            CreateMap<CreateAuctionDTO, Auction>().ForMember(d=>d.Item, o=>o.MapFrom(s=>s));
            CreateMap<CreateAuctionDTO,Item >();
            CreateMap<AuctionDTO, AuctionCreated>();

        }

    }
}
