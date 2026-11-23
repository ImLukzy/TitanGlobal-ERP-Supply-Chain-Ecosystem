using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

public class ListarTomasInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarTomasInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaTomasInventarioDto> ExecuteAsync(
        int? almacenId,
        string? estado,
        string? busqueda,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina)
    {
        string? estadoNormalizado = ValidacionTomaInventario.NormalizarFiltroEstado(estado);
        int paginaNormalizada = pagina < 1 ? 1 : pagina;
        int tamanoNormalizado = tamanoPagina < 1 ? 20 : tamanoPagina > 100 ? 100 : tamanoPagina;

        IQueryable<TomaInventario> consulta = _unitOfWork.Repository<TomaInventario>()
            .AsQueryable()
            .AsNoTracking();

        if (almacenId.HasValue)
        {
            consulta = consulta.Where(registro => registro.AlmacenId == almacenId.Value);
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(registro => registro.Estado == estadoNormalizado);
        }

        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(registro =>
                registro.Codigo.ToLower().Contains(termino) ||
                registro.Responsable.ToLower().Contains(termino));
        }

        if (fechaDesde.HasValue)
        {
            DateTime desde = FechasUtc.Normalizar(fechaDesde.Value);
            consulta = consulta.Where(registro => registro.FechaToma >= desde);
        }

        if (fechaHasta.HasValue)
        {
            DateTime hasta = FechasUtc.Normalizar(fechaHasta.Value);
            consulta = consulta.Where(registro => registro.FechaToma <= hasta);
        }

        int total = await consulta.CountAsync();

        List<TomaInventarioListadoDto> items = await consulta
            .OrderByDescending(registro => registro.FechaToma)
            .ThenByDescending(registro => registro.Id)
            .Skip((paginaNormalizada - 1) * tamanoNormalizado)
            .Take(tamanoNormalizado)
            .Select(registro => new TomaInventarioListadoDto
            {
                Id = registro.Id,
                Codigo = registro.Codigo,
                AlmacenId = registro.AlmacenId,
                AlmacenCodigo = registro.Almacen.Codigo,
                AlmacenNombre = registro.Almacen.Nombre,
                FechaToma = registro.FechaToma,
                Estado = registro.Estado,
                Responsable = registro.Responsable,
                CantidadProductos = registro.DetallesTomaInventario.Count,
                LineasConDiferencia = registro.DetallesTomaInventario.Count(detalle => detalle.Diferencia != 0)
            })
            .ToListAsync();

        return new PaginaTomasInventarioDto
        {
            Items = items,
            Total = total,
            Pagina = paginaNormalizada,
            TamanoPagina = tamanoNormalizado
        };
    }
}
