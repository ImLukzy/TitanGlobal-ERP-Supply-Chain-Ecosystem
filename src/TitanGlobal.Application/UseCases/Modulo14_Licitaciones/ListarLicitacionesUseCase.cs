using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class ListarLicitacionesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarLicitacionesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaLicitacionesDto> ExecuteAsync(
        string? busqueda,
        string? estado,
        string? tipo,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionLicitacion.Error("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionLicitacion.Error("El tamaño de página debe estar entre 1 y 100.");
        }

        string? estadoNormalizado = ValidacionLicitacion.NormalizarFiltroEstado(estado);
        string? tipoNormalizado = ValidacionLicitacion.NormalizarFiltroTipo(tipo);
        IQueryable<Licitacion> consulta = _unitOfWork.Repository<Licitacion>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(licitacion =>
                licitacion.Numero.ToLower().Contains(busquedaNormalizada)
                || licitacion.Titulo.ToLower().Contains(busquedaNormalizada));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(licitacion => licitacion.Estado == estadoNormalizado);
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(licitacion => licitacion.Tipo == tipoNormalizado);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<LicitacionListadoDto> items = await consulta
            .OrderByDescending(licitacion => licitacion.FechaRegistro)
            .ThenByDescending(licitacion => licitacion.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(licitacion => new LicitacionListadoDto
            {
                Id = licitacion.Id,
                Numero = licitacion.Numero,
                Titulo = licitacion.Titulo,
                Tipo = licitacion.Tipo,
                Estado = licitacion.Estado,
                PresupuestoReferencial = licitacion.PresupuestoReferencial,
                FechaCierre = licitacion.FechaCierre,
                TotalOfertas = licitacion.OfertasLicitacion.Count()
            })
            .ToListAsync();

        return new PaginaLicitacionesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
