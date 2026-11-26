using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class ListarMovimientosBancariosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarMovimientosBancariosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaMovimientosBancariosDto> ExecuteAsync(
        int id, string? tipo, DateOnly? desde, DateOnly? hasta, int pagina, int tamanoPagina)
    {
        ValidacionTesoreriaBancos.ValidarPagina(pagina, tamanoPagina);
        if (desde > hasta)
        {
            throw ValidacionTesoreriaBancos.DatosInvalidos();
        }

        await ConsultasTesoreriaBancos.CuentaAsync(_unitOfWork, id);
        IQueryable<MovimientoBancario> consulta = ConsultasTesoreriaBancos.Movimientos(_unitOfWork, id);
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoFiltro = ValidacionTesoreriaBancos.TipoMovimiento(tipo);
            consulta = consulta.Where(movimiento => movimiento.Tipo == tipoFiltro);
        }
        if (desde.HasValue)
        {
            consulta = consulta.Where(movimiento => movimiento.FechaOperacion >= desde.Value);
        }
        if (hasta.HasValue)
        {
            consulta = consulta.Where(movimiento => movimiento.FechaOperacion <= hasta.Value);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        IReadOnlyList<MovimientoBancarioDto> items = Array.Empty<MovimientoBancarioDto>();
        if (desplazamiento < total)
        {
            items = await ConsultasTesoreriaBancos.ProyectarMovimientos(consulta.OrderByDescending(movimiento => movimiento.FechaOperacion)
                .ThenByDescending(movimiento => movimiento.Id).Skip((int)desplazamiento).Take(tamanoPagina)).ToListAsync();
        }

        return new PaginaMovimientosBancariosDto
        {
            Items = items,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling((decimal)total / tamanoPagina)
        };
    }
}
