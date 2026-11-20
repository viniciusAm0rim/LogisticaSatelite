using Microsoft.EntityFrameworkCore;
using LogisticaSatelite.Api.Models;

namespace LogisticaSatelite.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<ItemPedido> ItensPedidos { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Produto> Produtos { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Transportadora> Transportadoras { get; set; } = null!;
        public DbSet<VolumePedido> VolumesPedidos { get; set; } = null!;
        public DbSet<LogOperacional> LogsOperacionais { get; set; } = null!;
        public DbSet<RegistroPausa> RegistrosPausas { get; set; } = null!;
        public DbSet<Cotacao> Cotacoes { get; set; } = null!;
        public DbSet<ItemCotacao> ItensCotacoes { get; set; } = null!;
        public DbSet<Devolucao> Devolucoes { get; set; } = null!;
        public DbSet<ItemDevolucao> ItensDevolucao { get; set; } = null!;
        public DbSet<ProdutoAvariado> ProdutosAvariados { get; set; } = null!;
    }
}