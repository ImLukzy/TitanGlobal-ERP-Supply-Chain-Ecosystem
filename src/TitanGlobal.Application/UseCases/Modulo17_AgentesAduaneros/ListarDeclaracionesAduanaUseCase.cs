using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ListarDeclaracionesAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarDeclaracionesAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaDeclaracionesDto> ExecuteAsync(
        string? busqueda,
        string? estado,
        string? canal,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionDeclaracionAduana.Error("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionDeclaracionAduana.Error("El tamaño de página debe estar entre 1 y 100.");
        }

        string? estadoNormalizado = ValidacionDeclaracionAduana.NormalizarFiltroEstado(estado);
        string? canalNormalizado = ValidacionDeclaracionAduana.NormalizarFiltroCanal(canal);
        IQueryable<DeclaracionAduana> consulta = _unitOfWork.Repository<DeclaracionAduana>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (busquedaNormalizada.Length > 0)
        {
            consulta = consulta.Where(declaracion =>
                declaracion.Numero.ToLower().Contains(busquedaNormalizada)
                || declaracion.ExpedienteImportacion.Numero.ToLower().Contains(busquedaNormalizada));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(declaracion => declaracion.Estado == estadoNormalizado);
        }

        if (canalNormalizado is not null)
        {
            consulta = consulta.Where(declaracion => declaracion.Canal == canalNormalizado);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<DeclaracionListadoDto> items = await consulta
            .OrderByDescending(declaracion => declaracion.FechaRegistro)
            .ThenByDescending(declaracion => declaracion.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(declaracion => new DeclaracionListadoDto
            {
                Id = declaracion.Id,
                Numero = declaracion.Numero,
                ExpedienteNumero = declaracion.ExpedienteImportacion.Numero,
                AgenteRazonSocial = declaracion.AgenteAduanero.RazonSocial,
                Regimen = declaracion.Regimen,
                Canal = declaracion.Canal,
                Estado = declaracion.Estado,
                DerechosArancel = declaracion.DerechosArancel
            })
            .ToListAsync();

        return new PaginaDeclaracionesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
