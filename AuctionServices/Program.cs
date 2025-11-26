using AuctionServices.Data;
//using Enchere.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

using AutoMapper;
using System.Reflection;
using AuctionServices.RequestHelpers;
using MassTransit;
using Polly;
using Microsoft.AspNetCore.Authentication.JwtBearer;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(options => {
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(10);
        o.UsePostgres();
        o.UseBusOutbox();
    });
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

/*builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
*/
/*builder.Services.AddAutoMapper(typeof(Program));
*/
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<MappingProfiles>();
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityServiceUrl"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.NameClaimType = "username";
    });
    
var app = builder.Build();

// Configure the HTTP request pipeline.
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetService<AuctionDbContext>();
    
    if (context == null)
    {
        throw new Exception("Cannot get DbContext from service provider");
    }

    Console.WriteLine("Starting database initialization...");
    await context.Database.MigrateAsync();
    
    if (!context.Auctions.Any())
    {
        Console.WriteLine("No data exists - seeding database...");
        DbInitializer.InitDb(app);
        Console.WriteLine("Seeding complete!");
    }
    else
    {
        Console.WriteLine("Database already contains data - skipping seeding");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred while initializing the database: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
