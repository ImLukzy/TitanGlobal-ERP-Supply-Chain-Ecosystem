using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

public class AdministrarEquiposProteccionUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarEquiposProteccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEquiposProteccionDto> ListarAsync(string? busqueda, bool? activo, int pagina, int tamanoPagina)
    {
        ValidacionSst.ValidarPagina(pagina, tamanoPagina);
        IQueryable<EquipoProteccion> consulta = _unidad.Repository<EquipoProteccion>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(equipo => equipo.Codigo.ToLower().Contains(termino)
                || equipo.Nombre.ToLower().Contains(termino));
        }

        if (activo is not null)
        {
            bool estaActivo = activo.Value;
            consulta = consulta.Where(equipo => equipo.Activo == estaActivo);
        }

        int total = await consulta.CountAsync();
        List<EquipoProteccion> equipos = await consulta
            .OrderBy(equipo => equipo.Codigo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<EquipoProteccionListadoDto> items = new List<EquipoProteccionListadoDto>();
        foreach (EquipoProteccion equipo in equipos)
        {
            items.Add(Convertir(equipo));
        }

        return new PaginaEquiposProteccionDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<EquipoProteccionListadoDto> ObtenerAsync(int id)
    {
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, id);
        return Convertir(equipo);
    }

    public async Task<EquipoProteccionIdDto> CrearAsync(GuardarEquipoProteccionRequestDto solicitud)
    {
        EquipoProteccion equipo = ValidacionSst.ValidarEquipo(solicitud);
        string codigo = equipo.Codigo;
        bool existe = await _unidad.Repository<EquipoProteccion>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<EquipoProteccion>().AddAsync(equipo);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionSst.EsCodigoEquipoDuplicado(excepcion))
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        return new EquipoProteccionIdDto { Id = equipo.Id };
    }

    public async Task<EquipoProteccionIdDto> ActualizarAsync(int id, GuardarEquipoProteccionRequestDto solicitud)
    {
        EquipoProteccion nuevo = ValidacionSst.ValidarEquipo(solicitud);
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, id);
        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<EquipoProteccion>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != id);
        if (existe)
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        equipo.Codigo = nuevo.Codigo;
        equipo.Nombre = nuevo.Nombre;
        equipo.VidaUtilDias = nuevo.VidaUtilDias;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionSst.EsCodigoEquipoDuplicado(excepcion))
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        return new EquipoProteccionIdDto { Id = equipo.Id };
    }

    public async Task<EquipoProteccionIdDto> DesactivarAsync(int id)
    {
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, id);
        if (!equipo.Activo)
        {
            throw ValidacionSst.EquipoYaEnEstado(equipo.Codigo, false);
        }

        equipo.Activo = false;
        await _unidad.CompleteAsync();
        return new EquipoProteccionIdDto { Id = equipo.Id };
    }

    public async Task<EquipoProteccionIdDto> ActivarAsync(int id)
    {
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, id);
        if (equipo.Activo)
        {
            throw ValidacionSst.EquipoYaEnEstado(equipo.Codigo, true);
        }

        equipo.Activo = true;
        await _unidad.CompleteAsync();
        return new EquipoProteccionIdDto { Id = equipo.Id };
    }

    public async Task<PaginaEntregasEppDto> ListarEntregasAsync(
        int? empleadoId,
        int? equipoId,
        bool? vencidas,
        int pagina,
        int tamanoPagina)
    {
        ValidacionSst.ValidarPagina(pagina, tamanoPagina);
        DateOnly hoy = ValidacionSst.Hoy();
        IQueryable<EntregaEpp> consulta = _unidad.Repository<EntregaEpp>().AsQueryable();
        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(entrega => entrega.EmpleadoId == idEmpleado);
        }

        if (equipoId is not null)
        {
            int idEquipo = equipoId.Value;
            consulta = consulta.Where(entrega => entrega.EquipoProteccionId == idEquipo);
        }

        if (vencidas is not null)
        {
            if (vencidas.Value)
            {
                consulta = consulta.Where(entrega => entrega.FechaVencimiento < hoy);
            }
            else
            {
                consulta = consulta.Where(entrega => entrega.FechaVencimiento >= hoy);
            }
        }

        int total = await consulta.CountAsync();
        List<EntregaEpp> entregas = await consulta
            .Include(entrega => entrega.Empleado)
            .Include(entrega => entrega.EquipoProteccion)
            .OrderByDescending(entrega => entrega.FechaEntrega)
            .ThenByDescending(entrega => entrega.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<EntregaEppDto> items = new List<EntregaEppDto>();
        foreach (EntregaEpp entrega in entregas)
        {
            items.Add(ConvertirEntrega(entrega, hoy));
        }

        return new PaginaEntregasEppDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<EntregaEppIdDto> RegistrarEntregaAsync(RegistrarEntregaEppRequestDto solicitud)
    {
        ValidacionSst.ValidarEntrega(solicitud);
        int empleadoId = solicitud.EmpleadoId!.Value;
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, solicitud.EquipoId ?? 0);
        if (!equipo.Activo)
        {
            throw ValidacionSst.EquipoInactivo(equipo.Codigo);
        }

        Empleado empleado = await ValidacionSst.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionSst.EmpleadoActivo)
        {
            throw ValidacionSst.EmpleadoCesado(empleadoId);
        }

        DateOnly fechaEntrega = solicitud.FechaEntrega!.Value;
        EntregaEpp entrega = new EntregaEpp
        {
            EquipoProteccionId = equipo.Id,
            EmpleadoId = empleadoId,
            FechaEntrega = fechaEntrega,
            Cantidad = solicitud.Cantidad!.Value,
            FechaVencimiento = fechaEntrega.AddDays(equipo.VidaUtilDias)
        };
        await _unidad.Repository<EntregaEpp>().AddAsync(entrega);
        await _unidad.CompleteAsync();
        return new EntregaEppIdDto { Id = entrega.Id };
    }

    private static EquipoProteccionListadoDto Convertir(EquipoProteccion equipo)
    {
        return new EquipoProteccionListadoDto
        {
            Id = equipo.Id,
            Codigo = equipo.Codigo,
            Nombre = equipo.Nombre,
            VidaUtilDias = equipo.VidaUtilDias,
            Activo = equipo.Activo
        };
    }

    private static EntregaEppDto ConvertirEntrega(EntregaEpp entrega, DateOnly hoy)
    {
        return new EntregaEppDto
        {
            Id = entrega.Id,
            EquipoId = entrega.EquipoProteccionId,
            EquipoCodigo = entrega.EquipoProteccion.Codigo,
            EquipoNombre = entrega.EquipoProteccion.Nombre,
            EmpleadoId = entrega.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(entrega.Empleado),
            FechaEntrega = entrega.FechaEntrega,
            Cantidad = entrega.Cantidad,
            FechaVencimiento = entrega.FechaVencimiento,
            Vencida = entrega.FechaVencimiento < hoy
        };
    }
}
