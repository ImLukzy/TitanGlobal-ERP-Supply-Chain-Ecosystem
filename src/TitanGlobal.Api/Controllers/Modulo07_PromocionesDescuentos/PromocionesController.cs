using MediatR;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Commands;
using TitanGlobal.Application.UseCases.Modulo07_PromocionesDescuentos.Queries;

namespace TitanGlobal.Api.Controllers.Modulo07_PromocionesDescuentos;

[ApiController]
[Route("api/v1/promociones")]
public class PromocionesController : ControllerBase
{
    private readonly ISender _sender;

    public PromocionesController(ISender sender)
    {
        _sender = sender;
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
        PaginaPromocionesDto resultado = await _sender.Send(new ListarPromocionesQuery
        {
            Busqueda = busqueda,
            Tipo = tipo,
            SegmentoId = segmentoId,
            Vigentes = vigentes,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenPromocionesDto resultado = await _sender.Send(new ResumenPromocionesQuery());
        return Ok(resultado);
    }

    [HttpPost("evaluar")]
    public async Task<IActionResult> Evaluar([FromBody] EvaluarCompraRequestDto solicitud)
    {
        EvaluacionCompraDto resultado = await _sender.Send(new EvaluarPromocionesQuery { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        PromocionDetalleDto resultado = await _sender.Send(new ObtenerPromocionQuery { Id = id });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarPromocionRequestDto solicitud)
    {
        int id = await _sender.Send(new GuardarPromocionCommand { Solicitud = solicitud, Id = null });
        return Ok(new PromocionCreadaDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarPromocionRequestDto solicitud)
    {
        int idActualizado = await _sender.Send(new GuardarPromocionCommand { Solicitud = solicitud, Id = id });
        return Ok(new PromocionCreadaDto { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _sender.Send(new EliminarPromocionCommand { Id = id });
        return NoContent();
    }
}
