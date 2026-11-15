namespace LogisticaSatelite.WebComercial.Models;

public enum PerfilAcesso
{
    Nenhum,
    Comercial,
    Adm
}

public class SessaoUsuario
{
    public string Login { get; set; } = string.Empty;
    public PerfilAcesso Perfil { get; set; } = PerfilAcesso.Nenhum;
    public bool UsarMock { get; set; }
    public bool Autenticado => Perfil != PerfilAcesso.Nenhum;
}
