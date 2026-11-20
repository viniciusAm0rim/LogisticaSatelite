using SpringApp.Models;

namespace SpringApp.Services;

public class MockAuthService : IAuthService
{
    private static readonly List<Usuario> _usuariosMock = new()
    {
        new Usuario { Id = 1, Nome = "Operador Separação", User = "separador@teste.com", Senha = "1234", Perfil = "Separador" },
        new Usuario { Id = 2, Nome = "Operador Conferência", User = "conferente@teste.com", Senha = "1234", Perfil = "Conferente" },
        new Usuario { Id = 3, Nome = "Vinicius Ferreira", User = "vinicius@teste.com", Senha = "1234", Perfil = "Hibrido" },
        new Usuario { Id = 4, Nome = "Gestão/Supervisor", User = "supervisor@teste.com", Senha = "1234", Perfil = "Supervisor" },
        new Usuario { Id = 5, Nome = "Área Técnica", User = "tecnica@teste.com", Senha = "1234", Perfil = "Area Tecnica" }
    };

    public async Task<(bool Sucesso, string Mensagem)> RealizarLoginAsync(string usuario, string senha)
    {
        await Task.Delay(150);

        var userMock = _usuariosMock.FirstOrDefault(u =>
            u.User.Equals(usuario.Trim(), StringComparison.OrdinalIgnoreCase) &&
            u.Senha == senha.Trim());

        if (userMock == null)
            return (false, "Usuário ou senha incorretos.");

        Preferences.Set("UsuarioId", userMock.Id);
        Preferences.Set("UsuarioNome", userMock.Nome.ToUpper());
        Preferences.Set("UsuarioUser", userMock.User);
        Preferences.Set("UsuarioPerfil", userMock.Perfil);

        return (true, "Sucesso");
    }
}