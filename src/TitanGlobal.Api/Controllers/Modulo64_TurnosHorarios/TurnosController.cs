using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

namespace TitanGlobal.Api.Controllers.Modulo64_TurnosHorarios;

[ApiController]
[Route("api/v1/turnos")]
public class TurnosController : ControllerBase
{
    private readonly AdministrarTurnosUseCase _administrarTurnos;

    public TurnosController(AdministrarTurnosUseCase administrarTurnos)
    {
        _administrarTurnos = administrarTurnos;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaTurnosDto resultado = await _administrarTurnos.ListarAsync(busqueda, tipo, activo, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenTurnosDto resultado = await _administrarTurnos.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        TurnoDetalleDto resultado = await _administrarTurnos.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Crear([FromBody] GuardarTurnoRequestDto solicitud)
    {
        TurnoIdDto resultado = await _administrarTurnos.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarTurnoRequestDto solicitud)
    {
        TurnoIdDto resultado = await _administrarTurnos.ActualizarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/desactivacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Desactivar(int id)
    {
        TurnoIdDto resultado = await _administrarTurnos.DesactivarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/activacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        TurnoIdDto resultado = await _administrarTurnos.ActivarAsync(id);
        return Ok(resultado);
    }
}
