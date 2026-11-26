using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class ListarAplicacionesPagoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAplicacionesPagoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAplicacionesPagoDto> ExecuteAsync(int id, int pagina, int tamanoPagina)
    {
        ValidacionCuentasPorPagar.ValidarPagina(pagina, tamanoPagina);
        await ConsultasCuentasPorPagar.CuentaAsync(_unitOfWork, id);
        IQueryable<AplicacionPago> consulta = ConsultasCuentasPorPagar.Pagos(_unitOfWork, id);
        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        IReadOnlyList<AplicacionPagoDto> items = Array.Empty<AplicacionPagoDto>();
        if (desplazamiento < total)
        {
            items = await ConsultasCuentasPorPagar.ProyectarPagos(consulta.OrderByDescending(pago => pago.FechaPago)
                .ThenByDescending(pago => pago.Id).Skip((int)desplazamiento).Take(tamanoPagina)).ToListAsync();
        }

        return new PaginaAplicacionesPagoDto
        {
            Items = items,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling((decimal)total / tamanoPagina)
        };
    }
}
