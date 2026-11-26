using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

public class ListarCuentasBancariasUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCuentasBancariasUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaCuentasBancariasDto> ExecuteAsync(string? moneda, string? tipo, int pagina, int tamanoPagina)
    {
        ValidacionTesoreriaBancos.ValidarPagina(pagina, tamanoPagina);
        IQueryable<CuentaBancaria> consulta = ConsultasTesoreriaBancos.Cuentas(_unitOfWork);
        if (!string.IsNullOrWhiteSpace(moneda))
        {
            string monedaFiltro = ValidacionTesoreriaBancos.Moneda(moneda);
            consulta = consulta.Where(cuenta => cuenta.Moneda == monedaFiltro);
        }
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoFiltro = ValidacionTesoreriaBancos.TipoCuenta(tipo);
            consulta = consulta.Where(cuenta => cuenta.Tipo == tipoFiltro);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        IReadOnlyList<CuentaBancariaListadoDto> items = Array.Empty<CuentaBancariaListadoDto>();
        if (desplazamiento < total)
        {
            IQueryable<CuentaBancaria> paginaCuentas = consulta.OrderBy(cuenta => cuenta.Banco)
                .ThenBy(cuenta => cuenta.Numero).ThenBy(cuenta => cuenta.Id).Skip((int)desplazamiento).Take(tamanoPagina);
            items = await ConsultasTesoreriaBancos.CuentasConSaldo(paginaCuentas).Select(datos => new CuentaBancariaListadoDto
            {
                Id = datos.Cuenta.Id,
                Banco = datos.Cuenta.Banco,
                Numero = datos.Cuenta.Numero,
                Nombre = datos.Cuenta.Nombre,
                Tipo = datos.Cuenta.Tipo,
                Moneda = datos.Cuenta.Moneda,
                Saldo = datos.Saldo
            }).ToListAsync();
        }

        return new PaginaCuentasBancariasDto
        {
            Items = items,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = total,
            TotalPaginas = (int)Math.Ceiling((decimal)total / tamanoPagina)
        };
    }
}
