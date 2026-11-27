using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo42_Conductores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo42_Conductores.Queries;

public class ListarConductoresQuery : IRequest<PaginaConductoresDto>
{
    public string? Busqueda { get; set; }

    public bool? Habilitado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarConductoresQueryHandler : IRequestHandler<ListarConductoresQuery, PaginaConductoresDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarConductoresQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaConductoresDto> Handle(ListarConductoresQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Conductor> consulta = _unitOfWork.Repository<Conductor>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLower();
            consulta = consulta.Where(conductor =>
                conductor.Nombre.ToLower().Contains(termino) ||
                conductor.DocumentoLicencia.ToLower().Contains(termino));
        }

        if (request.Habilitado.HasValue)
        {
            consulta = consulta.Where(conductor => conductor.Habilitado == request.Habilitado.Value);
        }

        int total = await consulta.CountAsync();

        List<ConductorListadoDto> items = await consulta
            .OrderBy(conductor => conductor.DocumentoLicencia)
            .ThenBy(conductor => conductor.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(conductor => new ConductorListadoDto
            {
                Id = conductor.Id,
                Nombre = conductor.Nombre,
                DocumentoLicencia = conductor.DocumentoLicencia,
                VencimientoLicencia = conductor.VencimientoLicencia,
                Habilitado = conductor.Habilitado,
                Activo = conductor.Activo
            })
            .ToListAsync();

        return new PaginaConductoresDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
