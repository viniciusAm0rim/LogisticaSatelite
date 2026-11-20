namespace SpringApp.Services;

public interface IAuthService
{
    Task<(bool Sucesso, string Mensagem)> RealizarLoginAsync(string usuario, string senha);
}