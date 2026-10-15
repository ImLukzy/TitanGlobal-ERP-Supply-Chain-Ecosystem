using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

public class ResumenSegmentacionClientesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenSegmentacionClientesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenSegmentacionDto> ExecuteAsync()
    {
        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        List<SegmentoCliente> segmentos = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .Where(segmento => segmento.Activo)
            .OrderBy(segmento => segmento.Prioridad)
            .ToListAsync();
        List<SegmentoClientesDto> porSegmento = segmentos.Select(segmento =>
        {
            List<ClienteClasificadoDto> asignados = clientes
                .Where(cliente => cliente.SegmentoId == segmento.Id)
                .ToList();
            return new SegmentoClientesDto
            {
                SegmentoId = segmento.Id,
                Codigo = segmento.Codigo,
                Nombre = segmento.Nombre,
                Clientes = asignados.Count,
                MontoTotal = asignados.Sum(cliente => cliente.MontoTotal)
            };
        }).ToList();
        int segmentados = clientes.Count(cliente => cliente.SegmentoId.HasValue);
        return new ResumenSegmentacionDto
        {
            TotalClientes = clientes.Count,
            ClientesSegmentados = segmentados,
            ClientesSinSegmento = clientes.Count - segmentados,
            PorSegmento = porSegmento
        };
    }
}
