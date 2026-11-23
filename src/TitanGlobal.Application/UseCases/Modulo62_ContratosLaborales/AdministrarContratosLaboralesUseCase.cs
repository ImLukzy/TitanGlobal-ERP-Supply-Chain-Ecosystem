using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales;

public class AdministrarContratosLaboralesUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarContratosLaboralesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaContratosLaboralesDto> ListarAsync(
        string? busqueda,
        string? estado,
        string? modalidad,
        int? empleadoId,
        int pagina,
        int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        string modalidadNormalizada = modalidad?.Trim() ?? string.Empty;
        ValidacionContratoLaboral.ValidarListado(pagina, tamanoPagina, estadoNormalizado, modalidadNormalizada);
        DateOnly hoy = ValidacionContratoLaboral.Hoy();
        IQueryable<ContratoLaboral> consulta = _unidad.Repository<ContratoLaboral>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
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

        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(contrato => contrato.EmpleadoId == idEmpleado);
        }

        consulta = AplicarFiltroEstado(consulta, estadoNormalizado, hoy);
        int total = await consulta.CountAsync();
        List<ContratoLaboralListadoDto> items = await consulta
            .OrderByDescending(contrato => contrato.FechaInicio)
            .ThenByDescending(contrato => contrato.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ContratoLaboralDetalleDto> ObtenerAsync(int id)
    {
        DateOnly hoy = ValidacionContratoLaboral.Hoy();
        ContratoLaboralDetalleDto? detalle = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .Where(contrato => contrato.Id == id)
            .Select(contrato => new ContratoLaboralDetalleDto
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
                    : contrato.Estado,
                ContratoAnteriorId = contrato.ContratoAnteriorId,
                ContratoAnteriorNumero = contrato.ContratoAnterior != null ? contrato.ContratoAnterior.Numero : null,
                FechaCierre = contrato.FechaCierre,
                MotivoCierre = contrato.MotivoCierre
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El contrato laboral {id} no existe.");
        }

        List<AdendaContratoDto> adendas = await _unidad.Repository<AdendaContrato>().AsQueryable()
            .Where(adenda => adenda.ContratoLaboralId == id)
            .OrderBy(adenda => adenda.FechaVigencia)
            .Select(adenda => new AdendaContratoDto
            {
                Id = adenda.Id,
                FechaVigencia = adenda.FechaVigencia,
                Cargo = adenda.Cargo,
                SueldoBasico = adenda.SueldoBasico
            })
            .ToListAsync();
        AdendaContratoDto? vigente = adendas.LastOrDefault(adenda => adenda.FechaVigencia <= hoy);
        detalle.Adendas = adendas;
        detalle.CargoVigente = vigente?.Cargo ?? detalle.Cargo;
        detalle.SueldoVigente = vigente?.SueldoBasico ?? detalle.SueldoBasico;
        return detalle;
    }

    public async Task<ContratoLaboralCreadoDto> CrearAsync(GuardarContratoLaboralRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El empleado es obligatorio.");
        }

        int empleadoId = solicitud.EmpleadoId.Value;
        string modalidad = ValidacionContratoLaboral.ValidarModalidad(solicitud.Modalidad);
        if (solicitud.FechaInicio is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de inicio es obligatoria.");
        }

        DateOnly fechaInicio = solicitud.FechaInicio.Value;
        decimal sueldo = ValidacionContratoLaboral.ValidarSueldo(solicitud.SueldoBasico);
        string cargo = ValidacionContratoLaboral.ValidarCargo(solicitud.Cargo);
        ValidacionContratoLaboral.ValidarPresenciaFechaFin(modalidad, solicitud.FechaFin);
        ValidacionContratoLaboral.ValidarOrdenFechas(fechaInicio, solicitud.FechaFin);
        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw ValidacionContratoLaboral.EmpleadoInexistente(empleadoId);
        }

        if (empleado.Estado == "Cesado")
        {
            throw ValidacionContratoLaboral.EmpleadoCesado(empleadoId);
        }

        if (fechaInicio < empleado.FechaIngreso)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        }

        ContratoLaboral contrato = new ContratoLaboral
        {
            Numero = ValidacionContratoLaboral.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = fechaInicio,
            FechaFin = solicitud.FechaFin,
            SueldoBasico = sueldo,
            Cargo = cargo,
            Estado = ValidacionContratoLaboral.EstadoVigente
        };
        DateOnly hoy = ValidacionContratoLaboral.Hoy();
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            List<ContratoLaboral> vigentes = await _unidad.Repository<ContratoLaboral>().AsQueryable()
                .Where(existente => existente.EmpleadoId == empleadoId
                    && existente.Estado == ValidacionContratoLaboral.EstadoVigente)
                .ToListAsync();
            foreach (ContratoLaboral vigente in vigentes.Where(existente => existente.FechaFin < hoy))
            {
                vigente.Estado = ValidacionContratoLaboral.EstadoVencido;
            }

            await _unidad.CompleteAsync();
            if (vigentes.Any(existente => existente.Estado == ValidacionContratoLaboral.EstadoVigente))
            {
                throw ValidacionContratoLaboral.ConflictoVigente(empleadoId);
            }

            await _unidad.Repository<ContratoLaboral>().AddAsync(contrato);
            await _unidad.CompleteAsync();
            contrato.Numero = ValidacionContratoLaboral.NumeroDefinitivo(contrato.Id);
            _unidad.Repository<ContratoLaboral>().Update(contrato);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsConflictoVigente(excepcion))
        {
            throw ValidacionContratoLaboral.ConflictoVigente(empleadoId);
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsEmpleadoInexistente(excepcion))
        {
            throw ValidacionContratoLaboral.EmpleadoInexistente(empleadoId);
        }

        return new ContratoLaboralCreadoDto { Id = contrato.Id, Numero = contrato.Numero };
    }

    public async Task<ContratoLaboralCreadoDto> RenovarAsync(int id, RenovarContratoLaboralRequestDto solicitud)
    {
        string modalidad = ValidacionContratoLaboral.ValidarModalidad(solicitud.Modalidad);
        decimal sueldo = ValidacionContratoLaboral.ValidarSueldo(solicitud.SueldoBasico);
        string cargo = ValidacionContratoLaboral.ValidarCargo(solicitud.Cargo);
        ValidacionContratoLaboral.ValidarPresenciaFechaFin(modalidad, solicitud.FechaFin);
        ContratoLaboral anterior = await ValidacionContratoLaboral.ObtenerAsync(_unidad, id);
        if (anterior.Estado != ValidacionContratoLaboral.EstadoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {anterior.Numero} no está vigente y no se puede renovar.");
        }

        if (anterior.FechaFin is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {anterior.Numero} es indeterminado y no se renueva.");
        }

        int empleadoId = anterior.EmpleadoId;
        Empleado empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleAsync(entidad => entidad.Id == empleadoId);
        if (empleado.Estado == "Cesado")
        {
            throw ValidacionContratoLaboral.EmpleadoCesado(empleadoId);
        }

        DateOnly fechaInicio = anterior.FechaFin.Value.AddDays(1);
        ValidacionContratoLaboral.ValidarOrdenFechas(fechaInicio, solicitud.FechaFin);
        ContratoLaboral nuevo = new ContratoLaboral
        {
            Numero = ValidacionContratoLaboral.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = fechaInicio,
            FechaFin = solicitud.FechaFin,
            SueldoBasico = sueldo,
            Cargo = cargo,
            Estado = ValidacionContratoLaboral.EstadoVigente,
            ContratoAnteriorId = anterior.Id
        };
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            anterior.Estado = ValidacionContratoLaboral.EstadoRenovado;
            await _unidad.CompleteAsync();
            await _unidad.Repository<ContratoLaboral>().AddAsync(nuevo);
            await _unidad.CompleteAsync();
            nuevo.Numero = ValidacionContratoLaboral.NumeroDefinitivo(nuevo.Id);
            _unidad.Repository<ContratoLaboral>().Update(nuevo);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsConflictoVigente(excepcion))
        {
            throw ValidacionContratoLaboral.ConflictoVigente(empleadoId);
        }

        return new ContratoLaboralCreadoDto { Id = nuevo.Id, Numero = nuevo.Numero };
    }

    public async Task<ContratoLaboralIdDto> AgregarAdendaAsync(int id, AdendaContratoRequestDto solicitud)
    {
        if (solicitud.FechaVigencia is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de vigencia es obligatoria.");
        }

        DateOnly fechaVigencia = solicitud.FechaVigencia.Value;
        string? cargoNuevo = string.IsNullOrWhiteSpace(solicitud.Cargo) ? null : solicitud.Cargo.Trim();
        if (cargoNuevo is null && solicitud.SueldoBasico is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La adenda debe cambiar el cargo o el sueldo básico.");
        }

        if (solicitud.SueldoBasico is not null)
        {
            ValidacionContratoLaboral.ValidarSueldoPresente(solicitud.SueldoBasico.Value);
        }

        if (cargoNuevo is not null)
        {
            ValidacionContratoLaboral.ValidarLongitudCargo(cargoNuevo);
        }

        ContratoLaboral contrato = await ValidacionContratoLaboral.ObtenerAsync(_unidad, id);
        if (contrato.Estado != ValidacionContratoLaboral.EstadoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {contrato.Numero} no está vigente y no admite adendas.");
        }

        if (fechaVigencia < contrato.FechaInicio || (contrato.FechaFin is not null && fechaVigencia > contrato.FechaFin.Value))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de vigencia debe estar dentro del periodo del contrato.");
        }

        List<AdendaContrato> adendas = await _unidad.Repository<AdendaContrato>().AsQueryable()
            .Where(adenda => adenda.ContratoLaboralId == id)
            .OrderBy(adenda => adenda.FechaVigencia)
            .ToListAsync();
        if (adendas.Any(adenda => adenda.FechaVigencia >= fechaVigencia))
        {
            throw ValidacionContratoLaboral.ConflictoAdenda();
        }

        AdendaContrato? ultima = adendas.LastOrDefault();
        string cargoVigente = ultima?.Cargo ?? contrato.Cargo;
        decimal sueldoVigente = ultima?.SueldoBasico ?? contrato.SueldoBasico;
        string cargoResultante = cargoNuevo ?? cargoVigente;
        decimal sueldoResultante = solicitud.SueldoBasico ?? sueldoVigente;
        if (cargoResultante == cargoVigente && sueldoResultante == sueldoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La adenda no cambia el cargo ni el sueldo vigentes en esa fecha.");
        }

        AdendaContrato adendaNueva = new AdendaContrato
        {
            ContratoLaboralId = id,
            FechaVigencia = fechaVigencia,
            Cargo = cargoResultante,
            SueldoBasico = sueldoResultante
        };
        try
        {
            await _unidad.Repository<AdendaContrato>().AddAsync(adendaNueva);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsConflictoAdenda(excepcion))
        {
            throw ValidacionContratoLaboral.ConflictoAdenda();
        }

        return new ContratoLaboralIdDto { Id = adendaNueva.Id };
    }

    public async Task<ContratoLaboralIdDto> AnularAsync(int id, AnularContratoLaboralRequestDto solicitud)
    {
        string motivo = ValidacionContratoLaboral.ValidarMotivoAnulacion(solicitud.Motivo);
        ContratoLaboral contrato = await ValidacionContratoLaboral.ObtenerAsync(_unidad, id);
        if (contrato.Estado != ValidacionContratoLaboral.EstadoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {contrato.Numero} no está vigente y no se puede anular.");
        }

        contrato.Estado = ValidacionContratoLaboral.EstadoAnulado;
        contrato.MotivoCierre = motivo;
        contrato.FechaCierre = ValidacionContratoLaboral.Hoy();
        await _unidad.CompleteAsync();
        return new ContratoLaboralIdDto { Id = contrato.Id };
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
