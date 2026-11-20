using System.Net.Http.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool Sucesso, string Mensagem)> RealizarLoginAsync(string usuario, string senha)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var payload = new { User = usuario, Senha = senha };
            var response = await _httpClient.PostAsJsonAsync("api/usuarios/login", payload, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var userDb = await response.Content.ReadFromJsonAsync<Usuario>(cancellationToken: cts.Token);

                if (userDb != null)
                {
                    Preferences.Set("UsuarioId", userDb.Id);
                    Preferences.Set("UsuarioNome", userDb.Nome.ToUpper());
                    Preferences.Set("UsuarioUser", userDb.User);
                    Preferences.Set("UsuarioPerfil", userDb.Perfil);

                    return (true, "Sucesso");
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return (false, "Usuário ou senha incorretos.");

            return (false, $"Erro servidor: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"Conexão falhou: {ex.Message}");
        }
    }

    public static void RealizarLogout()
    {
        Preferences.Clear();
    }
}