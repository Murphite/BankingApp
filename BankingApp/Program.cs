using BankingApp.API.Extensions;
using BankingApp.Domain.Models;
using BankingApp.Domain.Utility;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbServices(builder.Configuration);
builder.Services.AddServices(builder.Configuration);


// Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Set logging level
    .WriteTo.Console()    // Output to console
    .WriteTo.File("Logs/bankingapp-.log", rollingInterval: RollingInterval.Day)    
    .Enrich.FromLogContext()
    .CreateLogger();

// Replace default logging with Serilog
builder.Host.UseSerilog();


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "BankingApp_";
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BankingApp API", Version = "v1" });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
await SeedRolesAsync(services);

app.Run();


static async Task SeedRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = new[] { "Admin", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}