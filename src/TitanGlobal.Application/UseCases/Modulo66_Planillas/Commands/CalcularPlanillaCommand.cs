using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Commands;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class CalcularPlanillaCommand : IRequest<PlanillaCalculoDto>
{
    public int Id { get; set; }

    public required string Usuario { get; set; }
}

public class CalcularPlanillaCommandHandler : IRequestHandler<CalcularPlanillaCommand, PlanillaCalculoDto>
{
    private readonly IUnitOfWork _unidad;

    private readonly ISender _sender;

    public CalcularPlanillaCommandHandler(
        ISender sender,
        IUnitOfWork unidad)
    {
        _unidad = unidad;
        _sender = sender;
    }

    public async Task<PlanillaCalculoDto> Handle(CalcularPlanillaCommand request, CancellationToken cancellationToken)
    {
        Planilla planilla = await ValidacionPlanilla.ObtenerPlanillaAsync(_unidad, request.Id);
        if (ValidacionPlanilla.EstaCerrada(planilla.Estado))
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} está aprobada y no se modifica.");
        }

        ParametrosRemuneracion parametros = await ValidacionPlanilla.LeerParametrosAsync(_unidad);
        Dictionary<int, PensionRemuneracion> pensiones = await LeerPensionesAsync();
        bool esMensual = planilla.Tipo == ValidacionPlanilla.TipoMensual;
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await DeshacerCalculoAnteriorAsync(planilla);
        await _unidad.CompleteAsync();

        List<BonoPlanilla> bonos = await _unidad.Repository<BonoPlanilla>().AsQueryable()
            .Include(bono => bono.Empleado)
            .Where(bono => bono.Anio == planilla.Anio && bono.Mes == planilla.Mes && bono.TipoPlanilla == planilla.Tipo)
            .ToListAsync();
        List<PlanillaDetalle> detalles = esMensual
            ? await CalcularMensualAsync(planilla, parametros, pensiones, bonos)
            : await CalcularComplementariaAsync(planilla, parametros, pensiones, bonos);
        foreach (PlanillaDetalle detalle in detalles)
        {
            planilla.Detalles.Add(detalle);
        }

        List<PlanillaDetalle> incluidos = detalles.Where(detalle => detalle.Incidencia is null).ToList();
        planilla.Estado = ValidacionPlanilla.EstadoCalculada;
        planilla.Empleados = incluidos.Count;
        planilla.Incidencias = detalles.Count - incluidos.Count;
        planilla.TotalIngresos = incluidos.Sum(detalle => detalle.TotalIngresos);
        planilla.TotalDescuentos = incluidos.Sum(detalle => detalle.TotalDescuentos);
        planilla.TotalNeto = incluidos.Sum(detalle => detalle.Neto);
        planilla.TotalAportesEmpleador = incluidos.Sum(detalle => detalle.AporteEssalud);
        planilla.FechaCalculo = DateTime.UtcNow;
        planilla.CalculadoPor = request.Usuario;
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new PlanillaCalculoDto { Id = planilla.Id, Empleados = planilla.Empleados, Incidencias = planilla.Incidencias };
    }

    private const string TipoAusenciaSinGoce = "PermisoSinGoce";

    private const string EstadoAusenciaAprobada = "Aprobada";

    private const string EstadoJornadaFalta = "Falta";

    private const string EstadoHorasExtraAprobada = "Aprobada";

    // Revierte los descuentos de préstamos de la planilla, borra sus detalles y suelta los bonos.
    private async Task DeshacerCalculoAnteriorAsync(Planilla planilla)
    {
        await _sender.Send(new RevertirDescuentosCommand { PlanillaId = planilla.Id });
        List<PlanillaDetalle> anteriores = await _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .Where(detalle => detalle.PlanillaId == planilla.Id)
            .ToListAsync();
        foreach (PlanillaDetalle anterior in anteriores)
        {
            _unidad.Repository<PlanillaDetalle>().Remove(anterior);
        }

        List<BonoPlanilla> asignados = await _unidad.Repository<BonoPlanilla>().AsQueryable()
            .Where(bono => bono.PlanillaId == planilla.Id)
            .ToListAsync();
        foreach (BonoPlanilla bono in asignados)
        {
            bono.PlanillaId = null;
        }
    }

    private async Task<Dictionary<int, PensionRemuneracion>> LeerPensionesAsync()
    {
        List<AfiliacionPension> afiliaciones = await _unidad.Repository<AfiliacionPension>().AsQueryable()
            .AsNoTracking()
            .Include(afiliacion => afiliacion.ParametroPension)
            .ToListAsync();
        return afiliaciones.ToDictionary(
            afiliacion => afiliacion.EmpleadoId,
            afiliacion => new PensionRemuneracion
            {
                Codigo = afiliacion.ParametroPension.Codigo,
                Sistema = afiliacion.ParametroPension.Sistema,
                TasaAporte = afiliacion.ParametroPension.TasaAporte,
                TasaPrimaSeguro = afiliacion.ParametroPension.TasaPrimaSeguro,
                TasaComision = afiliacion.ParametroPension.TasaComision,
                RemuneracionMaximaAsegurable = afiliacion.ParametroPension.RemuneracionMaximaAsegurable
            });
    }

    private async Task<List<PlanillaDetalle>> CalcularMensualAsync(
        Planilla planilla,
        ParametrosRemuneracion parametros,
        Dictionary<int, PensionRemuneracion> pensiones,
        List<BonoPlanilla> bonos)
    {
        DateOnly inicioMes = new DateOnly(planilla.Anio, planilla.Mes, 1);
        DateOnly finMes = new DateOnly(planilla.Anio, planilla.Mes, DateTime.DaysInMonth(planilla.Anio, planilla.Mes));
        List<ContratoLaboral> contratos = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .AsNoTracking()
            .Include(contrato => contrato.AdendasContrato)
            .Include(contrato => contrato.Empleado)
            .Where(contrato => contrato.Estado != ValidacionPlanilla.EstadoContratoAnulado && contrato.FechaInicio <= finMes)
            .ToListAsync();
        List<ContratoLaboral> candidatos = contratos
            .Where(contrato => (contrato.FechaCierre ?? contrato.FechaFin ?? DateOnly.MaxValue) >= inicioMes)
            .ToList();
        Dictionary<int, int> faltas = await LeerFaltasAsync(inicioMes, finMes);
        Dictionary<int, List<int>> horasExtra = await LeerHorasExtraAsync(inicioMes, finMes);
        Dictionary<int, int> sinGoce = await LeerDiasSinGoceAsync(inicioMes, finMes);
        List<PlanillaDetalle> detalles = new List<PlanillaDetalle>();
        foreach (IGrouping<int, ContratoLaboral> grupo in candidatos.GroupBy(contrato => contrato.EmpleadoId).OrderBy(grupo => grupo.Key))
        {
            int empleadoId = grupo.Key;
            DateOnly? fechaCese = grupo.First().Empleado.FechaCese;
            CoberturaContratos.Cobertura? cobertura = CoberturaContratos.Calcular(grupo, planilla.Anio, planilla.Mes, fechaCese);
            if (cobertura is null)
            {
                continue;
            }

            List<BonoPlanilla> bonosEmpleado = bonos.Where(bono => bono.EmpleadoId == empleadoId).ToList();
            DatosRemuneracion datos = new DatosRemuneracion
            {
                SueldoBasico = cobertura.SueldoBasico,
                DiasContrato = cobertura.DiasContrato,
                DiasSinGoce = sinGoce.TryGetValue(empleadoId, out int diasSinGoce) ? diasSinGoce : 0,
                DiasFalta = faltas.TryGetValue(empleadoId, out int diasFalta) ? diasFalta : 0,
                MinutosExtraPorJornada = horasExtra.TryGetValue(empleadoId, out List<int>? minutos) ? minutos : new List<int>(),
                IngresoBonos = bonosEmpleado.Sum(bono => bono.Monto),
                Pension = pensiones.TryGetValue(empleadoId, out PensionRemuneracion? pension) ? pension : null
            };
            ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, parametros);
            if (!resultado.Excluido)
            {
                decimal descontado = await _sender.Send(new DescontarCuotasCommand
                {
                    EmpleadoId = empleadoId,
                    Anio = planilla.Anio,
                    Mes = planilla.Mes,
                    MontoMaximo = resultado.SaldoParaPrestamos,
                    PlanillaId = planilla.Id
                });
                CalculadoraRemuneracion.AplicarPrestamos(resultado, descontado);
                foreach (BonoPlanilla bono in bonosEmpleado)
                {
                    bono.PlanillaId = planilla.Id;
                }
            }

            detalles.Add(CrearDetalle(empleadoId, cobertura.Contrato.Id, cobertura.Cargo, datos, resultado));
        }

        return detalles;
    }

    // Complementaria: solo los bonos registrados para ella, con su pensión y quinta; sin básico, horas extra ni préstamos.
    private Task<List<PlanillaDetalle>> CalcularComplementariaAsync(
        Planilla planilla,
        ParametrosRemuneracion parametros,
        Dictionary<int, PensionRemuneracion> pensiones,
        List<BonoPlanilla> bonos)
    {
        List<PlanillaDetalle> detalles = new List<PlanillaDetalle>();
        foreach (IGrouping<int, BonoPlanilla> grupo in bonos.GroupBy(bono => bono.EmpleadoId).OrderBy(grupo => grupo.Key))
        {
            int empleadoId = grupo.Key;
            DatosRemuneracion datos = new DatosRemuneracion
            {
                IngresoBonos = grupo.Sum(bono => bono.Monto),
                Pension = pensiones.TryGetValue(empleadoId, out PensionRemuneracion? pension) ? pension : null
            };
            ResultadoRemuneracion resultado = CalculadoraRemuneracion.Calcular(datos, parametros);
            if (!resultado.Excluido)
            {
                foreach (BonoPlanilla bono in grupo)
                {
                    bono.PlanillaId = planilla.Id;
                }
            }

            detalles.Add(CrearDetalle(empleadoId, null, grupo.First().Empleado.Cargo, datos, resultado));
        }

        return Task.FromResult(detalles);
    }

    private static PlanillaDetalle CrearDetalle(int empleadoId, int? contratoId, string cargo, DatosRemuneracion datos, ResultadoRemuneracion resultado)
    {
        return new PlanillaDetalle
        {
            EmpleadoId = empleadoId,
            ContratoLaboralId = contratoId,
            Cargo = cargo,
            SueldoBasico = datos.SueldoBasico,
            DiasContrato = datos.DiasContrato,
            DiasSinGoce = datos.DiasSinGoce,
            DiasFalta = datos.DiasFalta,
            DiasLaborados = resultado.DiasLaborados,
            IngresoBasico = resultado.IngresoBasico,
            MinutosExtra = resultado.MinutosExtra,
            IngresoHorasExtra = resultado.IngresoHorasExtra,
            IngresoBonos = resultado.IngresoBonos,
            TotalIngresos = resultado.TotalIngresos,
            CodigoPension = resultado.CodigoPension,
            DescuentoPension = resultado.DescuentoPension,
            DescuentoQuinta = resultado.DescuentoQuinta,
            DescuentoPrestamos = resultado.DescuentoPrestamos,
            TotalDescuentos = resultado.TotalDescuentos,
            Neto = resultado.Neto,
            AporteEssalud = resultado.AporteEssalud,
            Incidencia = resultado.Incidencia
        };
    }

    // Días con jornada en estado Falta (módulo 63).
    private async Task<Dictionary<int, int>> LeerFaltasAsync(DateOnly inicio, DateOnly fin)
    {
        List<JornadaAsistencia> jornadas = await _unidad.Repository<JornadaAsistencia>().AsQueryable()
            .AsNoTracking()
            .Where(jornada => jornada.Fecha >= inicio && jornada.Fecha <= fin && jornada.Estado == EstadoJornadaFalta)
            .ToListAsync();
        return jornadas
            .GroupBy(jornada => jornada.EmpleadoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(jornada => jornada.Fecha).Distinct().Count());
    }

    // Minutos extra aprobados de cada jornada (módulo 63).
    private async Task<Dictionary<int, List<int>>> LeerHorasExtraAsync(DateOnly inicio, DateOnly fin)
    {
        List<JornadaAsistencia> jornadas = await _unidad.Repository<JornadaAsistencia>().AsQueryable()
            .AsNoTracking()
            .Where(jornada => jornada.Fecha >= inicio
                && jornada.Fecha <= fin
                && jornada.EstadoHorasExtra == EstadoHorasExtraAprobada
                && jornada.MinutosExtra > 0)
            .ToListAsync();
        return jornadas
            .GroupBy(jornada => jornada.EmpleadoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(jornada => jornada.MinutosExtra).ToList());
    }

    // Días naturales del mes cubiertos por permisos sin goce aprobados (módulo 65).
    private async Task<Dictionary<int, int>> LeerDiasSinGoceAsync(DateOnly inicio, DateOnly fin)
    {
        List<Ausencia> ausencias = await _unidad.Repository<Ausencia>().AsQueryable()
            .AsNoTracking()
            .Where(ausencia => ausencia.Tipo == TipoAusenciaSinGoce
                && ausencia.Estado == EstadoAusenciaAprobada
                && ausencia.FechaInicio <= fin
                && ausencia.FechaFin >= inicio)
            .ToListAsync();
        return ausencias
            .GroupBy(ausencia => ausencia.EmpleadoId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Sum(ausencia => DiasEnElMes(ausencia, inicio, fin)));
    }

    private static int DiasEnElMes(Ausencia ausencia, DateOnly inicio, DateOnly fin)
    {
        DateOnly desde = ausencia.FechaInicio > inicio ? ausencia.FechaInicio : inicio;
        DateOnly hasta = ausencia.FechaFin < fin ? ausencia.FechaFin : fin;
        return hasta.DayNumber - desde.DayNumber + 1;
    }
}
