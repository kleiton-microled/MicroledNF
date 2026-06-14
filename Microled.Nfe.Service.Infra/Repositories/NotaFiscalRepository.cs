using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;
using Microled.Nfe.Service.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Microled.Nfe.Service.Infra.Repositories;

public sealed class NotaFiscalRepository : INotaFiscalRepository
{
    private readonly NfeDbContext _dbContext;

    public NotaFiscalRepository(NfeDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken)
    {
        await _dbContext.NotasFiscais.AddAsync(notaFiscal, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<NotaFiscal?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.NotasFiscais.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<NotaFiscal?> GetByNumeroNotaAsync(string numeroNota, CancellationToken cancellationToken)
    {
        return _dbContext.NotasFiscais
            .FirstOrDefaultAsync(x => x.NumeroNota == numeroNota, cancellationToken);
    }

    public Task<NotaFiscal?> GetByProtocoloAsync(string protocolo, CancellationToken cancellationToken)
    {
        return _dbContext.NotasFiscais
            .FirstOrDefaultAsync(x => x.Protocolo == protocolo, cancellationToken);
    }

    public async Task<IReadOnlyList<NotaFiscal>> ListByProtocoloAsync(string protocolo, CancellationToken cancellationToken)
    {
        return await _dbContext.NotasFiscais
            .Where(x => x.Protocolo == protocolo)
            .ToListAsync(cancellationToken);
    }

    public Task<NotaFiscal?> GetByRpsAsync(
        string inscricaoPrestador,
        string serieRps,
        string numeroRps,
        CancellationToken cancellationToken)
    {
        return _dbContext.NotasFiscais
            .FirstOrDefaultAsync(
                x => x.InscricaoPrestador == inscricaoPrestador
                     && x.SerieRps == serieRps
                     && x.NumeroRps == numeroRps,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<NotaFiscal> Items, int TotalCount)> SearchAsync(
        NotaFiscalSearchFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.NotasFiscais.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Protocolo))
        {
            query = query.Where(x => x.Protocolo == filter.Protocolo);
        }

        if (!string.IsNullOrWhiteSpace(filter.NumeroNota))
        {
            query = query.Where(x => x.NumeroNota == filter.NumeroNota);
        }

        if (!string.IsNullOrWhiteSpace(filter.NumeroRps))
        {
            query = query.Where(x => x.NumeroRps == filter.NumeroRps);
        }

        if (!string.IsNullOrWhiteSpace(filter.SerieRps))
        {
            query = query.Where(x => x.SerieRps == filter.SerieRps);
        }

        if (!string.IsNullOrWhiteSpace(filter.InscricaoPrestador))
        {
            query = query.Where(x => x.InscricaoPrestador == filter.InscricaoPrestador);
        }

        if (!string.IsNullOrWhiteSpace(filter.CnpjPrestador))
        {
            query = query.Where(x => x.CnpjPrestador == filter.CnpjPrestador);
        }

        if (!string.IsNullOrWhiteSpace(filter.CpfCnpjTomador))
        {
            query = query.Where(x => x.CpfCnpjTomador == filter.CpfCnpjTomador);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.DataEmissaoInicio.HasValue)
        {
            query = query.Where(x => x.DataEmissao >= filter.DataEmissaoInicio);
        }

        if (filter.DataEmissaoFim.HasValue)
        {
            query = query.Where(x => x.DataEmissao <= filter.DataEmissaoFim);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 100);

        var items = await query
            .OrderByDescending(x => x.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken)
    {
        _dbContext.NotasFiscais.Update(notaFiscal);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var nota = await _dbContext.NotasFiscais.FindAsync([id], cancellationToken);
        if (nota is null)
        {
            return false;
        }

        _dbContext.NotasFiscais.Remove(nota);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
