using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.Dtos;
using TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.UseCases;

namespace TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.Controllers;

[ApiController]
[Route("api/v1/promociones")]
public class PromocionesController : ControllerBase
{
    private readonly PromocionesUseCase _promocionesUseCase;

    public PromocionesController(PromocionesUseCase promocionesUseCase)
    {
        _promocionesUseCase = promocionesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] int? segmentoId,
        [FromQuery] bool? vigentes,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaPromocionesDto resultado = await _promocionesUseCase.ListarAsync(
            busqueda, tipo, segmentoId, vigentes, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenPromocionesDto resultado = await _promocionesUseCase.ResumenAsync();
        return Ok(resultado);
    }

    [HttpPost("evaluar")]
    public async Task<IActionResult> Evaluar([FromBody] EvaluarCompraRequestDto solicitud)
    {
        EvaluacionCompraDto resultado = await _promocionesUseCase.EvaluarAsync(solicitud);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        PromocionDetalleDto resultado = await _promocionesUseCase.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarPromocionRequestDto solicitud)
    {
        int id = await _promocionesUseCase.GuardarAsync(solicitud, null);
        return Ok(new PromocionCreadaDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarPromocionRequestDto solicitud)
    {
        int idActualizado = await _promocionesUseCase.GuardarAsync(solicitud, id);
        return Ok(new PromocionCreadaDto { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _promocionesUseCase.EliminarAsync(id);
        return NoContent();
    }
}
