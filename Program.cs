using ItemService.Data;
using ItemService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// Add DbContext with explicit connection string and retry policy
builder.Services.AddDbContext<ItemDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? 
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    );
});

// Register RabbitMQService as Singleton
builder.Services.AddSingleton<RabbitMQService>();

var app = builder.Build();

// Apply migrations at startup with retry logic
using (var scope = app.Services.CreateScope())
{
    var retryCount = 0;
    const int maxRetries = 10;
    
    while (retryCount < maxRetries)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ItemDbContext>();
            db.Database.Migrate();
            break;
        }
        catch (Exception)
        {
            retryCount++;
            if (retryCount == maxRetries)
                throw;
                
            Thread.Sleep(2000); // Wait 2 seconds before retrying
        }
    }
}

// Configure the HTTP request pipeline.
app.MapGrpcService<ItemServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
