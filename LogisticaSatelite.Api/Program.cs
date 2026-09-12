using LogisticaSatelite.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
// Usa SQLite como banco de dados
//builder.Services.AddDbContext<AppDbContext>: Registra o contexto no container de injeção de dependências da API.
//options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")): Informa ao Entity Framework Core que o motor do banco de dados escolhido é o SQLite.
//builder.Configuration.GetConnectionString("DefaultConnection"): Obtém a string de conexão do banco de dados a partir do arquivo appsettings.json, onde "DefaultConnection" é o nome da configuração de conexão.

// Configurações necessárias para o Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Cria a aplicação web com base nas configurações definidas no builder.

app.UseExceptionHandler(_ => { });
// Configura o middleware de tratamento de exceções para capturar erros não tratados e fornecer respostas apropriadas.


// Ativa a interface visual do Swagger apenas em ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();