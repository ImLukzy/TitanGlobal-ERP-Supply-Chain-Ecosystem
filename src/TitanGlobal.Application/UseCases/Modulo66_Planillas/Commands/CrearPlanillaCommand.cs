using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class CrearPlanillaCommand : IRequest<PlanillaCreadaDto>
{
    public required CrearPlanillaRequestDto Solicitud { get; set; }
}

public class CrearPlanillaCommandHandler : IRequestHandler<CrearPlanillaCommand, PlanillaCreadaDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearPlanillaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PlanillaCreadaDto> Handle(CrearPlanillaCommand request, CancellationToken cancellationToken)
    {
        int anio = ValidacionPlanilla.ValidarAnio(request.Solicitud.Anio);
        int mes = ValidacionPlanilla.ValidarMes(request.Solicitud.Mes);
        string tipo = ValidacionPlanilla.ValidarTipo(request.Solicitud.Tipo);
        DateOnly hoy = ValidacionPlanilla.Hoy();
        if (anio * 12 + mes > hoy.Year * 12 + hoy.Month)
        {
            throw ValidacionPlanilla.Error400("No se puede crear una planilla de un periodo futuro.");
        }

        bool existe = await _unidad.Repository<Planilla>().AsQueryable()
            .AnyAsync(planilla => planilla.Anio == anio && planilla.Mes == mes && planilla.Tipo == tipo);
        if (existe)
        {
            throw CrearConflictoDuplicada(tipo, mes, anio);
        }

        if (tipo == ValidacionPlanilla.TipoComplementaria)
        {
            bool mensualCerrada = await _unidad.Repository<Planilla>().AsQueryable()
                .AnyAsync(planilla => planilla.Anio == anio
                    && planilla.Mes == mes
                    && planilla.Tipo == ValidacionPlanilla.TipoMensual
                    && (planilla.Estado == ValidacionPlanilla.EstadoAprobada || planilla.Estado == ValidacionPlanilla.EstadoPagada));
            if (!mensualCerrada)
            {
                throw ValidacionPlanilla.Error409($"La planilla complementaria necesita la planilla mensual de {mes}/{anio} aprobada.");
            }
        }

        Planilla planilla = new Planilla
        {
            Numero = ValidacionPlanilla.NumeroPlanilla(anio, mes, tipo),
            Anio = anio,
            Mes = mes,
            Tipo = tipo,
            Estado = ValidacionPlanilla.EstadoBorrador
        };
        await _unidad.Repository<Planilla>().AddAsync(planilla);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoDuplicada(tipo, mes, anio);
        }

        return new PlanillaCreadaDto { Id = planilla.Id, Numero = planilla.Numero };
    }

    private static ReglaNegocioException CrearConflictoDuplicada(string tipo, int mes, int anio)
    {
        return ValidacionPlanilla.Error409($"Ya existe la planilla {tipo} del periodo {mes}/{anio}.");
    }
}
