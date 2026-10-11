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
// Captura requisições que não corresponderam a nenhum endpoint (404) e devolve um JSON com o motivo
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var respostaDetalhada = new
        {
            erro = "Rota não encontrada (404)",
            motivo = "A URL ou o método HTTP enviado pelo aplicativo não existe nesta API.",
            urlTentada = context.Request.Path.Value,
            metodoHttp = context.Request.Method,
            queryParameters = context.Request.QueryString.Value,
            sugestao = "Verifique se a URL no SpringApp está escrita corretamente e se o ID do pedido está a ser passado na rota ou no corpo."
        };

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(respostaDetalhada, jsonOptions);

        // Exibe no terminal da API para depuração imediata
        Console.WriteLine($"\n[ERRO 404 CAPTURADO]");
        Console.WriteLine($"-> Método: {context.Request.Method}");
        Console.WriteLine($"-> URL: {context.Request.Path.Value}");
        Console.WriteLine($"-> Query: {context.Request.QueryString.Value}\n");

        await context.Response.WriteAsync(jsonResponse);
    }
});

app.Run();