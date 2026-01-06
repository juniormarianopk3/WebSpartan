using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;

namespace WebSpartan
{
    // Herdar de IdentityDbContext<ApplicationUser> para incluir todas as tabelas do Identity
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ===============================
        // Tabelas do projeto
        // ===============================

        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Configuracoes> Configuracoes { get; set; }
        public DbSet<UserAddress> Enderecos { get; set; }

        // (Descomente conforme for implementar)
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }
        // public DbSet<CashbackTransaction> CashbackTransacoes { get; set; }

        public DbSet<PixOrder> PixOrders { get; set; }
        public DbSet<FreteEstado> FretesEstado { get; set; }
        public DbSet<CashbackMovimento> CashbackMovimentos { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Importante: mantém as tabelas do Identity funcionando
            base.OnModelCreating(builder);

            // Relação Usuário → Endereços
            builder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany(u => u.Enderecos)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relação Pedido → Usuário
           builder.Entity<Pedido>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
           
            // Relação Pedido → Itens
            builder.Entity<ItemPedido>()
                .HasOne(i => i.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(i => i.PedidoId);
            
            // Relação Pedido → Endereço de entrega
            builder.Entity<Pedido>()
                .HasOne(p => p.EnderecoEntrega)
                .WithMany()
                .HasForeignKey(p => p.EnderecoEntregaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relação Cashback → Usuário
           /* builder.Entity<CashbackTransaction>()
                .HasOne(c => c.User)
                .WithMany(u => u.Cashbacks)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);*/
        }
    }
}
