using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class ListarExpedientesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarExpedientesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaExpedientesDto> ExecuteAsync(
        string? busqueda,
        string? estado,
        string? incoterm,
        string? medioTransporte,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionExpedienteImportacion.Error("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionExpedienteImportacion.Error("El tamaño de página debe estar entre 1 y 100.");
        }

        string? estadoNormalizado = ValidacionExpedienteImportacion.NormalizarFiltroEstado(estado);
        string? incotermNormalizado = ValidacionExpedienteImportacion.NormalizarFiltroIncoterm(incoterm);
        string? medioNormalizado = ValidacionExpedienteImportacion.NormalizarFiltroMedio(medioTransporte);
        IQueryable<ExpedienteImportacion> consulta = _unitOfWork.Repository<ExpedienteImportacion>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(expediente =>
                expediente.Numero.ToLower().Contains(busquedaNormalizada)
                || expediente.OrdenAbastecimiento.Numero.ToLower().Contains(busquedaNormalizada));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(expediente => expediente.Estado == estadoNormalizado);
        }

        if (incotermNormalizado is not null)
        {
            consulta = consulta.Where(expediente => expediente.Incoterm == incotermNormalizado);
        }

        if (medioNormalizado is not null)
        {
            consulta = consulta.Where(expediente => expediente.MedioTransporte == medioNormalizado);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ExpedienteListadoDto> items = await consulta
            .OrderBy(expediente => expediente.FechaLlegadaEstimada)
            .ThenBy(expediente => expediente.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(expediente => new ExpedienteListadoDto
            {
                Id = expediente.Id,
                Numero = expediente.Numero,
                OrdenNumero = expediente.OrdenAbastecimiento.Numero,
                ProveedorNombre = expediente.OrdenAbastecimiento.Proveedor.RazonSocial,
                Incoterm = expediente.Incoterm,
                MedioTransporte = expediente.MedioTransporte,
                Estado = expediente.Estado,
                FechaLlegadaEstimada = expediente.FechaLlegadaEstimada
            })
            .ToListAsync();

        return new PaginaExpedientesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
