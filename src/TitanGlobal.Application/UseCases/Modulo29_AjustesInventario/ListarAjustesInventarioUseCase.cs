using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

public class ListarAjustesInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAjustesInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAjustesInventarioDto> ExecuteAsync(
        int? almacenId,
        int? productoId,
        string? tipo,
        string? motivo,
        string? busqueda,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina)
    {
        ValidacionAjustesInventario.ValidarPaginacion(pagina, tamanoPagina);
        ValidacionAjustesInventario.ValidarRangoFechas(fechaDesde, fechaHasta);
        string? tipoNormalizado = ValidacionAjustesInventario.NormalizarTipoFiltro(tipo);
        string? motivoNormalizado = ValidacionAjustesInventario.NormalizarMotivoFiltro(motivo);

        IQueryable<AjusteInventario> consulta = _unitOfWork.Repository<AjusteInventario>()
            .AsQueryable()
            .Include(registro => registro.Almacen)
            .Include(registro => registro.Producto);

        if (almacenId.HasValue)
        {
            int filtroAlmacen = almacenId.Value;
            consulta = consulta.Where(registro => registro.AlmacenId == filtroAlmacen);
        }

        if (productoId.HasValue)
        {
            int filtroProducto = productoId.Value;
            consulta = consulta.Where(registro => registro.ProductoId == filtroProducto);
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(registro => registro.Tipo == tipoNormalizado);
        }

        if (motivoNormalizado is not null)
        {
            consulta = consulta.Where(registro => registro.Motivo == motivoNormalizado);
        }

        if (fechaDesde.HasValue)
        {
            DateTime desde = FechasUtc.Normalizar(fechaDesde.Value);
            consulta = consulta.Where(registro => registro.Fecha >= desde);
        }

        if (fechaHasta.HasValue)
        {
            DateTime hasta = FechasUtc.Normalizar(fechaHasta.Value);
            consulta = consulta.Where(registro => registro.Fecha <= hasta);
        }

        string termino = busqueda?.Trim().ToLower() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(registro =>
                registro.Codigo.ToLower().Contains(termino)
                || registro.Responsable.ToLower().Contains(termino)
                || registro.Producto.Categoria.ToLower().Contains(termino)
                || registro.Producto.Nombre.ToLower().Contains(termino)
                || registro.Almacen.Codigo.ToLower().Contains(termino)
                || registro.Almacen.Nombre.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        List<AjusteInventario> registros = await consulta
            .OrderByDescending(registro => registro.Fecha)
            .ThenByDescending(registro => registro.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        List<AjusteInventarioItemDto> items = registros
            .Select(registro => ValidacionAjustesInventario.MapearItem(registro))
            .ToList();

        return new PaginaAjustesInventarioDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
