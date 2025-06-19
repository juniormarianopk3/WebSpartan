using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;
using WebSpartan.Settings;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    { }

    public DbSet<Produto> Produtos { get; set; }
    public DbSet<Configuracoes> Configuracoes { get; set; }

}
