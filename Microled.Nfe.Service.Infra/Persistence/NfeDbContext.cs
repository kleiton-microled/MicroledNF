using Microled.Nfe.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microled.Nfe.Service.Infra.Persistence;

public sealed class NfeDbContext : DbContext
{
    public NfeDbContext(DbContextOptions<NfeDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NfeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
