using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

public class ListarClientesClasificadosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesClasificadosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaClientesSegmentoDto> ExecuteAsync(
        int? segmentoId,
        bool? sinSegmento,
        string? busqueda,
        int pagina,
        int tamanoPagina)
    {
        ValidacionSegmentoCliente.ValidarPaginacion(pagina, tamanoPagina);
        if (segmentoId.HasValue && sinSegmento == true)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "No se puede filtrar por segmento y sin segmento a la vez.");
        }

        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        IEnumerable<ClienteClasificadoDto> consulta = clientes;
        if (segmentoId.HasValue)
        {
            consulta = consulta.Where(cliente => cliente.SegmentoId == segmentoId.Value);
        }

        if (sinSegmento == true)
        {
            consulta = consulta.Where(cliente => cliente.SegmentoId is null);
        }

        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(cliente =>
                cliente.Nombre.ToLowerInvariant().Contains(busquedaNormalizada)
                || cliente.Ruc.ToLowerInvariant().Contains(busquedaNormalizada));
        }

        List<ClienteClasificadoDto> filtrados = consulta
            .OrderByDescending(cliente => cliente.MontoTotal)
            .ThenBy(cliente => cliente.ClienteId)
            .ToList();
        return new PaginaClientesSegmentoDto
        {
            Items = filtrados.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList(),
            Total = filtrados.Count,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
