using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas;

public class AdministrarPlanillasUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarPlanillasUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaPlanillasDto> ListarAsync(int? anio, string? tipo, string? estado, int pagina, int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionPlanilla.ValidarListado(pagina, tamanoPagina);
        ValidacionPlanilla.ValidarFiltroTipo(tipoNormalizado);
        ValidacionPlanilla.ValidarFiltroEstado(estadoNormalizado);
        IQueryable<Planilla> consulta = _unidad.Repository<Planilla>().AsQueryable().AsNoTracking();
        if (anio is not null)
        {
            int anioFiltro = anio.Value;
            consulta = consulta.Where(planilla => planilla.Anio == anioFiltro);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(planilla => planilla.Tipo == tipoNormalizado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(planilla => planilla.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<PlanillaListadoDto> items = await consulta
            .OrderByDescending(planilla => planilla.Anio)
            .ThenByDescending(planilla => planilla.Mes)
            .ThenBy(planilla => planilla.Tipo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(planilla => new PlanillaListadoDto
            {
                Id = planilla.Id,
                Numero = planilla.Numero,
                Anio = planilla.Anio,
                Mes = planilla.Mes,
                Tipo = planilla.Tipo,
                Estado = planilla.Estado,
                Empleados = planilla.Empleados,
                Incidencias = planilla.Incidencias,
                TotalIngresos = planilla.TotalIngresos,
                TotalDescuentos = planilla.TotalDescuentos,
                TotalNeto = planilla.TotalNeto
            })
            .ToListAsync();
        return new PaginaPlanillasDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<ResumenPlanillasDto> ResumenAsync()
    {
        Planilla? ultima = await _unidad.Repository<Planilla>().AsQueryable()
            .AsNoTracking()
            .Where(planilla => planilla.Tipo == ValidacionPlanilla.TipoMensual)
            .OrderByDescending(planilla => planilla.Anio)
            .ThenByDescending(planilla => planilla.Mes)
            .FirstOrDefaultAsync();
        int pendientes = await _unidad.Repository<Planilla>().AsQueryable()
            .CountAsync(planilla => planilla.Estado == ValidacionPlanilla.EstadoBorrador || planilla.Estado == ValidacionPlanilla.EstadoCalculada);
        ResumenPlanillasDto resumen = new ResumenPlanillasDto { BorradoresYCalculadas = pendientes };
        if (ultima is not null)
        {
            resumen.UltimoPeriodo = $"{ultima.Anio}-{ultima.Mes:D2}";
            resumen.EstadoUltimo = ultima.Estado;
            resumen.NetoUltimo = ultima.TotalNeto;
            resumen.EmpleadosUltimo = ultima.Empleados;
            resumen.IncidenciasUltimo = ultima.Incidencias;
        }

        return resumen;
    }

    public async Task<ParametrosPlanillaDto> ObtenerParametrosAsync()
    {
        return new ParametrosPlanillaDto
        {
            Parametros = await _unidad.Repository<ParametroPlanilla>().AsQueryable()
                .AsNoTracking()
                .OrderBy(parametro => parametro.Clave)
                .Select(parametro => new ParametroPlanillaDto { Clave = parametro.Clave, Valor = parametro.Valor, Descripcion = parametro.Descripcion })
                .ToListAsync(),
            Pensiones = await _unidad.Repository<ParametroPension>().AsQueryable()
                .AsNoTracking()
                .OrderBy(parametro => parametro.Codigo)
                .Select(parametro => new ParametroPensionDto
                {
                    Codigo = parametro.Codigo,
                    Sistema = parametro.Sistema,
                    TasaAporte = parametro.TasaAporte,
                    TasaPrimaSeguro = parametro.TasaPrimaSeguro,
                    TasaComision = parametro.TasaComision,
                    RemuneracionMaximaAsegurable = parametro.RemuneracionMaximaAsegurable
                })
                .ToListAsync(),
            Tramos = await _unidad.Repository<TramoQuintaCategoria>().AsQueryable()
                .AsNoTracking()
                .OrderBy(tramo => tramo.Orden)
                .Select(tramo => new TramoQuintaDto { Orden = tramo.Orden, HastaUit = tramo.HastaUit, Tasa = tramo.Tasa })
                .ToListAsync()
        };
    }

    public async Task<PlanillaDetalleDto> ObtenerAsync(int id)
    {
        PlanillaDetalleDto? detalle = await _unidad.Repository<Planilla>().AsQueryable()
            .AsNoTracking()
            .Where(planilla => planilla.Id == id)
            .Select(planilla => new PlanillaDetalleDto
            {
                Id = planilla.Id,
                Numero = planilla.Numero,
                Anio = planilla.Anio,
                Mes = planilla.Mes,
                Tipo = planilla.Tipo,
                Estado = planilla.Estado,
                Empleados = planilla.Empleados,
                Incidencias = planilla.Incidencias,
                TotalIngresos = planilla.TotalIngresos,
                TotalDescuentos = planilla.TotalDescuentos,
                TotalNeto = planilla.TotalNeto,
                TotalAportesEmpleador = planilla.TotalAportesEmpleador,
                FechaCalculo = planilla.FechaCalculo,
                CalculadoPor = planilla.CalculadoPor,
                AprobadoPor = planilla.AprobadoPor,
                FechaAprobacion = planilla.FechaAprobacion,
                FechaPago = planilla.FechaPago
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionPlanilla.PlanillaInexistente(id);
        }

        return detalle;
    }

    public async Task<PaginaPlanillaEmpleadosDto> ListarDetallesAsync(int id, string? busqueda, bool soloIncidencias, int pagina, int tamanoPagina)
    {
        ValidacionPlanilla.ValidarListado(pagina, tamanoPagina);
        bool existe = await _unidad.Repository<Planilla>().AsQueryable().AnyAsync(planilla => planilla.Id == id);
        if (!existe)
        {
            throw ValidacionPlanilla.PlanillaInexistente(id);
        }

        IQueryable<PlanillaDetalle> consulta = _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .AsNoTracking()
            .Where(detalle => detalle.PlanillaId == id);
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(detalle => detalle.Empleado.Nombres.ToLower().Contains(termino)
                || detalle.Empleado.Apellidos.ToLower().Contains(termino)
                || detalle.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (soloIncidencias)
        {
            consulta = consulta.Where(detalle => detalle.Incidencia != null);
        }

        int total = await consulta.CountAsync();
        List<PlanillaEmpleadoDto> items = await consulta
            .OrderBy(detalle => detalle.EmpleadoId)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(detalle => new PlanillaEmpleadoDto
            {
                Id = detalle.Id,
                EmpleadoId = detalle.EmpleadoId,
                EmpleadoLegajo = detalle.Empleado.Legajo,
                EmpleadoNombre = detalle.Empleado.Apellidos + ", " + detalle.Empleado.Nombres,
                Cargo = detalle.Cargo,
                SueldoBasico = detalle.SueldoBasico,
                DiasContrato = detalle.DiasContrato,
                DiasSinGoce = detalle.DiasSinGoce,
                DiasFalta = detalle.DiasFalta,
                DiasLaborados = detalle.DiasLaborados,
                IngresoBasico = detalle.IngresoBasico,
                MinutosExtra = detalle.MinutosExtra,
                IngresoHorasExtra = detalle.IngresoHorasExtra,
                IngresoBonos = detalle.IngresoBonos,
                TotalIngresos = detalle.TotalIngresos,
                CodigoPension = detalle.CodigoPension,
                DescuentoPension = detalle.DescuentoPension,
                DescuentoQuinta = detalle.DescuentoQuinta,
                DescuentoPrestamos = detalle.DescuentoPrestamos,
                TotalDescuentos = detalle.TotalDescuentos,
                Neto = detalle.Neto,
                AporteEssalud = detalle.AporteEssalud,
                Incidencia = detalle.Incidencia,
                NumeroBoleta = detalle.NumeroBoleta
            })
            .ToListAsync();
        return new PaginaPlanillaEmpleadosDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<BoletaPagoDto> ObtenerBoletaAsync(int detalleId)
    {
        PlanillaDetalle? detalle = await _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .AsNoTracking()
            .Include(entidad => entidad.Planilla)
            .Include(entidad => entidad.Empleado)
            .SingleOrDefaultAsync(entidad => entidad.Id == detalleId);
        if (detalle is null)
        {
            throw ValidacionPlanilla.Error404($"El detalle de planilla {detalleId} no existe.");
        }

        if (detalle.NumeroBoleta is null)
        {
            throw ValidacionPlanilla.Error409("La boleta se genera al aprobar la planilla.");
        }

        return new BoletaPagoDto
        {
            Empresa = ValidacionPlanilla.EmpresaBoleta,
            NumeroBoleta = detalle.NumeroBoleta,
            NumeroPlanilla = detalle.Planilla.Numero,
            Tipo = detalle.Planilla.Tipo,
            Anio = detalle.Planilla.Anio,
            Mes = detalle.Planilla.Mes,
            EmpleadoId = detalle.EmpleadoId,
            EmpleadoLegajo = detalle.Empleado.Legajo,
            EmpleadoNombre = detalle.Empleado.Apellidos + ", " + detalle.Empleado.Nombres,
            Cargo = detalle.Cargo,
            CodigoPension = detalle.CodigoPension,
            SueldoBasico = detalle.SueldoBasico,
            DiasContrato = detalle.DiasContrato,
            DiasSinGoce = detalle.DiasSinGoce,
            DiasFalta = detalle.DiasFalta,
            DiasLaborados = detalle.DiasLaborados,
            IngresoBasico = detalle.IngresoBasico,
            MinutosExtra = detalle.MinutosExtra,
            IngresoHorasExtra = detalle.IngresoHorasExtra,
            IngresoBonos = detalle.IngresoBonos,
            TotalIngresos = detalle.TotalIngresos,
            DescuentoPension = detalle.DescuentoPension,
            DescuentoQuinta = detalle.DescuentoQuinta,
            DescuentoPrestamos = detalle.DescuentoPrestamos,
            TotalDescuentos = detalle.TotalDescuentos,
            AporteEssalud = detalle.AporteEssalud,
            Neto = detalle.Neto,
            FechaPago = detalle.Planilla.FechaPago
        };
    }

    public async Task<PlanillaCreadaDto> CrearAsync(CrearPlanillaRequestDto solicitud)
    {
        int anio = ValidacionPlanilla.ValidarAnio(solicitud.Anio);
        int mes = ValidacionPlanilla.ValidarMes(solicitud.Mes);
        string tipo = ValidacionPlanilla.ValidarTipo(solicitud.Tipo);
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

    public async Task<PlanillaIdDto> AprobarAsync(int id, string usuario)
    {
        Planilla planilla = await ValidacionPlanilla.ObtenerPlanillaAsync(_unidad, id);
        if (planilla.Estado != ValidacionPlanilla.EstadoCalculada)
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} debe estar calculada para aprobarse.");
        }

        List<PlanillaDetalle> incluidos = await _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .Where(detalle => detalle.PlanillaId == id && detalle.Incidencia == null)
            .ToListAsync();
        if (incluidos.Count == 0)
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} no tiene empleados calculados.");
        }

        foreach (PlanillaDetalle detalle in incluidos)
        {
            detalle.NumeroBoleta = ValidacionPlanilla.NumeroBoleta(planilla.Anio, planilla.Mes, planilla.Tipo, detalle.EmpleadoId);
        }

        planilla.Estado = ValidacionPlanilla.EstadoAprobada;
        planilla.AprobadoPor = usuario;
        planilla.FechaAprobacion = DateTime.UtcNow;
        await _unidad.CompleteAsync();
        return new PlanillaIdDto { Id = planilla.Id };
    }

    public async Task<PlanillaIdDto> PagarAsync(int id, PagarPlanillaRequestDto solicitud)
    {
        if (solicitud.FechaPago is null)
        {
            throw ValidacionPlanilla.Error400("La fecha de pago es obligatoria.");
        }

        Planilla planilla = await ValidacionPlanilla.ObtenerPlanillaAsync(_unidad, id);
        if (planilla.Estado != ValidacionPlanilla.EstadoAprobada)
        {
            throw ValidacionPlanilla.Error409($"La planilla {planilla.Numero} debe estar aprobada para registrar el pago.");
        }

        DateOnly aprobacion = DateOnly.FromDateTime(planilla.FechaAprobacion ?? DateTime.UtcNow);
        if (solicitud.FechaPago.Value < aprobacion)
        {
            throw ValidacionPlanilla.Error400("La fecha de pago no puede ser anterior a la aprobación.");
        }

        planilla.Estado = ValidacionPlanilla.EstadoPagada;
        planilla.FechaPago = solicitud.FechaPago.Value;
        await _unidad.CompleteAsync();
        return new PlanillaIdDto { Id = planilla.Id };
    }

    private static ReglaNegocioException CrearConflictoDuplicada(string tipo, int mes, int anio)
    {
        return ValidacionPlanilla.Error409($"Ya existe la planilla {tipo} del periodo {mes}/{anio}.");
    }
}
