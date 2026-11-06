using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

namespace TitanGlobal.Api.Controllers.Modulo25_LotesSeries;

[ApiController]
[Route("api/v1/lotes-series/lotes")]
public class LotesController : ControllerBase
{
    private readonly ListarLotesUseCase _listarLotesUseCase;

    private readonly ObtenerLoteUseCase _obtenerLoteUseCase;

    private readonly CrearLoteUseCase _crearLoteUseCase;

    private readonly ActualizarLoteUseCase _actualizarLoteUseCase;

    private readonly ResumenLotesUseCase _resumenLotesUseCase;

    public LotesController(
        ListarLotesUseCase listarLotesUseCase,
        ObtenerLoteUseCase obtenerLoteUseCase,
        CrearLoteUseCase crearLoteUseCase,
        ActualizarLoteUseCase actualizarLoteUseCase,
        ResumenLotesUseCase resumenLotesUseCase)
    {
        _listarLotesUseCase = listarLotesUseCase;
        _obtenerLoteUseCase = obtenerLoteUseCase;
        _crearLoteUseCase = crearLoteUseCase;
        _actualizarLoteUseCase = actualizarLoteUseCase;
        _resumenLotesUseCase = resumenLotesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] string? estado,
        [FromQuery] bool? vencidos,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaLotesDto resultado = await _listarLotesUseCase.ExecuteAsync(
            productoId,
            estado,
            vencidos,
            busqueda,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenLotesDto resultado = await _resumenLotesUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        LoteDetalleDto resultado = await _obtenerLoteUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarLoteRequestDto solicitud)
    {
        int id = await _crearLoteUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarLoteRequestDto solicitud)
    {
        int idActualizado = await _actualizarLoteUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }
}
