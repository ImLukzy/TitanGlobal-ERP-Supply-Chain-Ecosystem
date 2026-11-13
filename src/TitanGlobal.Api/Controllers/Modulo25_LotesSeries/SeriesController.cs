using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

namespace TitanGlobal.Api.Controllers.Modulo25_LotesSeries;

[ApiController]
[Route("api/v1/lotes-series/series")]
public class SeriesController : ControllerBase
{
    private readonly ListarSeriesUseCase _listarSeriesUseCase;

    private readonly CrearSerieUseCase _crearSerieUseCase;

    private readonly CambiarEstadoSerieUseCase _cambiarEstadoSerieUseCase;

    public SeriesController(
        ListarSeriesUseCase listarSeriesUseCase,
        CrearSerieUseCase crearSerieUseCase,
        CambiarEstadoSerieUseCase cambiarEstadoSerieUseCase)
    {
        _listarSeriesUseCase = listarSeriesUseCase;
        _crearSerieUseCase = crearSerieUseCase;
        _cambiarEstadoSerieUseCase = cambiarEstadoSerieUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] int? loteId,
        [FromQuery] string? estado,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaSeriesDto resultado = await _listarSeriesUseCase.ExecuteAsync(
            productoId,
            loteId,
            estado,
            busqueda,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSerieRequestDto solicitud)
    {
        int id = await _crearSerieUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoSerieRequestDto solicitud)
    {
        int idActualizado = await _cambiarEstadoSerieUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }
}
