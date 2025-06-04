using SimpleTranslationService.Services;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
  var configuration = provider.GetRequiredService<IConfiguration>();
  var connectionString = configuration.GetConnectionString("Redis") ??
                        configuration["Redis:ConnectionString"] ??
                        "localhost:6379";

  return ConnectionMultiplexer.Connect(connectionString);
});

// Register cache service
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Register translation service as singleton to maintain a single instance
builder.Services.AddSingleton<TranslationService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
    policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
  });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();