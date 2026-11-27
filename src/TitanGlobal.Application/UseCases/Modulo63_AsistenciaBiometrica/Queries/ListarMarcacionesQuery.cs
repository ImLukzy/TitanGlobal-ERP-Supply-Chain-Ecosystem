using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Queries;

public class ListarMarcacionesQuery : IRequest<PaginaMarcacionesDto>
{
    public int? EmpleadoId { get; set; }

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public string? Tipo { get; set; }

    public string? Origen { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarMarcacionesQueryHandler : IRequestHandler<ListarMarcacionesQuery, PaginaMarcacionesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarMarcacionesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaMarcacionesDto> Handle(ListarMarcacionesQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        string origenNormalizado = request.Origen?.Trim() ?? string.Empty;
        ValidacionMarcacion.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        ValidacionMarcacion.ValidarFiltrosMarcaciones(request.Desde, request.Hasta, tipoNormalizado, origenNormalizado);
        IQueryable<Marcacion> consulta = _unidad.Repository<Marcacion>().AsQueryable();
        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(marcacion => marcacion.EmpleadoId == idEmpleado);
        }

        if (request.Desde is not null)
        {
            DateTime inicio = CalculadoraJornada.LocalAUtc(request.Desde.Value.ToDateTime(TimeOnly.MinValue));
            consulta = consulta.Where(marcacion => marcacion.Instante >= inicio);
        }

        if (request.Hasta is not null)
        {
            DateTime fin = CalculadoraJornada.LocalAUtc(request.Hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            consulta = consulta.Where(marcacion => marcacion.Instante < fin);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(marcacion => marcacion.Tipo == tipoNormalizado);
        }

        if (origenNormalizado.Length > 0)
        {
            consulta = consulta.Where(marcacion => marcacion.Origen == origenNormalizado);
        }

        int total = await consulta.CountAsync();
        List<MarcacionDto> items = await consulta
            .OrderByDescending(marcacion => marcacion.Instante)
            .ThenByDescending(marcacion => marcacion.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(marcacion => new MarcacionDto
            {
                Id = marcacion.Id,
                EmpleadoId = marcacion.EmpleadoId,
                EmpleadoLegajo = marcacion.Empleado.Legajo,
                EmpleadoNombre = marcacion.Empleado.Apellidos + ", " + marcacion.Empleado.Nombres,
                Tipo = marcacion.Tipo,
                Instante = marcacion.Instante,
                Origen = marcacion.Origen,
                Dispositivo = marcacion.Dispositivo,
                NumeroRegistro = marcacion.NumeroRegistro,
                Observacion = marcacion.Observacion
            })
            .ToListAsync();
        return new PaginaMarcacionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
