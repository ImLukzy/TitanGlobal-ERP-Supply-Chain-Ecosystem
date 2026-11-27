using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

public class ListarExamenesMedicosQuery : IRequest<PaginaExamenesMedicosDto>
{
    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public string? Resultado { get; set; }

    public bool? Vencidos { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarExamenesMedicosQueryHandler : IRequestHandler<ListarExamenesMedicosQuery, PaginaExamenesMedicosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarExamenesMedicosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaExamenesMedicosDto> Handle(ListarExamenesMedicosQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        string resultadoNormalizado = request.Resultado?.Trim() ?? string.Empty;
        ValidacionSst.ValidarPagina(request.Pagina, request.TamanoPagina);
        ValidacionSst.ValidarTipoExamenFiltro(tipoNormalizado);
        ValidacionSst.ValidarResultadoFiltro(resultadoNormalizado);
        DateOnly hoy = ValidacionSst.Hoy();
        IQueryable<ExamenMedico> consulta = _unidad.Repository<ExamenMedico>().AsQueryable();
        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(examen => examen.EmpleadoId == idEmpleado);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(examen => examen.Tipo == tipoNormalizado);
        }

        if (resultadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(examen => examen.Resultado == resultadoNormalizado);
        }

        if (request.Vencidos is not null)
        {
            if (request.Vencidos.Value)
            {
                consulta = consulta.Where(examen => examen.FechaVencimiento != null && examen.FechaVencimiento < hoy);
            }
            else
            {
                consulta = consulta.Where(examen => examen.FechaVencimiento == null || examen.FechaVencimiento >= hoy);
            }
        }

        int total = await consulta.CountAsync();
        List<ExamenMedico> examenes = await consulta
            .Include(examen => examen.Empleado)
            .OrderByDescending(examen => examen.Fecha)
            .ThenByDescending(examen => examen.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<ExamenMedicoDto> items = new List<ExamenMedicoDto>();
        foreach (ExamenMedico examen in examenes)
        {
            items.Add(Convertir(examen, hoy));
        }

        return new PaginaExamenesMedicosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static ExamenMedicoDto Convertir(ExamenMedico examen, DateOnly hoy)
    {
        return new ExamenMedicoDto
        {
            Id = examen.Id,
            EmpleadoId = examen.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(examen.Empleado),
            Tipo = examen.Tipo,
            Fecha = examen.Fecha,
            Resultado = examen.Resultado,
            Restricciones = examen.Restricciones,
            FechaVencimiento = examen.FechaVencimiento,
            Clinica = examen.Clinica,
            Vencido = examen.FechaVencimiento is not null && examen.FechaVencimiento.Value < hoy
        };
    }
}
