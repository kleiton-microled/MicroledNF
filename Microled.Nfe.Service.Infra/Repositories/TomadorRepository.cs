using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Microled.Nfe.Service.Infra.Repositories;

public sealed class TomadorRepository : ITomadorRepository
{
    private readonly NfeDbContext _dbContext;

    public TomadorRepository(NfeDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(Tomador tomador, CancellationToken cancellationToken)
    {
        await _dbContext.Tomadores.AddAsync(tomador, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Tomador tomador, CancellationToken cancellationToken)
    {
        _dbContext.Tomadores.Update(tomador);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tomador = await _dbContext.Tomadores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tomador is null)
        {
            return false;
        }

        _dbContext.Tomadores.Remove(tomador);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<Tomador?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Tomadores.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Tomador?> GetByCpfCnpjAsync(string cpfCnpj, CancellationToken cancellationToken)
    {
        var digits = Tomador.NormalizeCpfCnpj(cpfCnpj);
        if (digits.Length == 0)
        {
            return Task.FromResult<Tomador?>(null);
        }

        return _dbContext.Tomadores.FirstOrDefaultAsync(x => x.CpfCnpj == digits, cancellationToken);
    }

    public async Task<(IReadOnlyList<Tomador> Items, int TotalCount)> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);

        var source = _dbContext.Tomadores.AsQueryable();
        var term = query?.Trim() ?? string.Empty;
        if (term.Length > 0)
        {
            var digits = Tomador.NormalizeCpfCnpj(term);
            var like = $"%{term.ToLower()}%";
            source = source.Where(x =>
                (digits.Length >= 3 && x.CpfCnpj.Contains(digits))
                || x.RazaoSocial.ToLower().Contains(term.ToLower())
                || EF.Functions.Like(x.RazaoSocial.ToLower(), like));
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source
            .OrderBy(x => x.RazaoSocial)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
