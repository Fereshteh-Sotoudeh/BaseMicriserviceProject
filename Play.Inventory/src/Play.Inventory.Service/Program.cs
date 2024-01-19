using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Play.Inventory.Service;
using Play.Inventory.Service.Entities;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Inventory.Service.Clients;
using Polly;
using Polly.Timeout;
using Play.Common.MassTransit;



var builder = WebApplication.CreateBuilder(args);

ConfigurationManager Configuration = builder.Configuration;
//ServiceSettings serviceSettings;
// Add services to the container.

//serviceSettings = Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var servicePrivider = builder.Services.BuildServiceProvider();
builder.Services.AddMongo()
.AddMongoRepository<InventoryItem>("inventoryitems")
.AddMongoRepository<CatalogItem>("CatalogItems")
.AddMassTransitWithRabbitMq();
AddCatalogClient(builder, servicePrivider);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();

static void AddCatalogClient(WebApplicationBuilder builder, ServiceProvider servicePrivider)
{
    Random jitterer = new Random();
    builder.Services.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7209");
    })
    .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
        5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
        onRetry: (outcome, timespan, retryAttempt) =>
        {
            servicePrivider.GetService<ILogger<CatalogClient>>()
            .LogWarning($"Delaying for {timespan.TotalSeconds} secounds,then making rety {retryAttempt}");
        }
    ))
    .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
        3,
        TimeSpan.FromSeconds(15),
        onBreak: (outcome, timespan) =>
        {
            servicePrivider.GetService<ILogger<CatalogClient>>()
     .LogWarning($"Opening the circuit for {timespan.TotalSeconds} secounds...");

        },
        onReset: () =>
        {
            servicePrivider.GetService<ILogger<CatalogClient>>()?
            .LogWarning($"Closing the circuit");
        }
    ))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
    builder.Services.AddControllers(options =>
    {
        options.SuppressAsyncSuffixInActionNames = false;
    }

    );
}