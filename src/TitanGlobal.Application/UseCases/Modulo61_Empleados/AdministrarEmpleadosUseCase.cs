using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados;

public class AdministrarEmpleadosUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarEmpleadosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEmpleadosDto> ListarAsync(
        string? busqueda,
        string? estado,
        int pagina,
        int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionEmpleado.ValidarListado(pagina, tamanoPagina, estadoNormalizado);
        IQueryable<Empleado> consulta = _unidad.Repository<Empleado>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(empleado => empleado.Nombres.ToLower().Contains(termino)
                || empleado.Apellidos.ToLower().Contains(termino)
                || empleado.NumeroDocumento.ToLower().Contains(termino)
                || empleado.Legajo.ToLower().Contains(termino));
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(empleado => empleado.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<EmpleadoListadoDto> items = await consulta.OrderBy(empleado => empleado.Apellidos)
            .ThenBy(empleado => empleado.Nombres)
            .ThenBy(empleado => empleado.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(empleado => new EmpleadoListadoDto
            {
                Id = empleado.Id,
                Legajo = empleado.Legajo,
                TipoDocumento = empleado.TipoDocumento,
                NumeroDocumento = empleado.NumeroDocumento,
                Nombres = empleado.Nombres,
                Apellidos = empleado.Apellidos,
                Cargo = empleado.Cargo,
                Area = empleado.Area,
                FechaIngreso = empleado.FechaIngreso,
                Estado = empleado.Estado
            })
            .ToListAsync();
        return new PaginaEmpleadosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<EmpleadoDetalleDto> ObtenerAsync(int id)
    {
        Empleado empleado = await ValidacionEmpleado.ObtenerAsync(_unidad, id);
        return MapearDetalle(empleado);
    }

    public async Task<int> CrearAsync(GuardarEmpleadoRequestDto solicitud)
    {
        ValidacionEmpleado.DatosEmpleado datos = ValidacionEmpleado.Normalizar(solicitud);
        ValidacionEmpleado.ValidarEdadMinima(datos.FechaNacimiento, datos.FechaIngreso);
        await ValidacionEmpleado.ValidarDocumentoUnicoAsync(_unidad, datos.TipoDocumento, datos.NumeroDocumento, null);
        Empleado empleado = new Empleado
        {
            Legajo = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}",
            Estado = ValidacionEmpleado.EstadoActivo
        };
        AplicarDatos(empleado, datos);
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            await _unidad.Repository<Empleado>().AddAsync(empleado);
            await _unidad.CompleteAsync();
            empleado.Legajo = $"EMP-{empleado.Id:D6}";
            _unidad.Repository<Empleado>().Update(empleado);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEmpleado.EsDuplicadoDocumento(excepcion))
        {
            throw ValidacionEmpleado.CrearConflictoDocumento(datos.TipoDocumento, datos.NumeroDocumento);
        }

        return empleado.Id;
    }

    public async Task<int> ActualizarAsync(int id, GuardarEmpleadoRequestDto solicitud)
    {
        ValidacionEmpleado.DatosEmpleado datos = ValidacionEmpleado.Normalizar(solicitud);
        ValidacionEmpleado.ValidarEdadMinima(datos.FechaNacimiento, datos.FechaIngreso);
        await ValidacionEmpleado.ValidarDocumentoUnicoAsync(_unidad, datos.TipoDocumento, datos.NumeroDocumento, id);
        Empleado empleado = await ValidacionEmpleado.ObtenerAsync(_unidad, id);
        ValidacionEmpleado.ValidarEditable(empleado);
        AplicarDatos(empleado, datos);
        _unidad.Repository<Empleado>().Update(empleado);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEmpleado.EsDuplicadoDocumento(excepcion))
        {
            throw ValidacionEmpleado.CrearConflictoDocumento(datos.TipoDocumento, datos.NumeroDocumento);
        }

        return empleado.Id;
    }

    public async Task<int> CesarAsync(int id, CeseEmpleadoRequestDto solicitud)
    {
        DateOnly fechaCese = ValidacionEmpleado.ObtenerFechaCese(solicitud.FechaCese);
        string motivo = ValidacionEmpleado.NormalizarMotivoCese(solicitud.Motivo);
        Empleado empleado = await ValidacionEmpleado.ObtenerAsync(_unidad, id);
        ValidacionEmpleado.ValidarCesable(empleado);
        ValidacionEmpleado.ValidarFechaCese(fechaCese, empleado.FechaIngreso);
        empleado.Estado = ValidacionEmpleado.EstadoCesado;
        empleado.FechaCese = fechaCese;
        empleado.MotivoCese = motivo;
        _unidad.Repository<Empleado>().Update(empleado);
        await _unidad.CompleteAsync();
        return empleado.Id;
    }

    private static void AplicarDatos(Empleado empleado, ValidacionEmpleado.DatosEmpleado datos)
    {
        empleado.TipoDocumento = datos.TipoDocumento;
        empleado.NumeroDocumento = datos.NumeroDocumento;
        empleado.Nombres = datos.Nombres;
        empleado.Apellidos = datos.Apellidos;
        empleado.FechaNacimiento = datos.FechaNacimiento;
        empleado.FechaIngreso = datos.FechaIngreso;
        empleado.Cargo = datos.Cargo;
        empleado.Area = datos.Area;
        empleado.Email = datos.Email;
        empleado.Telefono = datos.Telefono;
    }

    private static EmpleadoDetalleDto MapearDetalle(Empleado empleado)
    {
        return new EmpleadoDetalleDto
        {
            Id = empleado.Id,
            Legajo = empleado.Legajo,
            TipoDocumento = empleado.TipoDocumento,
            NumeroDocumento = empleado.NumeroDocumento,
            Nombres = empleado.Nombres,
            Apellidos = empleado.Apellidos,
            Cargo = empleado.Cargo,
            Area = empleado.Area,
            FechaIngreso = empleado.FechaIngreso,
            Estado = empleado.Estado,
            FechaNacimiento = empleado.FechaNacimiento,
            Email = empleado.Email,
            Telefono = empleado.Telefono,
            FechaCese = empleado.FechaCese,
            MotivoCese = empleado.MotivoCese
        };
    }
}
