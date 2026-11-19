using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class ListarAcuerdosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAcuerdosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAcuerdosDto> ExecuteAsync(
        string? busqueda,
        string? estado,
        int? proveedorId,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionAcuerdoAbastecimiento.Error("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionAcuerdoAbastecimiento.Error("El tamaño de página debe estar entre 1 y 100.");
        }

        string? estadoNormalizado = ValidacionAcuerdoAbastecimiento.NormalizarFiltroEstado(estado);
        IQueryable<AcuerdoAbastecimiento> consulta = _unitOfWork.Repository<AcuerdoAbastecimiento>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(acuerdo =>
                acuerdo.Numero.ToLower().Contains(busquedaNormalizada)
                || acuerdo.Proveedor.RazonSocial.ToLower().Contains(busquedaNormalizada));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(acuerdo => acuerdo.Estado == estadoNormalizado);
        }

        if (proveedorId is not null)
        {
            consulta = consulta.Where(acuerdo => acuerdo.ProveedorId == proveedorId);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<AcuerdoListadoDto> items = await consulta
            .OrderByDescending(acuerdo => acuerdo.FechaRegistro)
            .ThenByDescending(acuerdo => acuerdo.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(acuerdo => new AcuerdoListadoDto
            {
                Id = acuerdo.Id,
                Numero = acuerdo.Numero,
                ProveedorNombre = acuerdo.Proveedor.RazonSocial,
                Estado = acuerdo.Estado,
                FechaInicio = acuerdo.FechaInicio,
                FechaFin = acuerdo.FechaFin,
                MontoMaximo = acuerdo.MontoMaximo,
                MontoComprometido = acuerdo.LineasAcuerdoAbastecimiento
                    .Sum(linea => linea.PrecioAcordado * linea.CantidadComprometida),
                MontoConsumido = acuerdo.LineasAcuerdoAbastecimiento
                    .Sum(linea => linea.PrecioAcordado * linea.CantidadConsumida)
            })
            .ToListAsync();

        return new PaginaAcuerdosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
