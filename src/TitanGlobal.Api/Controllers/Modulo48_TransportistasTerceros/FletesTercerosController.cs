using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

namespace TitanGlobal.Api.Controllers.Modulo48_TransportistasTerceros;

[ApiController]
[Route("api/v1/fletes-terceros")]
public class FletesTercerosController : ControllerBase
{
    private readonly ListarFletesTercerosUseCase _listarFletesTercerosUseCase;
    private readonly ObtenerFleteTerceroUseCase _obtenerFleteTerceroUseCase;
    private readonly RegistrarFleteTerceroUseCase _registrarFleteTerceroUseCase;
    private readonly CancelarFleteTerceroUseCase _cancelarFleteTerceroUseCase;

    public FletesTercerosController(ListarFletesTercerosUseCase listarFletesTercerosUseCase, ObtenerFleteTerceroUseCase obtenerFleteTerceroUseCase, RegistrarFleteTerceroUseCase registrarFleteTerceroUseCase, CancelarFleteTerceroUseCase cancelarFleteTerceroUseCase)
    {
        _listarFletesTercerosUseCase = listarFletesTercerosUseCase;
        _obtenerFleteTerceroUseCase = obtenerFleteTerceroUseCase;
        _registrarFleteTerceroUseCase = registrarFleteTerceroUseCase;
        _cancelarFleteTerceroUseCase = cancelarFleteTerceroUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? transportistaId, [FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        PaginaFletesTercerosDto resultado = await _listarFletesTercerosUseCase.ExecuteAsync(transportistaId, busqueda, estado, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        FleteTerceroDetalleDto resultado = await _obtenerFleteTerceroUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarFleteTerceroRequestDto solicitud)
    {
        int resultado = await _registrarFleteTerceroUseCase.ExecuteAsync(solicitud);
        return Ok(new { Id = resultado });
    }

    [HttpPost("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarFleteTerceroRequestDto solicitud)
    {
        int resultado = await _cancelarFleteTerceroUseCase.ExecuteAsync(id, solicitud);
        return Ok(new { Id = resultado });
    }
}
