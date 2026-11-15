using LogisticaSatelite.WebComercial.Components;
using LogisticaSatelite.WebComercial.Services.Interfaces;
using LogisticaSatelite.WebComercial.Services.Mock;
using LogisticaSatelite.WebComercial.Services.Producao;
using LogisticaSatelite.WebComercial.Services.Sessao;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5283";

// Estado da sessão do usuário autenticado (Produção vs. Mock)
builder.Services.AddScoped<SessaoStateService>();

// Serviços de Produção (comunicação real com a API)
builder.Services.AddHttpClient<ProducaoPedidosService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<ProducaoAdmService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Serviços Mock (100% em memória, zero chamadas de rede)
builder.Services.AddScoped<MockDataStore>();
builder.Services.AddScoped<MockPedidosService>();
builder.Services.AddScoped<MockAdmService>();

// Resolução dinâmica da fonte de dados conforme o login ativo
builder.Services.AddScoped<IPedidosService>(sp =>
{
    var sessao = sp.GetRequiredService<SessaoStateService>();
    return sessao.UsarMock
        ? sp.GetRequiredService<MockPedidosService>()
        : sp.GetRequiredService<ProducaoPedidosService>();
});

builder.Services.AddScoped<IAdmService>(sp =>
{
    var sessao = sp.GetRequiredService<SessaoStateService>();
    return sessao.UsarMock
        ? sp.GetRequiredService<MockAdmService>()
        : sp.GetRequiredService<ProducaoAdmService>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();