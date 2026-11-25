using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

namespace TitanGlobal.Api.Controllers.Modulo48_TransportistasTerceros;

[ApiController]
[Route("api/v1/transportistas-terceros")]
public class TransportistasTercerosController : ControllerBase
{
    private readonly ListarTransportistasTercerosUseCase _listarTransportistasTercerosUseCase;
    private readonly ResumenTransportistasTercerosUseCase _resumenTransportistasTercerosUseCase;
    private readonly ObtenerTransportistaTerceroUseCase _obtenerTransportistaTerceroUseCase;
    private readonly CrearTransportistaTerceroUseCase _crearTransportistaTerceroUseCase;
    private readonly ActualizarTransportistaTerceroUseCase _actualizarTransportistaTerceroUseCase;
    private readonly EliminarTransportistaTerceroUseCase _eliminarTransportistaTerceroUseCase;

    public TransportistasTercerosController(ListarTransportistasTercerosUseCase listarTransportistasTercerosUseCase, ResumenTransportistasTercerosUseCase resumenTransportistasTercerosUseCase, ObtenerTransportistaTerceroUseCase obtenerTransportistaTerceroUseCase, CrearTransportistaTerceroUseCase crearTransportistaTerceroUseCase, ActualizarTransportistaTerceroUseCase actualizarTransportistaTerceroUseCase, EliminarTransportistaTerceroUseCase eliminarTransportistaTerceroUseCase)
    {
        _listarTransportistasTercerosUseCase = listarTransportistasTercerosUseCase;
        _resumenTransportistasTercerosUseCase = resumenTransportistasTercerosUseCase;
        _obtenerTransportistaTerceroUseCase = obtenerTransportistaTerceroUseCase;
        _crearTransportistaTerceroUseCase = crearTransportistaTerceroUseCase;
        _actualizarTransportistaTerceroUseCase = actualizarTransportistaTerceroUseCase;
        _eliminarTransportistaTerceroUseCase = eliminarTransportistaTerceroUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? pais, [FromQuery] string? tipoServicio, [FromQuery] bool? activo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaTransportistasTercerosDto resultado = await _listarTransportistasTercerosUseCase.ExecuteAsync(busqueda, pais, tipoServicio, activo, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenTransportistasTercerosDto resultado = await _resumenTransportistasTercerosUseCase.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        TransportistaTerceroDetalleDto resultado = await _obtenerTransportistaTerceroUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarTransportistaTerceroRequestDto solicitud)
    {
        int resultado = await _crearTransportistaTerceroUseCase.ExecuteAsync(solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarTransportistaTerceroRequestDto solicitud)
    {
        int resultado = await _actualizarTransportistaTerceroUseCase.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarTransportistaTerceroUseCase.ExecuteAsync(id);
        return NoContent();
    }
}
