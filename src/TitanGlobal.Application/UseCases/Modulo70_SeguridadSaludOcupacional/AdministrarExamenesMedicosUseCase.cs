using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

public class AdministrarExamenesMedicosUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarExamenesMedicosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaExamenesMedicosDto> ListarAsync(
        int? empleadoId,
        string? tipo,
        string? resultado,
        bool? vencidos,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        string resultadoNormalizado = resultado?.Trim() ?? string.Empty;
        ValidacionSst.ValidarPagina(pagina, tamanoPagina);
        ValidacionSst.ValidarTipoExamenFiltro(tipoNormalizado);
        ValidacionSst.ValidarResultadoFiltro(resultadoNormalizado);
        DateOnly hoy = ValidacionSst.Hoy();
        IQueryable<ExamenMedico> consulta = _unidad.Repository<ExamenMedico>().AsQueryable();
        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
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

        if (vencidos is not null)
        {
            if (vencidos.Value)
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
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ExamenMedicoIdDto> RegistrarAsync(RegistrarExamenMedicoRequestDto solicitud)
    {
        ExamenMedico examen = ValidacionSst.ValidarExamen(solicitud);
        Empleado empleado = await ValidacionSst.ObtenerEmpleadoAsync(_unidad, examen.EmpleadoId);
        if (empleado.Estado != ValidacionSst.EmpleadoActivo)
        {
            if (examen.Tipo != ValidacionSst.ExamenRetiro)
            {
                throw ValidacionSst.EmpleadoCesado(empleado.Id);
            }

            if (empleado.FechaCese is null
                || examen.Fecha > empleado.FechaCese.Value.AddDays(ValidacionSst.DiasRetiroTrasCese))
            {
                throw ValidacionSst.Error409($"El examen de retiro se registra hasta 30 días después del cese del empleado {empleado.Id}.");
            }
        }

        await _unidad.Repository<ExamenMedico>().AddAsync(examen);
        await _unidad.CompleteAsync();
        return new ExamenMedicoIdDto { Id = examen.Id };
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
