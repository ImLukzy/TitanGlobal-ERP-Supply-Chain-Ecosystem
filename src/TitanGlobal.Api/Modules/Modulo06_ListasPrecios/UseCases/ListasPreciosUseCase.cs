using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;
using TitanGlobal.Api.Modules.Modulo06_ListasPrecios.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo06_ListasPrecios.UseCases;

public class ListasPreciosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListasPreciosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaListasDto> ListarAsync(string? busqueda, int? segmentoId, bool? vigentes, int pagina, int tamanoPagina)
    {
        ValidarPaginacion(pagina, tamanoPagina);
        IQueryable<ListaPrecios> consulta = _unitOfWork.Repository<ListaPrecios>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(lista => lista.Codigo.ToLower().Contains(termino)
                || lista.Nombre.ToLower().Contains(termino));
        }

        if (segmentoId.HasValue)
        {
            consulta = consulta.Where(lista => lista.SegmentoClienteId == segmentoId.Value);
        }

        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        if (vigentes == true)
        {
            consulta = consulta.Where(lista => lista.Activa && lista.FechaInicio <= hoy
                && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy));
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<ListaListadoDto> items = await consulta.OrderBy(lista => lista.Prioridad).ThenBy(lista => lista.Id)
            .Skip(saltar).Take(tamanoPagina)
            .Select(lista => new ListaListadoDto
            {
                Id = lista.Id,
                Codigo = lista.Codigo,
                Nombre = lista.Nombre,
                SegmentoId = lista.SegmentoClienteId,
                SegmentoCodigo = lista.SegmentoCliente == null ? null : lista.SegmentoCliente.Codigo,
                Prioridad = lista.Prioridad,
                FechaInicio = lista.FechaInicio,
                FechaFin = lista.FechaFin,
                Activa = lista.Activa,
                Vigente = lista.Activa && lista.FechaInicio <= hoy && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy),
                Productos = lista.Items.Count
            }).ToListAsync();
        return new PaginaListasDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }

    public async Task<ListaDetalleDto> ObtenerAsync(int id)
    {
        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        ListaPrecios? lista = await _unitOfWork.Repository<ListaPrecios>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (lista is null)
        {
            throw Error(StatusCodes.Status404NotFound, $"La lista de precios {id} no existe.");
        }

        string? segmentoCodigo = await _unitOfWork.Repository<SegmentoCliente>().AsQueryable()
            .Where(segmento => segmento.Id == lista.SegmentoClienteId).Select(segmento => segmento.Codigo).SingleOrDefaultAsync();
        List<ItemListaDto> items = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
            .Where(item => item.ListaPreciosId == id)
            .Join(_unitOfWork.Repository<Producto>().AsQueryable(), item => item.ProductoId, producto => producto.Id,
                (item, producto) => new ItemListaDto
                {
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    PrecioBase = producto.Precio,
                    Precio = item.Precio,
                    DiferenciaPorcentaje = producto.Precio == 0m ? 0m
                        : decimal.Round((item.Precio - producto.Precio) / producto.Precio * 100m, 2)
                })
            .OrderBy(item => item.ProductoNombre).ToListAsync();
        ListaListadoDto listado = ProyectarListado(lista, hoy, segmentoCodigo, items.Count);
        return new ListaDetalleDto
        {
            Id = listado.Id,
            Codigo = listado.Codigo,
            Nombre = listado.Nombre,
            SegmentoId = listado.SegmentoId,
            SegmentoCodigo = listado.SegmentoCodigo,
            Prioridad = listado.Prioridad,
            FechaInicio = listado.FechaInicio,
            FechaFin = listado.FechaFin,
            Activa = listado.Activa,
            Vigente = listado.Vigente,
            Productos = listado.Productos,
            Items = items
        };
    }

    public async Task<int> GuardarAsync(GuardarListaRequestDto solicitud, int? id)
    {
        ListaPrecios? lista = null;
        if (id.HasValue)
        {
            lista = await _unitOfWork.Repository<ListaPrecios>().AsQueryable()
                .SingleOrDefaultAsync(entidad => entidad.Id == id.Value);
            if (lista is null)
            {
                throw Error(StatusCodes.Status404NotFound, $"La lista de precios {id.Value} no existe.");
            }
        }

        await ValidacionListaPrecios.ValidarAsync(solicitud, id, _unitOfWork);
        if (lista is null)
        {
            lista = new ListaPrecios();
            await _unitOfWork.Repository<ListaPrecios>().AddAsync(lista);
        }
        else
        {
            List<ItemListaPrecios> existentes = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
                .Where(item => item.ListaPreciosId == lista.Id).ToListAsync();
            foreach (ItemListaPrecios existente in existentes)
            {
                _unitOfWork.Repository<ItemListaPrecios>().Remove(existente);
            }
        }

        lista.Codigo = solicitud.Codigo;
        lista.Nombre = solicitud.Nombre;
        lista.SegmentoClienteId = solicitud.SegmentoClienteId;
        lista.Prioridad = solicitud.Prioridad;
        lista.FechaInicio = FechasUtc.Normalizar(solicitud.FechaInicio);
        lista.FechaFin = solicitud.FechaFin.HasValue ? FechasUtc.Normalizar(solicitud.FechaFin.Value) : null;
        lista.Activa = solicitud.Activa;
        foreach (GuardarItemListaDto item in solicitud.Items)
        {
            lista.Items.Add(new ItemListaPrecios
            {
                ListaPreciosId = lista.Id,
                ProductoId = item.ProductoId,
                Precio = item.Precio
            });
        }

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (excepcion.InnerException is PostgresException postgres
            && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            if (postgres.ConstraintName?.Contains("Codigo", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw Error(StatusCodes.Status409Conflict,
                    $"Ya existe una lista de precios con el código {solicitud.Codigo}.");
            }

            throw Error(StatusCodes.Status409Conflict,
                $"Ya existe una lista de precios con la prioridad {solicitud.Prioridad}.");
        }

        return lista.Id;
    }

    public async Task EliminarAsync(int id)
    {
        ListaPrecios? lista = await _unitOfWork.Repository<ListaPrecios>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (lista is null)
        {
            throw Error(StatusCodes.Status404NotFound, $"La lista de precios {id} no existe.");
        }

        _unitOfWork.Repository<ListaPrecios>().Remove(lista);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<PrecioResueltoDto> ResolverAsync(int productoId, int clienteId, DateTime? fecha)
    {
        Producto? producto = await _unitOfWork.Repository<Producto>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == productoId);
        if (producto is null)
        {
            throw Error(StatusCodes.Status400BadRequest, $"El producto {productoId} no existe.");
        }

        ClienteClasificadoDto? clienteClasificado = await ConsultaSegmentacionClientes.ObtenerClienteClasificadoAsync(clienteId, _unitOfWork);
        if (clienteClasificado is null)
        {
            throw Error(StatusCodes.Status400BadRequest, $"El cliente {clienteId} no existe.");
        }

        DateTime fechaUtc = FechasUtc.Normalizar(fecha ?? DateTime.UtcNow).Date;
        List<CandidatoPrecio> candidatos = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
            .Where(item => item.ProductoId == productoId)
            .Join(_unitOfWork.Repository<ListaPrecios>().AsQueryable(), item => item.ListaPreciosId, lista => lista.Id,
                (item, lista) => new CandidatoPrecio
                {
                    ListaId = lista.Id,
                    Codigo = lista.Codigo,
                    SegmentoId = lista.SegmentoClienteId,
                    Prioridad = lista.Prioridad,
                    Activa = lista.Activa,
                    FechaInicio = lista.FechaInicio,
                    FechaFin = lista.FechaFin,
                    Precio = item.Precio
                })
            .Where(candidato => candidato.Activa && candidato.FechaInicio <= fechaUtc
                && (!candidato.FechaFin.HasValue || candidato.FechaFin.Value >= fechaUtc)
                && (!candidato.SegmentoId.HasValue || candidato.SegmentoId == clienteClasificado.SegmentoId))
            .OrderByDescending(candidato => candidato.SegmentoId.HasValue)
            .ThenBy(candidato => candidato.Prioridad).ToListAsync();
        CandidatoPrecio? elegido = candidatos.FirstOrDefault();
        return new PrecioResueltoDto
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            ClienteId = clienteId,
            ClienteNombre = clienteClasificado.Nombre,
            SegmentoCodigo = clienteClasificado.SegmentoCodigo,
            Fecha = fechaUtc,
            PrecioBase = producto.Precio,
            Precio = elegido?.Precio ?? producto.Precio,
            Origen = elegido is null ? "Base" : elegido.SegmentoId.HasValue ? "Segmento" : "General",
            ListaId = elegido?.ListaId,
            ListaCodigo = elegido?.Codigo
        };
    }

    public async Task<ResumenListasDto> ResumenAsync()
    {
        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        IQueryable<ListaPrecios> listas = _unitOfWork.Repository<ListaPrecios>().AsQueryable();
        int total = await listas.CountAsync();
        int activas = await listas.CountAsync(lista => lista.Activa);
        int vigentes = await listas.CountAsync(lista => lista.Activa && lista.FechaInicio <= hoy
            && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy));
        int deSegmento = await listas.CountAsync(lista => lista.SegmentoClienteId.HasValue);
        int generales = await listas.CountAsync(lista => !lista.SegmentoClienteId.HasValue);
        int productosConPrecioEspecial = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
            .Where(item => item.ListaPrecios.Activa && item.ListaPrecios.FechaInicio <= hoy
                && (!item.ListaPrecios.FechaFin.HasValue || item.ListaPrecios.FechaFin.Value >= hoy))
            .Select(item => item.ProductoId).Distinct().CountAsync();
        return new ResumenListasDto
        {
            Total = total,
            Activas = activas,
            Vigentes = vigentes,
            DeSegmento = deSegmento,
            Generales = generales,
            ProductosConPrecioEspecial = productosConPrecioEspecial
        };
    }

    private static ListaListadoDto ProyectarListado(ListaPrecios lista, DateTime hoy, string? segmentoCodigo = null, int? productos = null)
    {
        return new ListaListadoDto
        {
            Id = lista.Id,
            Codigo = lista.Codigo,
            Nombre = lista.Nombre,
            SegmentoId = lista.SegmentoClienteId,
            SegmentoCodigo = segmentoCodigo,
            Prioridad = lista.Prioridad,
            FechaInicio = lista.FechaInicio,
            FechaFin = lista.FechaFin,
            Activa = lista.Activa,
            Vigente = lista.Activa && lista.FechaInicio <= hoy && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy),
            Productos = productos ?? lista.Items.Count
        };
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw Error(StatusCodes.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error(StatusCodes.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    private static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }

    private sealed class CandidatoPrecio
    {
        public int ListaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int? SegmentoId { get; set; }
        public int Prioridad { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal Precio { get; set; }
    }
}
