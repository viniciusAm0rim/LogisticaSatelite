using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services.Sessao;

public class SessaoStateService
{
    public SessaoUsuario Atual { get; private set; } = new();

    public bool Autenticado => Atual.Autenticado;
    public bool UsarMock => Atual.UsarMock;
    public PerfilAcesso Perfil => Atual.Perfil;
    public string NomeUsuario => Atual.Login;

    public bool TentarAutenticar(string usuario, string senha)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
            return false;

        var userNorm = usuario.Trim().ToLowerInvariant();
        var passNorm = senha.Trim();

        if (passNorm != "1234")
            return false;

        SessaoUsuario? novaSessao = userNorm switch
        {
            "comercial" => new SessaoUsuario { Login = "Comercial", Perfil = PerfilAcesso.Comercial, UsarMock = false },
            "adm" => new SessaoUsuario { Login = "ADM", Perfil = PerfilAcesso.Adm, UsarMock = false },
            "comercialmock" => new SessaoUsuario { Login = "Comercialmock", Perfil = PerfilAcesso.Comercial, UsarMock = true },
            "admmock" => new SessaoUsuario { Login = "Admmock", Perfil = PerfilAcesso.Adm, UsarMock = true },
            _ => null
        };

        if (novaSessao == null)
            return false;

        Atual = novaSessao;
        return true;
    }

    public void EncerrarSessao()
    {
        Atual = new SessaoUsuario();
    }
}