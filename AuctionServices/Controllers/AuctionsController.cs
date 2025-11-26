using AuctionServices.Data;
using AuctionServices.DTO;
using AuctionServices.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionServices.Controllers
{
    [ApiController]
    [Route("api/auctions")]
    public class AuctionsController : ControllerBase
    {
        private readonly AuctionDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuctionsController(AuctionDbContext context, IMapper mapper , IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;

        }
        [HttpGet]
        public async Task<ActionResult<List<AuctionDTO>>> GetAllAuctions(string date)
        {
            var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();
            //var auction = await _context.Auctions.Include(x => x.Item).OrderBy(x => x.Item.Make).ToListAsync();
            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime())>0);

            }
            return await query.ProjectTo<AuctionDTO>(_mapper.ConfigurationProvider).ToListAsync();
            //return _mapper.Map<List<AuctionDTO>>(auction);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDTO>> GetAuctionById(Guid id)
        {
            var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);
            if (auction == null)
            {
                return NotFound();
            }
            return _mapper.Map<AuctionDTO>(auction);
        }
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<AuctionDTO>> CreateAuction(CreateAuctionDTO auctiondto)
        {
            var auction = _mapper.Map<Auction>(auctiondto);
            auction.Seller = User.Identity.Name;
            _context.Auctions.Add(auction);
            var newAuction = _mapper.Map<AuctionDTO>(auction);
            
            var createEvent = _mapper.Map<AuctionCreated>(newAuction);
            Console.WriteLine($"Publishing AuctionCreated event for auction {createEvent.Id} with model: {createEvent.Model}");
            await _publishEndpoint.Publish(createEvent);
            
            var result = await _context.SaveChangesAsync() > 0;
            
            if (!result)
            {
                return BadRequest("Could not Save changes to DB");
            }
            return CreatedAtAction(nameof(GetAuctionById), new { auction.Id },newAuction);
        }
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAuction(Guid Id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await _context.Auctions.Include(x =>x.Item).FirstOrDefaultAsync(x => x.Id == Id);
            if (auction == null)
            {
                return NotFound();
            }
            if(auction.Seller != User.Identity.Name)
            {
                return Forbid();//Badrequest()
            }
            auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
            
            var updateEvent = _mapper.Map<AuctionUpdated>(auction);
            Console.WriteLine($"Publishing update event for auction {updateEvent.Id} with model: {updateEvent.Model}");
            await _publishEndpoint.Publish(updateEvent);
            
            var result = await _context.SaveChangesAsync() > 0;
            if (result)return Ok();
            
            return BadRequest();
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await _context.Auctions.FindAsync(id);
            
            // Check for null FIRST
            if (auction == null)
            {
                return NotFound();
            }
            if(auction.Seller != User.Identity.Name)
            {
                return Forbid();
            }

            // Now safe to use auction properties
            await _publishEndpoint.Publish<AuctionDeleted>(new { Id = id });
            
            _context.Auctions.Remove(auction);
            var result = await _context.SaveChangesAsync() > 0;
            
            if (result)
            {
                return Ok();  // ← Returns 200 OK with NO BODY
            }
            
            return BadRequest("Couldn't update DB");



        }
    }
}
