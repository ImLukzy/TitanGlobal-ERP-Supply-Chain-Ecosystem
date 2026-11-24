using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

namespace TitanGlobal.Api.Controllers.Modulo52_NotasCreditoDebito;

[ApiController]
[Route("api/v1/notas-credito-debito")]
public sealed class NotasCreditoDebitoController : ControllerBase
{
    private readonly CrearNotaCreditoDebitoUseCase _crear;
    private readonly ObtenerNotaCreditoDebitoUseCase _obtener;
    private readonly ListarNotasCreditoDebitoUseCase _listar;
    private readonly ActualizarNotaCreditoDebitoUseCase _actualizar;
    private readonly EmitirNotaCreditoDebitoUseCase _emitir;
    private readonly AnularNotaCreditoDebitoUseCase _anular;

    public NotasCreditoDebitoController(
        CrearNotaCreditoDebitoUseCase crear,
        ObtenerNotaCreditoDebitoUseCase obtener,
        ListarNotasCreditoDebitoUseCase listar,
        ActualizarNotaCreditoDebitoUseCase actualizar,
        EmitirNotaCreditoDebitoUseCase emitir,
        AnularNotaCreditoDebitoUseCase anular)
    {
        _crear = crear;
        _obtener = obtener;
        _listar = listar;
        _actualizar = actualizar;
        _emitir = emitir;
        _anular = anular;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaNotasCreditoDebitoDto>> Listar(
        [FromQuery] int? facturaId = null,
        [FromQuery] string? tipo = null,
        [FromQuery] string? estado = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listar.ExecuteAsync(facturaId, tipo, estado, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NotaCreditoDebitoDetalleDto>> Obtener(int id)
    {
        return Ok(await _obtener.ExecuteAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType<NotaCreditoDebitoDetalleDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NotaCreditoDebitoDetalleDto>> Crear([FromBody] CrearNotaCreditoDebitoRequestDto solicitud)
    {
        NotaCreditoDebitoDetalleDto detalle = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = detalle.Id }, detalle);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<NotaCreditoDebitoDetalleDto>> Actualizar(
        int id, [FromBody] ActualizarNotaCreditoDebitoRequestDto solicitud)
    {
        return Ok(await _actualizar.ExecuteAsync(id, solicitud));
    }

    [HttpPost("{id:int}/emitir")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<ActionResult<NotaCreditoDebitoDetalleDto>> Emitir(int id)
    {
        return Ok(await _emitir.ExecuteAsync(id));
    }

    [HttpPost("{id:int}/anular")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<NotaCreditoDebitoDetalleDto>> Anular(
        int id, [FromBody] AnularNotaCreditoDebitoRequestDto solicitud)
    {
        return Ok(await _anular.ExecuteAsync(id, solicitud));
    }
}
