using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.Dtos;
using TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.UseCases;

namespace TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.Controllers;

[ApiController]
[Route("api/v1/auditoria")]
public class AuditoriaController : ControllerBase
{
    private readonly ListarAuditoriaUseCase _listarAuditoriaUseCase;

    private readonly ObtenerRegistroAuditoriaUseCase _obtenerRegistroAuditoriaUseCase;

    private readonly ListarEntidadesAuditoriaUseCase _listarEntidadesAuditoriaUseCase;

    private readonly ResumenAuditoriaUseCase _resumenAuditoriaUseCase;

    public AuditoriaController(
        ListarAuditoriaUseCase listarAuditoriaUseCase,
        ObtenerRegistroAuditoriaUseCase obtenerRegistroAuditoriaUseCase,
        ListarEntidadesAuditoriaUseCase listarEntidadesAuditoriaUseCase,
        ResumenAuditoriaUseCase resumenAuditoriaUseCase)
    {
        _listarAuditoriaUseCase = listarAuditoriaUseCase;
        _obtenerRegistroAuditoriaUseCase = obtenerRegistroAuditoriaUseCase;
        _listarEntidadesAuditoriaUseCase = listarEntidadesAuditoriaUseCase;
        _resumenAuditoriaUseCase = resumenAuditoriaUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? entidad,
        [FromQuery] string? entidadId,
        [FromQuery] string? accion,
        [FromQuery] string? usuario,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAuditoriaDto resultado = await _listarAuditoriaUseCase.ExecuteAsync(
            entidad,
            entidadId,
            accion,
            usuario,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("entidades")]
    public async Task<IActionResult> ListarEntidades()
    {
        List<string> resultado = await _listarEntidadesAuditoriaUseCase.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAuditoriaDto resultado = await _resumenAuditoriaUseCase.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id)
    {
        RegistroDetalleDto resultado = await _obtenerRegistroAuditoriaUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }
}
