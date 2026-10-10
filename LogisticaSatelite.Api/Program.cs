using LogisticaSatelite.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Força o Kestrel a escutar na placa de rede (0.0.0.0) nas portas HTTP (5283) e HTTPS (7063)
builder.WebHost.UseUrls("http://0.0.0.0:5283", "https://0.0.0.0:7063");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurações do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Desativado temporariamente para permitir acesso HTTP direto via IP da rede (192.168.15.12)
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();