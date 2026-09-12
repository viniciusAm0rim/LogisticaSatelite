namespace LogisticaSatelite.Api.Models
{
    public class Usuario
    {
        public int Id { get; set; } // Identificador único do usuário 
        public string User { get; set; } = string.Empty; // Matrícula de usuário para login
        public string Senha { get; set; } = string.Empty; // Senha de usuário para login
        public string Nome { get; set; } = string.Empty; // Nome completo do usuário
        public string Perfil { get; set; } = string.Empty; // Perfil do usuário (ex: Supervisor, Separador, Conferente, Hibrido)
    }

}


