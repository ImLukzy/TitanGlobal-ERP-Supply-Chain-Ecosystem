using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos;

public class AdministrarPrestamosUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarPrestamosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaPrestamosDto> ListarAsync(
        string? busqueda,
        int? empleadoId,
        string? tipo,
        string? estado,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionPrestamo.ValidarListado(pagina, tamanoPagina, tipoNormalizado, estadoNormalizado);
        if (empleadoId is not null)
        {
            await ValidacionPrestamo.ObtenerEmpleadoAsync(_unidad, empleadoId.Value);
        }

        IQueryable<Prestamo> consulta = _unidad.Repository<Prestamo>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(prestamo => prestamo.Numero.ToLower().Contains(termino)
                || prestamo.Empleado.Nombres.ToLower().Contains(termino)
                || prestamo.Empleado.Apellidos.ToLower().Contains(termino)
                || prestamo.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(prestamo => prestamo.EmpleadoId == idEmpleado);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(prestamo => prestamo.Tipo == tipoNormalizado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(prestamo => prestamo.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<PrestamoListadoDto> items = await consulta
            .OrderByDescending(prestamo => prestamo.FechaOtorgamiento)
            .ThenByDescending(prestamo => prestamo.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(prestamo => new PrestamoListadoDto
            {
                Id = prestamo.Id,
                Numero = prestamo.Numero,
                EmpleadoId = prestamo.EmpleadoId,
                EmpleadoLegajo = prestamo.Empleado.Legajo,
                EmpleadoNombre = prestamo.Empleado.Apellidos + ", " + prestamo.Empleado.Nombres,
                Tipo = prestamo.Tipo,
                Monto = prestamo.Monto,
                NumeroCuotas = prestamo.NumeroCuotas,
                FechaOtorgamiento = prestamo.FechaOtorgamiento,
                Saldo = prestamo.Saldo,
                Estado = prestamo.Estado
            })
            .ToListAsync();
        return new PaginaPrestamosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ResumenPrestamosDto> ResumenAsync()
    {
        DateOnly hoy = ValidacionPrestamo.Hoy();
        DateOnly inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        DateOnly finMes = inicioMes.AddMonths(1).AddDays(-1);
        List<FilaPrestamo> prestamos = await _unidad.Repository<Prestamo>().AsQueryable()
            .AsNoTracking()
            .Where(prestamo => prestamo.Estado != ValidacionPrestamo.EstadoAnulado)
            .Select(prestamo => new FilaPrestamo
            {
                Estado = prestamo.Estado,
                Monto = prestamo.Monto,
                Saldo = prestamo.Saldo,
                FechaOtorgamiento = prestamo.FechaOtorgamiento
            })
            .ToListAsync();
        List<CuotaPrestamo> cuotasDelMes = await _unidad.Repository<CuotaPrestamo>().AsQueryable()
            .AsNoTracking()
            .Where(cuota => cuota.Estado == ValidacionPrestamo.CuotaPendiente
                && cuota.Prestamo.Estado == ValidacionPrestamo.EstadoActivo
                && cuota.Anio == hoy.Year
                && cuota.Mes == hoy.Month)
            .ToListAsync();
        List<FilaPrestamo> activos = prestamos.Where(fila => fila.Estado == ValidacionPrestamo.EstadoActivo).ToList();
        return new ResumenPrestamosDto
        {
            Activos = activos.Count,
            SaldoPendienteTotal = activos.Sum(fila => fila.Saldo),
            OtorgadoMes = prestamos
                .Where(fila => fila.FechaOtorgamiento >= inicioMes && fila.FechaOtorgamiento <= finMes)
                .Sum(fila => fila.Monto),
            CuotasDelMes = cuotasDelMes.Sum(cuota => cuota.Monto - cuota.MontoDescontado)
        };
    }

    public async Task<PrestamoDetalleDto> ObtenerAsync(int id)
    {
        PrestamoDetalleDto? detalle = await _unidad.Repository<Prestamo>().AsQueryable()
            .AsNoTracking()
            .Where(prestamo => prestamo.Id == id)
            .Select(prestamo => new PrestamoDetalleDto
            {
                Id = prestamo.Id,
                Numero = prestamo.Numero,
                EmpleadoId = prestamo.EmpleadoId,
                EmpleadoLegajo = prestamo.Empleado.Legajo,
                EmpleadoNombre = prestamo.Empleado.Apellidos + ", " + prestamo.Empleado.Nombres,
                Tipo = prestamo.Tipo,
                Monto = prestamo.Monto,
                NumeroCuotas = prestamo.NumeroCuotas,
                FechaOtorgamiento = prestamo.FechaOtorgamiento,
                PrimerAnio = prestamo.PrimerAnio,
                PrimerMes = prestamo.PrimerMes,
                Motivo = prestamo.Motivo,
                Saldo = prestamo.Saldo,
                Estado = prestamo.Estado,
                FechaAnulacion = prestamo.FechaAnulacion,
                MotivoAnulacion = prestamo.MotivoAnulacion
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionPrestamo.PrestamoInexistente(id);
        }

        detalle.Cuotas = await _unidad.Repository<CuotaPrestamo>().AsQueryable()
            .AsNoTracking()
            .Where(cuota => cuota.PrestamoId == id)
            .OrderBy(cuota => cuota.NumeroCuota)
            .Select(cuota => new CuotaPrestamoDto
            {
                Id = cuota.Id,
                NumeroCuota = cuota.NumeroCuota,
                Anio = cuota.Anio,
                Mes = cuota.Mes,
                Monto = cuota.Monto,
                MontoDescontado = cuota.MontoDescontado,
                Estado = cuota.Estado
            })
            .ToListAsync();
        return detalle;
    }

    public async Task<CapacidadPrestamoDto> ObtenerCapacidadAsync(int empleadoId, DateOnly? fecha)
    {
        await ValidacionPrestamo.ObtenerEmpleadoAsync(_unidad, empleadoId);
        DateOnly dia = fecha ?? ValidacionPrestamo.Hoy();
        decimal sueldo = await ValidacionPrestamo.LeerSueldoVigenteAsync(_unidad, empleadoId, dia) ?? 0m;
        decimal neto = ValidacionPrestamo.NetoEstimado(sueldo);
        Dictionary<(int Anio, int Mes), decimal> comprometido =
            await ValidacionPrestamo.LeerComprometidoPorMesAsync(_unidad, empleadoId, dia.Year, dia.Month);
        return new CapacidadPrestamoDto
        {
            EmpleadoId = empleadoId,
            Fecha = dia,
            SueldoVigente = sueldo,
            NetoEstimado = neto,
            CuotaMaxima = ValidacionPrestamo.CuotaMaxima(neto),
            CuotasComprometidasPorMes = comprometido
                .OrderBy(par => par.Key.Anio)
                .ThenBy(par => par.Key.Mes)
                .Select(par => new CuotaComprometidaDto { Anio = par.Key.Anio, Mes = par.Key.Mes, Monto = par.Value })
                .ToList()
        };
    }

    public async Task<PrestamoCreadoDto> RegistrarAsync(RegistrarPrestamoRequestDto solicitud)
    {
        ValidacionPrestamo.ValidarSolicitud(solicitud, out string tipo, out int numeroCuotas, out string motivo);
        int empleadoId = solicitud.EmpleadoId!.Value;
        decimal monto = solicitud.Monto!.Value;
        DateOnly otorgamiento = solicitud.FechaOtorgamiento!.Value;
        int primerAnio = solicitud.PrimerAnio!.Value;
        int primerMes = solicitud.PrimerMes!.Value;
        Empleado empleado = await ValidacionPrestamo.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionPrestamo.EstadoEmpleadoActivo)
        {
            throw ValidacionPrestamo.Error409($"El empleado {empleadoId} está cesado y no admite préstamos.");
        }

        decimal? sueldo = await ValidacionPrestamo.LeerSueldoVigenteAsync(_unidad, empleadoId, otorgamiento);
        if (sueldo is null)
        {
            throw ValidacionPrestamo.Error409($"El empleado {empleadoId} no tiene un contrato vigente en la fecha de otorgamiento.");
        }

        List<CronogramaPrestamo.CuotaProgramada> cronograma = CronogramaPrestamo.Generar(monto, numeroCuotas, primerAnio, primerMes);
        decimal maximo = ValidacionPrestamo.CuotaMaxima(ValidacionPrestamo.NetoEstimado(sueldo.Value));
        Dictionary<(int Anio, int Mes), decimal> comprometido =
            await ValidacionPrestamo.LeerComprometidoPorMesAsync(_unidad, empleadoId, primerAnio, primerMes);
        foreach (CronogramaPrestamo.CuotaProgramada cuota in cronograma)
        {
            decimal previo = comprometido.TryGetValue((cuota.Anio, cuota.Mes), out decimal existente) ? existente : 0m;
            decimal total = previo + cuota.Monto;
            if (total > maximo)
            {
                throw ValidacionPrestamo.Error409(
                    $"La cuota de {cuota.Mes:D2}/{cuota.Anio} sería {ValidacionPrestamo.Importe(total)} y supera el máximo de {ValidacionPrestamo.Importe(maximo)} (30 % del neto estimado).");
            }
        }

        Prestamo prestamo = new Prestamo
        {
            Numero = ValidacionPrestamo.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Monto = monto,
            NumeroCuotas = numeroCuotas,
            FechaOtorgamiento = otorgamiento,
            PrimerAnio = primerAnio,
            PrimerMes = primerMes,
            Motivo = motivo,
            Estado = ValidacionPrestamo.EstadoActivo,
            Saldo = monto
        };
        foreach (CronogramaPrestamo.CuotaProgramada cuota in cronograma)
        {
            prestamo.CuotasPrestamo.Add(new CuotaPrestamo
            {
                NumeroCuota = cuota.NumeroCuota,
                Anio = cuota.Anio,
                Mes = cuota.Mes,
                Monto = cuota.Monto,
                MontoDescontado = 0m,
                Estado = ValidacionPrestamo.CuotaPendiente
            });
        }

        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await _unidad.Repository<Prestamo>().AddAsync(prestamo);
        await _unidad.CompleteAsync();
        prestamo.Numero = ValidacionPrestamo.NumeroDefinitivo(prestamo.Id);
        _unidad.Repository<Prestamo>().Update(prestamo);
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new PrestamoCreadoDto { Id = prestamo.Id, Numero = prestamo.Numero };
    }

    public async Task<PrestamoIdDto> AnularAsync(int id, AnularPrestamoRequestDto solicitud)
    {
        Prestamo prestamo = await ValidacionPrestamo.ObtenerAsync(_unidad, id);
        if (prestamo.Estado != ValidacionPrestamo.EstadoActivo)
        {
            throw ValidacionPrestamo.Error409($"El préstamo {prestamo.Numero} no está activo y no se puede anular.");
        }

        if (prestamo.CuotasPrestamo.Any(cuota => cuota.MontoDescontado > 0m))
        {
            throw ValidacionPrestamo.Error409($"El préstamo {prestamo.Numero} ya tiene descuentos y no se puede anular.");
        }

        string motivo = ValidacionPrestamo.ValidarMotivoAnulacion(solicitud.Motivo);
        prestamo.Estado = ValidacionPrestamo.EstadoAnulado;
        prestamo.Saldo = 0m;
        prestamo.FechaAnulacion = ValidacionPrestamo.Hoy();
        prestamo.MotivoAnulacion = motivo;
        await _unidad.CompleteAsync();
        return new PrestamoIdDto { Id = prestamo.Id };
    }

    // Operación para Planillas (66): descuenta las cuotas pendientes con mes <= (anio, mes), en orden, sin superar
    // montoMaximo ni el saldo de cada préstamo. No guarda: quien la llama invoca CompleteAsync en su transacción.
    // Devuelve el total descontado.
    public async Task<decimal> DescontarCuotasAsync(int empleadoId, int anio, int mes, decimal montoMaximo, int planillaId)
    {
        decimal restante = montoMaximo;
        decimal descontado = 0m;
        if (restante <= 0m)
        {
            return descontado;
        }

        int hasta = anio * 12 + mes;
        List<CuotaPrestamo> cuotas = await _unidad.Repository<CuotaPrestamo>().AsQueryable()
            .Include(cuota => cuota.Prestamo)
            .Where(cuota => cuota.Estado == ValidacionPrestamo.CuotaPendiente
                && cuota.Prestamo.EmpleadoId == empleadoId
                && cuota.Prestamo.Estado == ValidacionPrestamo.EstadoActivo
                && cuota.Anio * 12 + cuota.Mes <= hasta)
            .OrderBy(cuota => cuota.Anio)
            .ThenBy(cuota => cuota.Mes)
            .ThenBy(cuota => cuota.PrestamoId)
            .ThenBy(cuota => cuota.NumeroCuota)
            .ToListAsync();
        foreach (CuotaPrestamo cuota in cuotas)
        {
            if (restante <= 0m)
            {
                break;
            }

            Prestamo prestamo = cuota.Prestamo;
            decimal pendiente = cuota.Monto - cuota.MontoDescontado;
            decimal importe = Math.Min(Math.Min(pendiente, restante), prestamo.Saldo);
            if (importe <= 0m)
            {
                continue;
            }

            cuota.MontoDescontado += importe;
            await _unidad.Repository<DescuentoCuotaPrestamo>().AddAsync(new DescuentoCuotaPrestamo { CuotaPrestamo = cuota, PlanillaId = planillaId, Monto = importe });
            if (cuota.MontoDescontado >= cuota.Monto)
            {
                cuota.Estado = ValidacionPrestamo.CuotaDescontada;
            }

            prestamo.Saldo -= importe;
            if (prestamo.Saldo <= 0m)
            {
                prestamo.Estado = ValidacionPrestamo.EstadoCancelado;
            }

            restante -= importe;
            descontado += importe;
        }

        return descontado;
    }

    // Deshace lo descontado por una planilla (el 66 la llama al recalcular una planilla en Borrador o Calculada):
    // borra solo los descuentos de esa planilla y resta exactamente esos importes de la cuota y del saldo.
    // No guarda. Devuelve el total revertido.
    public async Task<decimal> RevertirDescuentosAsync(int planillaId)
    {
        List<DescuentoCuotaPrestamo> descuentos = await _unidad.Repository<DescuentoCuotaPrestamo>().AsQueryable()
            .Include(descuento => descuento.CuotaPrestamo)
            .ThenInclude(cuota => cuota.Prestamo)
            .Where(descuento => descuento.PlanillaId == planillaId)
            .ToListAsync();
        decimal revertido = 0m;
        foreach (DescuentoCuotaPrestamo descuento in descuentos)
        {
            CuotaPrestamo cuota = descuento.CuotaPrestamo;
            Prestamo prestamo = cuota.Prestamo;
            cuota.MontoDescontado -= descuento.Monto;
            if (cuota.MontoDescontado < cuota.Monto)
            {
                cuota.Estado = ValidacionPrestamo.CuotaPendiente;
            }

            prestamo.Saldo += descuento.Monto;
            if (prestamo.Estado == ValidacionPrestamo.EstadoCancelado)
            {
                prestamo.Estado = ValidacionPrestamo.EstadoActivo;
            }

            revertido += descuento.Monto;
            _unidad.Repository<DescuentoCuotaPrestamo>().Remove(descuento);
        }

        return revertido;
    }

    private sealed class FilaPrestamo
    {
        public string Estado { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public decimal Saldo { get; set; }

        public DateOnly FechaOtorgamiento { get; set; }
    }
}
