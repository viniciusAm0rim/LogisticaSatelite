using LogisticaSatelite.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticaSatelite.Api.Data
{
    public class AppDbContext : DbContext 
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
            //pega as configurações do banco (qual banco usar e a senha/endereço da conexão) entrega para o Entity Framework Core para que ele consiga abrir o banco e rodar suas tabelas.
        {
        }
        public DbSet<Pedido> Pedidos { get; set; } = null!; 
        // Transforma a entidade/tabela Pedido em uma coleção de objetos do tipo Pedido, permitindo realizar operações de CRUD (Create, Read, Update, Delete) no banco de dados.
        public DbSet<ItemPedido> ItensPedidos { get; set; } = null!;
        // Transforma a entidade/tabela ItemPedido em uma coleção de objetos do tipo ItemPedido, permitindo realizar operações de CRUD (Create, Read, Update, Delete) no banco de dados.                                                      
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        // Transforma a entidade/tabela Usuario em uma coleção de objetos do tipo Usuario, permitindo realizar operações de CRUD (Create, Read, Update, Delete) no banco de dados. 



    }
}
