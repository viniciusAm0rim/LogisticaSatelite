namespace LogisticaSatelite.Api.Models
{
    public class LogOperacional
    {
        public int Id { get; set; }
        public int IdPedido { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;

        public DateTime? HoraInicioSeparacao { get; set; }
        public DateTime? HoraFimSeparacao { get; set; }
        public DateTime? HoraInicioConferencia { get; set; }
        public DateTime? HoraFimConferencia { get; set; }
        public DateTime? HoraInicioExpedicao { get; set; }
        public DateTime? HoraFimExpedicao { get; set; }
        public DateTime? HoraCancelamento { get; set; }

        // --- ERROS SEPARADOS POR ETAPA ---
        public int ErrosEvitadosSep { get; set; } = 0;
        public int ErrosEvitadosConf { get; set; } = 0;
        public int ErrosEvitadosExp { get; set; } = 0;
    }
}