using Microled.Nfe.Service.Application.DTOs.NotasFiscais;
using Microled.Nfe.Service.Application.DTOs.Tomadores;
using Microled.Nfe.Service.Application.Interfaces.Tomadores;
using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Interfaces;

namespace Microled.Nfe.Service.Application.UseCases.Tomadores;

public sealed class SearchTomadoresUseCase : ISearchTomadoresUseCase
{
    private readonly ITomadorRepository _repository;

    public SearchTomadoresUseCase(ITomadorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<PagedTomadorResponse>> ExecuteAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.SearchAsync(query, page, pageSize, cancellationToken);
        return ApiResponse<PagedTomadorResponse>.Ok(new PagedTomadorResponse
        {
            Items = items.Select(TomadorMapper.ToResponse).ToList(),
            TotalCount = total,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200)
        });
    }
}

public sealed class GetTomadorByCpfCnpjUseCase : IGetTomadorByCpfCnpjUseCase
{
    private readonly ITomadorRepository _repository;

    public GetTomadorByCpfCnpjUseCase(ITomadorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<TomadorResponse>> ExecuteAsync(string cpfCnpj, CancellationToken cancellationToken)
    {
        var tomador = await _repository.GetByCpfCnpjAsync(cpfCnpj, cancellationToken);
        if (tomador is null)
        {
            return ApiResponse<TomadorResponse>.Fail("Tomador not found.");
        }

        return ApiResponse<TomadorResponse>.Ok(TomadorMapper.ToResponse(tomador));
    }
}

public sealed class CreateTomadorUseCase : ICreateTomadorUseCase
{
    private readonly ITomadorRepository _repository;

    public CreateTomadorUseCase(ITomadorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<TomadorResponse>> ExecuteAsync(
        TomadorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _repository.GetByCpfCnpjAsync(request.CpfCnpj, cancellationToken);
            if (existing is not null)
            {
                return ApiResponse<TomadorResponse>.Fail("Já existe um tomador com este CPF/CNPJ.");
            }

            var tomador = Tomador.Create(
                request.CpfCnpj,
                request.RazaoSocial,
                request.InscricaoMunicipal,
                request.InscricaoEstadual,
                request.Email,
                request.TipoLogradouro,
                request.Logradouro,
                request.Numero,
                request.Complemento,
                request.Bairro,
                request.Uf,
                request.CodigoMunicipio,
                request.Cep);

            await _repository.AddAsync(tomador, cancellationToken);
            return ApiResponse<TomadorResponse>.Ok(TomadorMapper.ToResponse(tomador));
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<TomadorResponse>.Fail(ex.Message);
        }
    }
}

public sealed class UpdateTomadorUseCase : IUpdateTomadorUseCase
{
    private readonly ITomadorRepository _repository;

    public UpdateTomadorUseCase(ITomadorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<TomadorResponse>> ExecuteAsync(
        Guid id,
        TomadorRequest request,
        CancellationToken cancellationToken)
    {
        var tomador = await _repository.GetByIdAsync(id, cancellationToken);
        if (tomador is null)
        {
            return ApiResponse<TomadorResponse>.Fail("Tomador not found.");
        }

        var digits = Tomador.NormalizeCpfCnpj(request.CpfCnpj);
        if (digits.Length > 0 && !string.Equals(digits, tomador.CpfCnpj, StringComparison.Ordinal))
        {
            var other = await _repository.GetByCpfCnpjAsync(digits, cancellationToken);
            if (other is not null && other.Id != tomador.Id)
            {
                return ApiResponse<TomadorResponse>.Fail("Já existe um tomador com este CPF/CNPJ.");
            }
        }

        try
        {
            tomador.Update(
                request.RazaoSocial,
                request.InscricaoMunicipal,
                request.InscricaoEstadual,
                request.Email,
                request.TipoLogradouro,
                request.Logradouro,
                request.Numero,
                request.Complemento,
                request.Bairro,
                request.Uf,
                request.CodigoMunicipio,
                request.Cep);
            await _repository.UpdateAsync(tomador, cancellationToken);
            return ApiResponse<TomadorResponse>.Ok(TomadorMapper.ToResponse(tomador));
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<TomadorResponse>.Fail(ex.Message);
        }
    }
}

public sealed class DeleteTomadorUseCase : IDeleteTomadorUseCase
{
    private readonly ITomadorRepository _repository;

    public DeleteTomadorUseCase(ITomadorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ApiResponse<bool>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return ApiResponse<bool>.Fail("Tomador not found.");
        }

        return ApiResponse<bool>.Ok(true);
    }
}
