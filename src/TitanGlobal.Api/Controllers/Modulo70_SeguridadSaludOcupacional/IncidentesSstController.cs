using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

namespace TitanGlobal.Api.Controllers.Modulo70_SeguridadSaludOcupacional;

[ApiController]
[Route("api/v1/incidentes-sst")]
public class IncidentesSstController : ControllerBase
{
    private readonly AdministrarIncidentesSstUseCase _administrarIncidentes;
    private readonly ResumenSstUseCase _resumenSst;

    public IncidentesSstController(AdministrarIncidentesSstUseCase administrarIncidentes, ResumenSstUseCase resumenSst)
    {
        _administrarIncidentes = administrarIncidentes;
        _resumenSst = resumenSst;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? tipo,
        [FromQuery] string? severidad,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaIncidentesSstDto resultado = await _administrarIncidentes.ListarAsync(busqueda, tipo, severidad, estado, desde, hasta, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] int? anio)
    {
        ResumenSstDto resultado = await _resumenSst.ResumenAsync(anio);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        IncidenteSstDetalleDto resultado = await _administrarIncidentes.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarIncidenteSstRequestDto solicitud)
    {
        IncidenteSstCreadoDto resultado = await _administrarIncidentes.RegistrarAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/acciones")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> AgregarAccion(int id, [FromBody] GuardarAccionCorrectivaRequestDto solicitud)
    {
        AccionCorrectivaSstIdDto resultado = await _administrarIncidentes.AgregarAccionAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/acciones/{accionId:int}/completar")]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> CompletarAccion(int id, int accionId, [FromBody] CompletarAccionCorrectivaRequestDto solicitud)
    {
        AccionCorrectivaSstIdDto resultado = await _administrarIncidentes.CompletarAccionAsync(id, accionId, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cierre")]
    [Authorize(Roles = "RRHH,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Cerrar(int id)
    {
        IncidenteSstIdDto resultado = await _administrarIncidentes.CerrarAsync(id);
        return Ok(resultado);
    }
}
