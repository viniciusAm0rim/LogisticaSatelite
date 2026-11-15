using LogisticaSatelite.WebComercial.Models;

namespace LogisticaSatelite.WebComercial.Services.Interfaces;

public record ResultadoDashboardAdm(bool Sucesso, AdmDashboardDto Dados, string? MensagemErro = null);

public interface IAdmService
{
    Task<ResultadoDashboardAdm> ObterDashboardAsync(string funcionario = "Todos");
}
