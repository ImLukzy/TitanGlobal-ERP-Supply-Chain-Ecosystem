using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos;

namespace TitanGlobal.Api.Controllers.Modulo67_AdelantosPrestamos;

[ApiController]
[Route("api/v1/prestamos")]
public class PrestamosController : ControllerBase
{
    private readonly AdministrarPrestamosUseCase _administrarPrestamos;

    public PrestamosController(AdministrarPrestamosUseCase administrarPrestamos)
    {
        _administrarPrestamos = administrarPrestamos;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? empleadoId,
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaPrestamosDto resultado = await _administrarPrestamos.ListarAsync(
            busqueda,
            empleadoId,
            tipo,
            estado,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenPrestamosDto resultado = await _administrarPrestamos.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        PrestamoDetalleDto resultado = await _administrarPrestamos.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpGet("capacidad/{empleadoId:int}")]
    public async Task<IActionResult> ObtenerCapacidad(int empleadoId, [FromQuery] DateOnly? fecha)
    {
        CapacidadPrestamoDto resultado = await _administrarPrestamos.ObtenerCapacidadAsync(empleadoId, fecha);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPrestamoRequestDto solicitud)
    {
        PrestamoCreadoDto resultado = await _administrarPrestamos.RegistrarAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/anulacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularPrestamoRequestDto solicitud)
    {
        PrestamoIdDto resultado = await _administrarPrestamos.AnularAsync(id, solicitud);
        return Ok(resultado);
    }
}
