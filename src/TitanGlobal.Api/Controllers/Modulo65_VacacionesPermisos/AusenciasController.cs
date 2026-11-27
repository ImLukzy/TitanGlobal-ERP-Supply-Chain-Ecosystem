using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Commands;
using TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Queries;

namespace TitanGlobal.Api.Controllers.Modulo65_VacacionesPermisos;

[ApiController]
[Route("api/v1/ausencias")]
public class AusenciasController : ControllerBase
{
    private readonly ISender _sender;

    public AusenciasController(ISender sender)
    {
        _sender = sender;
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
        PaginaAusenciasDto resultado = await _sender.Send(new ListarAusenciasQuery
        {
            Busqueda = busqueda,
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Estado = estado,
            Desde = desde,
            Hasta = hasta,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAusenciasDto resultado = await _sender.Send(new ResumenAusenciasQuery());
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        AusenciaDetalleDto resultado = await _sender.Send(new ObtenerAusenciaQuery { Id = id });
        return Ok(resultado);
    }

    [HttpGet("saldo-vacacional/{empleadoId:int}")]
    public async Task<IActionResult> ObtenerSaldo(int empleadoId, [FromQuery] DateOnly? fecha)
    {
        SaldoVacacionalDto resultado = await _sender.Send(new ObtenerSaldoQuery
        {
            EmpleadoId = empleadoId,
            Fecha = fecha
        });
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarAusenciaRequestDto solicitud)
    {
        AusenciaCreadaDto resultado = await _sender.Send(new RegistrarAusenciaCommand { Solicitud = solicitud });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/aprobacion")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Aprobar(int id)
    {
        AusenciaIdDto resultado = await _sender.Send(new AprobarAusenciaCommand
        {
            Id = id,
            Usuario = ObtenerUsuario()
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/rechazo")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarAusenciaRequestDto solicitud)
    {
        AusenciaIdDto resultado = await _sender.Send(new RechazarAusenciaCommand
        {
            Id = id,
            Solicitud = solicitud,
            Usuario = ObtenerUsuario()
        });
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cancelacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cancelar(int id)
    {
        AusenciaIdDto resultado = await _sender.Send(new CancelarAusenciaCommand { Id = id });
        return Ok(resultado);
    }

    // El JWT nombra la reclamación "name" y AutenticacionJwtExtensions la usa como nombre de la identidad.
    private string ObtenerUsuario()
    {
        return User.Identity?.Name ?? string.Empty;
    }
}
