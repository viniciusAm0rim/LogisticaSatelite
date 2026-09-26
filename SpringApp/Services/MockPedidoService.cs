using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpringApp.Models;

namespace SpringApp.Services;

public class MockPedidoService
{
    // ====================================================================================
    // MEMÓRIA VIRTUAL: Guarda as etiquetas reais bipadas na Conferência para a Expedição
    // ====================================================================================
    public static Dictionary<int, List<string>> CodigosDosVolumes = new Dictionary<int, List<string>>();

    // ====================================================================================
    // BANCO DE DADOS FALSO (Mock)
    // ====================================================================================
    private static List<Pedido> _pedidosMock = new List<Pedido>
    {
        new Pedido
 {
     Id = 0001,
     Destinatario = "PROTOTIPO TESTE",
     Modal = "RETIRA",
     Status = "Aguardando",
     Volumes = 0,
     SepUser = null, ConfUser = null, ExpUser = null,
     Itens = new List<ItemPedido>
     {
         new ItemPedido
         {
             Id = 1, CodItem = "001", Nome = "CCBI10/2-5MXA",
             LocalPicking = "R1-A-01", Quantidade = 2, BipagensSeparacao = 0, BipagensConferencia = 0
         },
         new ItemPedido
         {
             Id = 2, CodItem = "002", Nome = "CPLI10/2-5MXB",
             LocalPicking = "R2-A-02", Quantidade = 1, BipagensSeparacao = 0, BipagensConferencia = 0
         }
     }
 }
    };

    public async Task<List<Pedido>> GetPedidosAsync()
    {
        await Task.Delay(300); // Simula o tempo de carregamento da internet
        return _pedidosMock;
    }

    public async Task<List<Pedido>> ObterPorStatusAsync(string status)
    {
        await Task.Delay(300);
        return _pedidosMock.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Pedido> GetPedidoPorIdAsync(int id)
    {
        await Task.Delay(200);
        return _pedidosMock.FirstOrDefault(p => p.Id == id);
    }

    public async Task<(bool Sucesso, string Mensagem)> BloquearPedidoAsync(int id, string usuario)
    {
        await Task.Delay(200);
        return (true, string.Empty);
    }

    public async Task<(bool Sucesso, string Mensagem)> AvancarEtapaAsync(int id, string etapaAtual, string usuario = null)
    {
        await Task.Delay(500);
        var mock = _pedidosMock.FirstOrDefault(p => p.Id == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        DateTime dataHoraAtual = DateTime.Now;

        if (mock.Status == "Aguardando")
        {
            mock.Status = "Em Separaçao";
        }
        else if (mock.Status == "Em Separaçao")
        {
            mock.Status = "Aguardando Conferencia";
            mock.SepUser = usuario;
            mock.SepData = dataHoraAtual;
        }
        else if (mock.Status == "Aguardando Conferencia")
        {
            mock.Status = "Em Conferencia";
        }
        else if (mock.Status == "Aguardando Expediçao")
        {
            mock.Status = "Expedido";
            mock.ExpUser = usuario;
            mock.ExpData = dataHoraAtual;
        }

        return (true, "Sucesso");
    }

    // ====================================================================================
    // CONCLUIR CONFERÊNCIA (Recebe a lista de códigos reais colados nas caixas)
    // ====================================================================================
    public async Task<(bool Sucesso, string Mensagem)> ConcluirConferenciaAsync(int id, List<string> codigosVolumes, string usuario)
    {
        await Task.Delay(500);
        var mock = _pedidosMock.FirstOrDefault(p => p.Id == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        mock.Status = "Aguardando Expediçao";
        mock.Volumes = codigosVolumes.Count; // O número de caixas é o tamanho da lista bipada
        mock.ConfUser = usuario;
        mock.ConfData = DateTime.Now;

        // Salva os códigos reais no dicionário em memória para a Expedição ler depois
        CodigosDosVolumes[id] = codigosVolumes;

        return (true, "Sucesso");
    }

    public async Task<(bool Sucesso, string Mensagem)> ReverterEtapaAsync(int id)
    {
        await Task.Delay(300);
        var mock = _pedidosMock.FirstOrDefault(p => p.Id == id);
        if (mock == null) return (false, "Pedido não encontrado.");

        if (mock.Status == "Em Separaçao")
            mock.Status = "Aguardando";
        else if (mock.Status == "Em Conferencia")
            mock.Status = "Aguardando Conferencia";

        return (true, "Sucesso");
    }
}


