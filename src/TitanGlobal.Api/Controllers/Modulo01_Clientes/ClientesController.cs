using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Application.UseCases.Modulo01_Clientes;

namespace TitanGlobal.Api.Controllers.Modulo01_Clientes;

[ApiController]
[Route("api/v1/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ListarClientesUseCase _listarClientesUseCase;

    private readonly ObtenerClienteUseCase _obtenerClienteUseCase;

    private readonly CrearClienteUseCase _crearClienteUseCase;

    private readonly ActualizarClienteUseCase _actualizarClienteUseCase;

    private readonly EliminarClienteUseCase _eliminarClienteUseCase;

    private readonly ResumenClientesUseCase _resumenClientesUseCase;

    public ClientesController(
        ListarClientesUseCase listarClientesUseCase,
        ObtenerClienteUseCase obtenerClienteUseCase,
        CrearClienteUseCase crearClienteUseCase,
        ActualizarClienteUseCase actualizarClienteUseCase,
        EliminarClienteUseCase eliminarClienteUseCase,
        ResumenClientesUseCase resumenClientesUseCase)
    {
        _listarClientesUseCase = listarClientesUseCase;
        _obtenerClienteUseCase = obtenerClienteUseCase;
        _crearClienteUseCase = crearClienteUseCase;
        _actualizarClienteUseCase = actualizarClienteUseCase;
        _eliminarClienteUseCase = eliminarClienteUseCase;
        _resumenClientesUseCase = resumenClientesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaClientesDto resultado = await _listarClientesUseCase.ExecuteAsync(busqueda, pagina, tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenClientesDto resultado = await _resumenClientesUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ClienteDetalleDto resultado = await _obtenerClienteUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarClienteRequestDto solicitud)
    {
        int id = await _crearClienteUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarClienteRequestDto solicitud)
    {
        int idActualizado = await _actualizarClienteUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarClienteUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
