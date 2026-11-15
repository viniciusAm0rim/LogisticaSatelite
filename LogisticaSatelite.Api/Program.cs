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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Captura requisições 404 e devolve detalhes no console e no JSON
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var respostaDetalhada = new
        {
            erro = "Rota não encontrada (404)",
            motivo = "A URL ou o método HTTP enviado pelo cliente não existe nesta API.",
            urlTentada = context.Request.Path.Value,
            metodoHttp = context.Request.Method,
            queryParameters = context.Request.QueryString.Value
        };

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(
            respostaDetalhada,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine($"\n[ERRO 404 CAPTURADO] {context.Request.Method} {context.Request.Path.Value}{context.Request.QueryString.Value}\n");

        await context.Response.WriteAsync(jsonResponse);
    }
});

app.UseAuthorization();
app.MapControllers();

app.Run();