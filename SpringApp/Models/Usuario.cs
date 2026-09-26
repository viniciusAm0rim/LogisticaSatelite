namespace SpringApp.Models;

public class Usuario
{
    public int Id { get; set; }
    public string User { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
}