using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

namespace TitanGlobal.Api.Controllers.Modulo28_TomaInventario;

[ApiController]
[Route("api/v1/tomas-inventario")]
public class TomasInventarioController : ControllerBase
{
    private readonly ListarTomasInventarioUseCase _listarTomasInventarioUseCase;

    private readonly ObtenerTomaInventarioUseCase _obtenerTomaInventarioUseCase;

    private readonly ResumenTomasInventarioUseCase _resumenTomasInventarioUseCase;

    private readonly CrearTomaInventarioUseCase _crearTomaInventarioUseCase;

    private readonly ActualizarTomaInventarioUseCase _actualizarTomaInventarioUseCase;

    private readonly CerrarTomaInventarioUseCase _cerrarTomaInventarioUseCase;

    private readonly AnularTomaInventarioUseCase _anularTomaInventarioUseCase;

    public TomasInventarioController(
        ListarTomasInventarioUseCase listarTomasInventarioUseCase,
        ObtenerTomaInventarioUseCase obtenerTomaInventarioUseCase,
        ResumenTomasInventarioUseCase resumenTomasInventarioUseCase,
        CrearTomaInventarioUseCase crearTomaInventarioUseCase,
        ActualizarTomaInventarioUseCase actualizarTomaInventarioUseCase,
        CerrarTomaInventarioUseCase cerrarTomaInventarioUseCase,
        AnularTomaInventarioUseCase anularTomaInventarioUseCase)
    {
        _listarTomasInventarioUseCase = listarTomasInventarioUseCase;
        _obtenerTomaInventarioUseCase = obtenerTomaInventarioUseCase;
        _resumenTomasInventarioUseCase = resumenTomasInventarioUseCase;
        _crearTomaInventarioUseCase = crearTomaInventarioUseCase;
        _actualizarTomaInventarioUseCase = actualizarTomaInventarioUseCase;
        _cerrarTomaInventarioUseCase = cerrarTomaInventarioUseCase;
        _anularTomaInventarioUseCase = anularTomaInventarioUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? almacenId,
        [FromQuery] string? estado,
        [FromQuery] string? busqueda,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaTomasInventarioDto resultado = await _listarTomasInventarioUseCase.ExecuteAsync(
            almacenId,
            estado,
            busqueda,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenTomasInventarioDto resultado = await _resumenTomasInventarioUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        TomaInventarioDetalleDto resultado = await _obtenerTomaInventarioUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearTomaInventarioRequestDto solicitud)
    {
        TomaInventarioDetalleDto resultado = await _crearTomaInventarioUseCase.ExecuteAsync(solicitud);

        return CreatedAtAction(nameof(Obtener), new { id = resultado.Id }, resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarTomaInventarioRequestDto solicitud)
    {
        TomaInventarioDetalleDto resultado = await _actualizarTomaInventarioUseCase.ExecuteAsync(id, solicitud);

        return Ok(resultado);
    }

    [HttpPost("{id:int}/cerrar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        TomaInventarioDetalleDto resultado = await _cerrarTomaInventarioUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost("{id:int}/anular")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Anular(int id)
    {
        TomaInventarioDetalleDto resultado = await _anularTomaInventarioUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }
}
