using LogisticaSatelite.WebComercial.Components;
using LogisticaSatelite.WebComercial.Services;

var builder = WebApplication.CreateBuilder(args);

// Adiciona suporte a Razor Components e Interatividade Server-side
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registro do cliente HTTP tipado para comunicação com a API Backend
builder.Services.AddHttpClient<IComercialApiService, ComercialApiService>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5283";
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();