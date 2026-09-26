using System.Net.Http.Json;
using SpringApp.Models;

namespace SpringApp.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

#if ANDROID
    private const string BaseHost = "https://10.0.2.2:44379/api/usuarios";
#else    
    private const string BaseHost = "https://localhost:44379/api/usuarios";
#endif

    public AuthService()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
        _httpClient = new HttpClient(handler);
    }

    public async Task<(bool Sucesso, string Mensagem)> RealizarLoginAsync(string usuario, string senha)
    {
        try
        {
            var payload = new { User = usuario, Senha = senha };
            var response = await _httpClient.PostAsJsonAsync($"{BaseHost}/login", payload);

            if (response.IsSuccessStatusCode)
            {
                var userDb = await response.Content.ReadFromJsonAsync<Usuario>();

                if (userDb != null)
                {
                    // Grava os dados vindos diretamente do SQLite
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
