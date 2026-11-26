using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Application.Exceptions;
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
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value.Date > fechaHasta.Value.Date)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha inicial no puede ser posterior a la final.");
        }

        string? estadoNormalizado = ValidacionTomaInventario.NormalizarFiltroEstado(estado);

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
            DateTime desde = FechasUtc.Normalizar(DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc));
            consulta = consulta.Where(registro => registro.FechaToma >= desde);
        }

        if (fechaHasta.HasValue)
        {
            DateTime hastaExclusivo = FechasUtc.Normalizar(
                DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1), DateTimeKind.Utc));
            consulta = consulta.Where(registro => registro.FechaToma < hastaExclusivo);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;

        List<TomaInventarioListadoDto> items = await consulta
            .OrderByDescending(registro => registro.FechaToma)
            .ThenByDescending(registro => registro.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
