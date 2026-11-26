using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos;

namespace TitanGlobal.Api.Controllers.Modulo65_VacacionesPermisos;

[ApiController]
[Route("api/v1/ausencias")]
public class AusenciasController : ControllerBase
{
    private readonly AdministrarAusenciasUseCase _administrarAusencias;

    public AusenciasController(AdministrarAusenciasUseCase administrarAusencias)
    {
        _administrarAusencias = administrarAusencias;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? empleadoId,
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaAusenciasDto resultado = await _administrarAusencias.ListarAsync(
            busqueda,
            empleadoId,
            tipo,
            estado,
            desde,
            hasta,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAusenciasDto resultado = await _administrarAusencias.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        AusenciaDetalleDto resultado = await _administrarAusencias.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpGet("saldo-vacacional/{empleadoId:int}")]
    public async Task<IActionResult> ObtenerSaldo(int empleadoId, [FromQuery] DateOnly? fecha)
    {
        SaldoVacacionalDto resultado = await _administrarAusencias.ObtenerSaldoAsync(empleadoId, fecha);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarAusenciaRequestDto solicitud)
    {
        AusenciaCreadaDto resultado = await _administrarAusencias.RegistrarAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aprobar(int id)
    {
        AusenciaIdDto resultado = await _administrarAusencias.AprobarAsync(id, ObtenerUsuario());
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazo")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarAusenciaRequestDto solicitud)
    {
        AusenciaIdDto resultado = await _administrarAusencias.RechazarAsync(id, solicitud, ObtenerUsuario());
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cancelacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cancelar(int id)
    {
        AusenciaIdDto resultado = await _administrarAusencias.CancelarAsync(id);
        return Ok(resultado);
    }

    // El JWT nombra la reclamación "name" y AutenticacionJwtExtensions la usa como nombre de la identidad.
    private string ObtenerUsuario()
    {
        return User.Identity?.Name ?? string.Empty;
    }
}
