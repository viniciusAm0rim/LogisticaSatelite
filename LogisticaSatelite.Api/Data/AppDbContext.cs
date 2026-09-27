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
        public DbSet<Produto> Produtos { get; set; } = null!; // <--- Adicionado aqui
        public DbSet<Cliente> Clientes { get; set; } = null!; // <-- Adicionar
        public DbSet<Transportadora> Transportadoras { get; set; } = null!; // <-- Adicionar
        public DbSet<VolumePedido> VolumesPedidos { get; set; }
        public DbSet<LogOperacional> LogsOperacionais { get; set; } = null!;
    }
}