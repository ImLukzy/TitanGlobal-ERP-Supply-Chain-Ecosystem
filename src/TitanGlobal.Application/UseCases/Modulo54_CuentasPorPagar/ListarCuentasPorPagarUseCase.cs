using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

public class ListarCuentasPorPagarUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _reloj;

    public ListarCuentasPorPagarUseCase(IUnitOfWork unitOfWork, TimeProvider reloj)
    {
        _unitOfWork = unitOfWork;
        _reloj = reloj;
    }

    public async Task<PaginaCuentasPorPagarDto> ExecuteAsync(int? proveedorId, string? moneda, bool? soloVencidas, int pagina, int tamanoPagina)
    {
        ValidacionCuentasPorPagar.ValidarPagina(pagina, tamanoPagina);
        DateOnly corte = ValidacionCuentasPorPagar.CorteLima(_reloj);
        IQueryable<CuentaPorPagarConSaldo> consulta = ConsultasCuentasPorPagar.ConSaldos(
            ConsultasCuentasPorPagar.Cuentas(_unitOfWork, proveedorId, moneda));
        if (soloVencidas == true)
        {
            consulta = consulta.Where(fila => fila.Cuenta.FechaVencimiento < corte && fila.Saldo > 0m);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        IReadOnlyList<CuentaPorPagarListadoDto> items = Array.Empty<CuentaPorPagarListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderByDescending(fila => fila.Cuenta.FechaRegistro).ThenByDescending(fila => fila.Cuenta.Id)
                .Skip((int)desplazamiento).Take(tamanoPagina).Select(fila => new CuentaPorPagarListadoDto
                {
                    Id = fila.Cuenta.Id,
                    ProveedorId = fila.Cuenta.ProveedorId,
                    Serie = fila.Cuenta.Serie,
                    NumeroDocumento = fila.Cuenta.NumeroDocumento,
                    Moneda = fila.Cuenta.Moneda,
                    Total = fila.Cuenta.Total,
                    Saldo = fila.Saldo,
                    FechaVencimiento = fila.Cuenta.FechaVencimiento,
                    Anulada = fila.Cuenta.FechaAnulacion.HasValue,
                    Vencida = fila.Cuenta.FechaVencimiento < corte && fila.Saldo > 0m
                }).ToListAsync();
        }

        return new PaginaCuentasPorPagarDto
        {
            Items = items,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling((decimal)total / tamanoPagina)
        };
    }
}
