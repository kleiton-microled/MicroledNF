using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Microled.Nfe.Service.Infra.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public sealed class NfeDbContextFactory : IDesignTimeDbContextFactory<NfeDbContext>
{
    public NfeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NfeDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("NFE_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=DB_NFE;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new NfeDbContext(optionsBuilder.Options);
    }
}
