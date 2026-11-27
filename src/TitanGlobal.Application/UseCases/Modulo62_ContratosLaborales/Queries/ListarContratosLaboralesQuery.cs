using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Queries;

public class ListarContratosLaboralesQuery : IRequest<PaginaContratosLaboralesDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public string? Modalidad { get; set; }

    public int? EmpleadoId { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarContratosLaboralesQueryHandler : IRequestHandler<ListarContratosLaboralesQuery, PaginaContratosLaboralesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarContratosLaboralesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaContratosLaboralesDto> Handle(ListarContratosLaboralesQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        string modalidadNormalizada = request.Modalidad?.Trim() ?? string.Empty;
        ValidacionContratoLaboral.ValidarListado(request.Pagina, request.TamanoPagina, estadoNormalizado, modalidadNormalizada);
        DateOnly hoy = ValidacionContratoLaboral.Hoy();
        IQueryable<ContratoLaboral> consulta = _unidad.Repository<ContratoLaboral>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(contrato => contrato.Numero.ToLower().Contains(termino)
                || contrato.Cargo.ToLower().Contains(termino)
                || contrato.Empleado.Nombres.ToLower().Contains(termino)
                || contrato.Empleado.Apellidos.ToLower().Contains(termino)
                || contrato.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (modalidadNormalizada.Length > 0)
        {
            consulta = consulta.Where(contrato => contrato.Modalidad == modalidadNormalizada);
        }

        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(contrato => contrato.EmpleadoId == idEmpleado);
        }

        consulta = AplicarFiltroEstado(consulta, estadoNormalizado, hoy);
        int total = await consulta.CountAsync();
        List<ContratoLaboralListadoDto> items = await consulta
            .OrderByDescending(contrato => contrato.FechaInicio)
            .ThenByDescending(contrato => contrato.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(contrato => new ContratoLaboralListadoDto
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                EmpleadoId = contrato.EmpleadoId,
                EmpleadoLegajo = contrato.Empleado.Legajo,
                EmpleadoNombre = contrato.Empleado.Apellidos + ", " + contrato.Empleado.Nombres,
                Modalidad = contrato.Modalidad,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                SueldoBasico = contrato.SueldoBasico,
                Cargo = contrato.Cargo,
                Estado = contrato.Estado == ValidacionContratoLaboral.EstadoVigente
                    && contrato.FechaFin != null
                    && contrato.FechaFin < hoy
                    ? ValidacionContratoLaboral.EstadoVencido
                    : contrato.Estado
            })
            .ToListAsync();
        return new PaginaContratosLaboralesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static IQueryable<ContratoLaboral> AplicarFiltroEstado(
        IQueryable<ContratoLaboral> consulta,
        string estado,
        DateOnly hoy)
    {
        if (estado.Length == 0)
        {
            return consulta;
        }

        if (estado == ValidacionContratoLaboral.EstadoVigente)
        {
            return consulta.Where(contrato => contrato.Estado == ValidacionContratoLaboral.EstadoVigente
                && (contrato.FechaFin == null || contrato.FechaFin >= hoy));
        }

        if (estado == ValidacionContratoLaboral.EstadoVencido)
        {
            return consulta.Where(contrato => contrato.Estado == ValidacionContratoLaboral.EstadoVencido
                || (contrato.Estado == ValidacionContratoLaboral.EstadoVigente
                    && contrato.FechaFin != null
                    && contrato.FechaFin < hoy));
        }

        return consulta.Where(contrato => contrato.Estado == estado);
    }
}
