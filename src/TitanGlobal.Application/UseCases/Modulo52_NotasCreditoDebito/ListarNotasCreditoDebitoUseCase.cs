using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

public sealed class ListarNotasCreditoDebitoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarNotasCreditoDebitoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaNotasCreditoDebitoDto> ExecuteAsync(int? facturaId, int pagina, int tamanoPagina)
    {
        int desplazamiento = ValidacionNotaCreditoDebito.ObtenerDesplazamiento(pagina, tamanoPagina);
        IQueryable<NotaCreditoDebito> consulta = _unitOfWork.Repository<NotaCreditoDebito>()
            .AsQueryable()
            .AsNoTracking();

        if (facturaId.HasValue)
        {
            ValidacionNotaCreditoDebito.ValidarFacturaId(facturaId.Value);
            consulta = consulta.Where(nota => nota.FacturaId == facturaId.Value);
        }

        int totalRegistros = await consulta.CountAsync();
        List<NotaCreditoDebitoListadoDto> items = await consulta
            .OrderByDescending(nota => nota.FechaCreacion)
            .ThenByDescending(nota => nota.Id)
            .Skip(desplazamiento)
            .Take(tamanoPagina)
            .Select(nota => new NotaCreditoDebitoListadoDto
            {
                Id = nota.Id,
                FacturaId = nota.FacturaId,
                Tipo = nota.Tipo,
                Serie = nota.Serie,
                Numero = nota.Numero,
                Estado = nota.Estado,
                Total = nota.Total,
                Moneda = nota.Factura.Moneda,
                FechaCreacion = nota.FechaCreacion,
                FechaEmision = nota.FechaEmision
            })
            .ToListAsync();

        return new PaginaNotasCreditoDebitoDto
        {
            Items = items,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = totalRegistros,
            TotalPaginas = (int)Math.Ceiling((decimal)totalRegistros / tamanoPagina)
        };
    }
}
