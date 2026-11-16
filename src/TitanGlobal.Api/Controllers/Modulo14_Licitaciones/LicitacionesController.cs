using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

namespace TitanGlobal.Api.Controllers.Modulo14_Licitaciones;

[ApiController]
[Route("api/v1/licitaciones")]
public class LicitacionesController : ControllerBase
{
    private readonly ListarLicitacionesUseCase _listarLicitacionesUseCase;

    private readonly ObtenerLicitacionUseCase _obtenerLicitacionUseCase;

    private readonly CrearLicitacionUseCase _crearLicitacionUseCase;

    private readonly ActualizarLicitacionUseCase _actualizarLicitacionUseCase;

    private readonly EliminarLicitacionUseCase _eliminarLicitacionUseCase;

    private readonly CrearOfertaLicitacionUseCase _crearOfertaUseCase;

    private readonly MejorarOfertaLicitacionUseCase _mejorarOfertaUseCase;

    private readonly CerrarLicitacionUseCase _cerrarLicitacionUseCase;

    private readonly AdjudicarLicitacionUseCase _adjudicarLicitacionUseCase;

    private readonly CancelarLicitacionUseCase _cancelarLicitacionUseCase;

    private readonly ResumenLicitacionesUseCase _resumenLicitacionesUseCase;

    public LicitacionesController(
        ListarLicitacionesUseCase listarLicitacionesUseCase,
        ObtenerLicitacionUseCase obtenerLicitacionUseCase,
        CrearLicitacionUseCase crearLicitacionUseCase,
        ActualizarLicitacionUseCase actualizarLicitacionUseCase,
        EliminarLicitacionUseCase eliminarLicitacionUseCase,
        CrearOfertaLicitacionUseCase crearOfertaUseCase,
        MejorarOfertaLicitacionUseCase mejorarOfertaUseCase,
        CerrarLicitacionUseCase cerrarLicitacionUseCase,
        AdjudicarLicitacionUseCase adjudicarLicitacionUseCase,
        CancelarLicitacionUseCase cancelarLicitacionUseCase,
        ResumenLicitacionesUseCase resumenLicitacionesUseCase)
    {
        _listarLicitacionesUseCase = listarLicitacionesUseCase;
        _obtenerLicitacionUseCase = obtenerLicitacionUseCase;
        _crearLicitacionUseCase = crearLicitacionUseCase;
        _actualizarLicitacionUseCase = actualizarLicitacionUseCase;
        _eliminarLicitacionUseCase = eliminarLicitacionUseCase;
        _crearOfertaUseCase = crearOfertaUseCase;
        _mejorarOfertaUseCase = mejorarOfertaUseCase;
        _cerrarLicitacionUseCase = cerrarLicitacionUseCase;
        _adjudicarLicitacionUseCase = adjudicarLicitacionUseCase;
        _cancelarLicitacionUseCase = cancelarLicitacionUseCase;
        _resumenLicitacionesUseCase = resumenLicitacionesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? tipo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaLicitacionesDto resultado = await _listarLicitacionesUseCase.ExecuteAsync(
            busqueda,
            estado,
            tipo,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenLicitacionesDto resultado = await _resumenLicitacionesUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        LicitacionDetalleDto resultado = await _obtenerLicitacionUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearLicitacionRequestDto solicitud)
    {
        int id = await _crearLicitacionUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarLicitacionRequestDto solicitud)
    {
        int idActualizado = await _actualizarLicitacionUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarLicitacionUseCase.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:int}/ofertas")]
    public async Task<IActionResult> CrearOferta(int id, [FromBody] CrearOfertaRequestDto solicitud)
    {
        int ofertaId = await _crearOfertaUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = ofertaId });
    }

    [HttpPut("{id:int}/ofertas/{ofertaId:int}")]
    public async Task<IActionResult> MejorarOferta(
        int id,
        int ofertaId,
        [FromBody] MejorarOfertaRequestDto solicitud)
    {
        int idActualizado = await _mejorarOfertaUseCase.ExecuteAsync(id, ofertaId, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpPost("{id:int}/cerrar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        int idCerrado = await _cerrarLicitacionUseCase.ExecuteAsync(id);

        return Ok(new { Id = idCerrado });
    }

    [HttpPost("{id:int}/adjudicar")]
    public async Task<IActionResult> Adjudicar(int id, [FromBody] AdjudicarLicitacionRequestDto solicitud)
    {
        int idAdjudicado = await _adjudicarLicitacionUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idAdjudicado });
    }

    [HttpPost("{id:int}/cancelar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cancelar(int id)
    {
        int idCancelado = await _cancelarLicitacionUseCase.ExecuteAsync(id);

        return Ok(new { Id = idCancelado });
    }
}
