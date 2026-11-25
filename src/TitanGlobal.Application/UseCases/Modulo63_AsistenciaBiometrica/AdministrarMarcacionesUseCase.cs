using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

public class AdministrarMarcacionesUseCase
{
    private const int MaximoObservacion = 200;

    private const int MaximoDispositivo = 50;

    private const int MaximoNumeroRegistro = 50;

    private readonly IUnitOfWork _unidad;

    public AdministrarMarcacionesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaMarcacionesDto> ListarAsync(
        int? empleadoId,
        DateOnly? desde,
        DateOnly? hasta,
        string? tipo,
        string? origen,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        string origenNormalizado = origen?.Trim() ?? string.Empty;
        ValidacionMarcacion.ValidarPaginacion(pagina, tamanoPagina);
        ValidacionMarcacion.ValidarFiltrosMarcaciones(desde, hasta, tipoNormalizado, origenNormalizado);
        IQueryable<Marcacion> consulta = _unidad.Repository<Marcacion>().AsQueryable();
        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(marcacion => marcacion.EmpleadoId == idEmpleado);
        }

        if (desde is not null)
        {
            DateTime inicio = CalculadoraJornada.LocalAUtc(desde.Value.ToDateTime(TimeOnly.MinValue));
            consulta = consulta.Where(marcacion => marcacion.Instante >= inicio);
        }

        if (hasta is not null)
        {
            DateTime fin = CalculadoraJornada.LocalAUtc(hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
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
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<MarcacionIdDto> RegistrarAsync(RegistrarMarcacionRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw ValidacionMarcacion.Error400("El empleado es obligatorio.");
        }

        string tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!ValidacionMarcacion.EsTipoValido(tipo))
        {
            throw ValidacionMarcacion.Error400("El tipo de marcación debe ser Entrada o Salida.");
        }

        if (solicitud.Instante is null)
        {
            throw ValidacionMarcacion.Error400("El instante de la marcación es obligatorio.");
        }

        string observacion = solicitud.Observacion?.Trim() ?? string.Empty;
        if (observacion.Length == 0)
        {
            throw ValidacionMarcacion.Error400("La observación es obligatoria en una marcación manual.");
        }

        if (observacion.Length > MaximoObservacion)
        {
            throw ValidacionMarcacion.Error400("La observación no puede superar 200 caracteres.");
        }

        int empleadoId = solicitud.EmpleadoId.Value;
        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw ValidacionMarcacion.EmpleadoInexistente(empleadoId);
        }

        DateTime instante = FechasUtc.Normalizar(solicitud.Instante.Value);
        DateOnly fechaLocal = CalculadoraJornada.FechaLocal(instante);
        List<ContratoLaboral> contratos = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .Where(contrato => contrato.EmpleadoId == empleadoId)
            .ToListAsync();
        string? motivo = ValidacionMarcacion.MotivoNoVigente(empleado, contratos, fechaLocal);
        if (motivo is not null)
        {
            throw ValidacionMarcacion.Error409(motivo);
        }

        DateOnly? fechaDecidida = await ValidacionMarcacion.BuscarJornadaDecididaAsync(_unidad, empleadoId, instante);
        if (fechaDecidida is not null)
        {
            throw ValidacionMarcacion.JornadaDecidida(fechaDecidida.Value);
        }

        DateTime desdeDuplicado = instante.AddSeconds(-ValidacionMarcacion.SegundosDuplicado);
        DateTime hastaDuplicado = instante.AddSeconds(ValidacionMarcacion.SegundosDuplicado);
        bool duplicada = await _unidad.Repository<Marcacion>().AsQueryable()
            .AnyAsync(marcacion => marcacion.EmpleadoId == empleadoId
                && marcacion.Tipo == tipo
                && marcacion.Instante >= desdeDuplicado
                && marcacion.Instante <= hastaDuplicado);
        if (duplicada)
        {
            throw ValidacionMarcacion.Error409(ValidacionMarcacion.MarcacionDuplicada());
        }

        Marcacion marcacionNueva = new Marcacion
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Instante = instante,
            Origen = CalculadoraJornada.OrigenManual,
            Observacion = observacion
        };
        await _unidad.Repository<Marcacion>().AddAsync(marcacionNueva);
        await _unidad.CompleteAsync();
        return new MarcacionIdDto { Id = marcacionNueva.Id };
    }

    public async Task<ResultadoImportacionDto> ImportarAsync(ImportarMarcacionesRequestDto solicitud)
    {
        string dispositivo = solicitud.Dispositivo?.Trim() ?? string.Empty;
        if (dispositivo.Length == 0)
        {
            throw ValidacionMarcacion.Error400("El dispositivo es obligatorio.");
        }

        if (dispositivo.Length > MaximoDispositivo)
        {
            throw ValidacionMarcacion.Error400("El dispositivo no puede superar 50 caracteres.");
        }

        List<RegistroRelojDto?> registros = solicitud.Registros?.Cast<RegistroRelojDto?>().ToList() ?? new List<RegistroRelojDto?>();
        if (registros.Count < 1 || registros.Count > ValidacionMarcacion.MaximoRegistrosImportacion)
        {
            throw ValidacionMarcacion.Error400("La importación debe tener entre 1 y 1000 registros.");
        }

        ResultadoImportacionDto resultado = new ResultadoImportacionDto { Recibidos = registros.Count };
        List<string> legajos = registros
            .Select(registro => registro?.Legajo?.Trim() ?? string.Empty)
            .Where(legajo => legajo.Length > 0)
            .Distinct()
            .ToList();
        Dictionary<string, Empleado> empleados = (await _unidad.Repository<Empleado>().AsQueryable()
            .Where(empleado => legajos.Contains(empleado.Legajo))
            .ToListAsync()).ToDictionary(empleado => empleado.Legajo);
        List<int> empleadoIds = empleados.Values.Select(empleado => empleado.Id).ToList();
        List<ContratoLaboral> contratos = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .Where(contrato => empleadoIds.Contains(contrato.EmpleadoId))
            .ToListAsync();
        List<string> numeros = registros
            .Select(registro => registro?.NumeroRegistro?.Trim() ?? string.Empty)
            .Where(numero => numero.Length > 0)
            .Distinct()
            .ToList();
        HashSet<string> yaImportados = (await _unidad.Repository<Marcacion>().AsQueryable()
            .Where(marcacion => marcacion.Dispositivo == dispositivo
                && marcacion.NumeroRegistro != null
                && numeros.Contains(marcacion.NumeroRegistro))
            .Select(marcacion => marcacion.NumeroRegistro!)
            .ToListAsync()).ToHashSet();
        List<Marcacion> comparables = await CargarMarcacionesCercanasAsync(registros, empleados, empleadoIds);
        List<Marcacion> nuevas = new List<Marcacion>();
        foreach (RegistroRelojDto? registro in registros)
        {
            RegistroRelojDto actual = registro ?? new RegistroRelojDto();
            string numero = actual.NumeroRegistro?.Trim() ?? string.Empty;
            string? motivo = ValidarRegistro(actual, numero, empleados, contratos);
            if (motivo is not null)
            {
                resultado.Rechazados.Add(new RegistroRechazadoDto { NumeroRegistro = numero, Motivo = motivo });
                continue;
            }

            if (!yaImportados.Add(numero))
            {
                resultado.YaImportados++;
                continue;
            }

            Empleado empleado = empleados[actual.Legajo!.Trim()];
            string tipo = actual.Tipo!.Trim();
            DateTime instante = FechasUtc.Normalizar(actual.Instante!.Value);
            if (ExisteDuplicada(comparables, empleado.Id, tipo, instante))
            {
                resultado.Duplicados++;
                continue;
            }

            Marcacion marcacion = new Marcacion
            {
                EmpleadoId = empleado.Id,
                Tipo = tipo,
                Instante = instante,
                Origen = CalculadoraJornada.OrigenReloj,
                Dispositivo = dispositivo,
                NumeroRegistro = numero
            };
            nuevas.Add(marcacion);
            comparables.Add(marcacion);
            resultado.Importados++;
        }

        if (nuevas.Count > 0)
        {
            foreach (Marcacion marcacion in nuevas)
            {
                await _unidad.Repository<Marcacion>().AddAsync(marcacion);
            }

            await _unidad.CompleteAsync();
        }

        return resultado;
    }

    public async Task EliminarAsync(int id)
    {
        Marcacion? marcacion = await _unidad.Repository<Marcacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (marcacion is null)
        {
            throw ValidacionMarcacion.MarcacionInexistente(id);
        }

        if (marcacion.Origen == CalculadoraJornada.OrigenReloj)
        {
            throw ValidacionMarcacion.Error409("Las marcaciones del reloj no se eliminan.");
        }

        DateOnly? fechaDecidida = await ValidacionMarcacion.BuscarJornadaDecididaAsync(
            _unidad,
            marcacion.EmpleadoId,
            FechasUtc.Normalizar(marcacion.Instante));
        if (fechaDecidida is not null)
        {
            throw ValidacionMarcacion.JornadaDecidida(fechaDecidida.Value);
        }

        _unidad.Repository<Marcacion>().Remove(marcacion);
        await _unidad.CompleteAsync();
    }

    // Motivo literal por el que se rechaza un registro del reloj, o null si es válido. Mensajes 2, 3, 13, 7 y 8.
    private static string? ValidarRegistro(
        RegistroRelojDto registro,
        string numero,
        Dictionary<string, Empleado> empleados,
        List<ContratoLaboral> contratos)
    {
        string tipo = registro.Tipo?.Trim() ?? string.Empty;
        if (!ValidacionMarcacion.EsTipoValido(tipo))
        {
            return "El tipo de marcación debe ser Entrada o Salida.";
        }

        if (registro.Instante is null)
        {
            return "El instante de la marcación es obligatorio.";
        }

        if (numero.Length == 0)
        {
            return "El número de registro es obligatorio.";
        }

        if (numero.Length > MaximoNumeroRegistro)
        {
            return "El número de registro no puede superar 50 caracteres.";
        }

        string legajo = registro.Legajo?.Trim() ?? string.Empty;
        if (!empleados.TryGetValue(legajo, out Empleado? empleado))
        {
            return $"No existe un empleado con el legajo {legajo}.";
        }

        DateOnly fechaLocal = CalculadoraJornada.FechaLocal(FechasUtc.Normalizar(registro.Instante.Value));
        return ValidacionMarcacion.MotivoNoVigente(empleado, contratos, fechaLocal);
    }

    private async Task<List<Marcacion>> CargarMarcacionesCercanasAsync(
        List<RegistroRelojDto?> registros,
        Dictionary<string, Empleado> empleados,
        List<int> empleadoIds)
    {
        List<DateTime> instantes = registros
            .Where(registro => registro?.Instante is not null)
            .Select(registro => FechasUtc.Normalizar(registro!.Instante!.Value))
            .ToList();
        if (instantes.Count == 0 || empleados.Count == 0)
        {
            return new List<Marcacion>();
        }

        DateTime desde = instantes.Min().AddSeconds(-ValidacionMarcacion.SegundosDuplicado);
        DateTime hasta = instantes.Max().AddSeconds(ValidacionMarcacion.SegundosDuplicado);
        return await _unidad.Repository<Marcacion>().AsQueryable()
            .AsNoTracking()
            .Where(marcacion => empleadoIds.Contains(marcacion.EmpleadoId)
                && marcacion.Instante >= desde
                && marcacion.Instante <= hasta)
            .ToListAsync();
    }

    private static bool ExisteDuplicada(List<Marcacion> comparables, int empleadoId, string tipo, DateTime instante)
    {
        return comparables.Any(marcacion => marcacion.EmpleadoId == empleadoId
            && marcacion.Tipo == tipo
            && Math.Abs((FechasUtc.Normalizar(marcacion.Instante) - instante).TotalSeconds) <= ValidacionMarcacion.SegundosDuplicado);
    }
}
