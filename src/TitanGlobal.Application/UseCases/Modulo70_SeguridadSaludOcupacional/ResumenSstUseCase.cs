using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

public class ResumenSstUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenSstUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenSstDto> ResumenAsync(int? anio)
    {
        int anioConsulta = ValidacionSst.ResolverAnio(anio);
        DateOnly hoy = ValidacionSst.Hoy();
        DateOnly inicio = new DateOnly(anioConsulta, 1, 1);
        DateOnly fin = new DateOnly(anioConsulta, 12, 31);
        IQueryable<IncidenteSst> incidentes = _unidad.Repository<IncidenteSst>().AsQueryable();
        IQueryable<IncidenteSst> delAnio = incidentes.Where(incidente => incidente.Fecha >= inicio && incidente.Fecha <= fin);
        IQueryable<ExamenMedico> examenes = _unidad.Repository<ExamenMedico>().AsQueryable();
        int diasBaja = await delAnio.SumAsync(incidente => (int?)incidente.DiasBaja) ?? 0;
        int noAptos = await examenes
            .Where(examen => examen.Resultado == ValidacionSst.ResultadoNoApto
                && examen.Empleado.Estado == ValidacionSst.EmpleadoActivo
                && !examenes.Any(otro => otro.EmpleadoId == examen.EmpleadoId
                    && (otro.Fecha > examen.Fecha || (otro.Fecha == examen.Fecha && otro.Id > examen.Id))))
            .Select(examen => examen.EmpleadoId)
            .Distinct()
            .CountAsync();
        return new ResumenSstDto
        {
            Anio = anioConsulta,
            IncidentesAbiertos = await incidentes.CountAsync(incidente => incidente.Estado == ValidacionSst.EstadoAbierto),
            IncidentesAnio = await delAnio.CountAsync(),
            AccidentesConBaja = await delAnio.CountAsync(incidente => incidente.Tipo == ValidacionSst.IncidenteAccidenteConBaja),
            DiasBajaAnio = diasBaja,
            EntregasEppVencidas = await _unidad.Repository<EntregaEpp>().AsQueryable()
                .CountAsync(entrega => entrega.FechaVencimiento < hoy),
            ExamenesVencidos = await examenes
                .CountAsync(examen => examen.FechaVencimiento != null && examen.FechaVencimiento < hoy),
            EmpleadosNoAptos = noAptos
        };
    }
}
