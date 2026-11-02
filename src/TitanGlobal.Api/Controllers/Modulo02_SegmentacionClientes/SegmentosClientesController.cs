using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

namespace TitanGlobal.Api.Controllers.Modulo02_SegmentacionClientes;

[ApiController]
[Route("api/v1/segmentos-clientes")]
public class SegmentosClientesController : ControllerBase
{
    private readonly ListarSegmentosClientesUseCase _listarUseCase;
    private readonly ObtenerSegmentoClienteUseCase _obtenerUseCase;
    private readonly CrearSegmentoClienteUseCase _crearUseCase;
    private readonly ActualizarSegmentoClienteUseCase _actualizarUseCase;
    private readonly EliminarSegmentoClienteUseCase _eliminarUseCase;
    private readonly ListarClientesDeSegmentoUseCase _clientesSegmentoUseCase;
    private readonly ListarClientesClasificadosUseCase _clasificacionUseCase;
    private readonly ResumenSegmentacionClientesUseCase _resumenUseCase;

    public SegmentosClientesController(
        ListarSegmentosClientesUseCase listarUseCase,
        ObtenerSegmentoClienteUseCase obtenerUseCase,
        CrearSegmentoClienteUseCase crearUseCase,
        ActualizarSegmentoClienteUseCase actualizarUseCase,
        EliminarSegmentoClienteUseCase eliminarUseCase,
        ListarClientesDeSegmentoUseCase clientesSegmentoUseCase,
        ListarClientesClasificadosUseCase clasificacionUseCase,
        ResumenSegmentacionClientesUseCase resumenUseCase)
    {
        _listarUseCase = listarUseCase;
        _obtenerUseCase = obtenerUseCase;
        _crearUseCase = crearUseCase;
        _actualizarUseCase = actualizarUseCase;
        _eliminarUseCase = eliminarUseCase;
        _clientesSegmentoUseCase = clientesSegmentoUseCase;
        _clasificacionUseCase = clasificacionUseCase;
        _resumenUseCase = resumenUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaSegmentosDto resultado = await _listarUseCase.ExecuteAsync(busqueda, activo, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("clasificacion")]
    public async Task<IActionResult> Clasificacion(
        [FromQuery] int? segmentoId,
        [FromQuery] bool? sinSegmento,
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaClientesSegmentoDto resultado = await _clasificacionUseCase.ExecuteAsync(
            segmentoId,
            sinSegmento,
            busqueda,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenSegmentacionDto resultado = await _resumenUseCase.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}/clientes")]
    public async Task<IActionResult> Clientes(int id, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaClientesSegmentoDto resultado = await _clientesSegmentoUseCase.ExecuteAsync(id, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        SegmentoDetalleDto resultado = await _obtenerUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSegmentoRequestDto solicitud)
    {
        int id = await _crearUseCase.ExecuteAsync(solicitud);
        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarSegmentoRequestDto solicitud)
    {
        int idActualizado = await _actualizarUseCase.ExecuteAsync(id, solicitud);
        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarUseCase.ExecuteAsync(id);
        return NoContent();
    }
}
