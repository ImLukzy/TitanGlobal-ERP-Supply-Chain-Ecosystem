using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Queries;

public class ListarCentrosTrabajoQuery : IRequest<PaginaCentrosTrabajoDto>
{
    public string? Busqueda { get; set; }

    public string? Planta { get; set; }

    public string? Tipo { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarCentrosTrabajoQueryHandler : IRequestHandler<ListarCentrosTrabajoQuery, PaginaCentrosTrabajoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCentrosTrabajoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaCentrosTrabajoDto> Handle(ListarCentrosTrabajoQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<CentroTrabajo> consulta = _unitOfWork.Repository<CentroTrabajo>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(centro =>
                centro.Codigo.ToLower().Contains(busquedaNormalizada)
                || centro.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        string plantaNormalizada = request.Planta?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(plantaNormalizada))
        {
            consulta = consulta.Where(centro => centro.Planta.ToLower() == plantaNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(request.Tipo))
        {
            string tipoNormalizado = ValidacionCentroTrabajo.NormalizarTipo(request.Tipo);
            consulta = consulta.Where(centro => centro.Tipo == tipoNormalizado);
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(centro => centro.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<CentroTrabajoListadoDto> items = await consulta
            .OrderBy(centro => centro.Codigo)
            .ThenBy(centro => centro.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
            .Select(centro => new CentroTrabajoListadoDto
            {
                Id = centro.Id,
                Codigo = centro.Codigo,
                Nombre = centro.Nombre,
                Tipo = centro.Tipo,
                Planta = centro.Planta,
                CapacidadHorasDia = centro.CapacidadHorasDia,
                Activo = centro.Activo
            })
            .ToListAsync();

        return new PaginaCentrosTrabajoDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
