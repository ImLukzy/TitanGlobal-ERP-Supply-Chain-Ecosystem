using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

namespace TitanGlobal.Api.Controllers.Modulo59_CentrosCosto;

[ApiController]
[Authorize]
[Route("api/v1/centros-costo")]
public class CentrosCostoController : ControllerBase
{
    private readonly ListarCentrosCostoUseCase _listar;
    private readonly ObtenerCentroCostoUseCase _obtener;
    private readonly CrearCentroCostoUseCase _crear;
    private readonly ActualizarCentroCostoUseCase _actualizar;
    private readonly DesactivarCentroCostoUseCase _desactivar;
    private readonly ReactivarCentroCostoUseCase _reactivar;

    public CentrosCostoController(
        ListarCentrosCostoUseCase listar,
        ObtenerCentroCostoUseCase obtener,
        CrearCentroCostoUseCase crear,
        ActualizarCentroCostoUseCase actualizar,
        DesactivarCentroCostoUseCase desactivar,
        ReactivarCentroCostoUseCase reactivar)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _desactivar = desactivar;
        _reactivar = reactivar;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaCentrosCostoDto>> Listar(
        [FromQuery] bool? activo,
        [FromQuery] int? centroCostoPadreId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listar.ExecuteAsync(activo, centroCostoPadreId, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CentroCostoDetalleDto>> Obtener(int id)
    {
        return Ok(await _obtener.ExecuteAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CentroCostoDetalleDto>> Crear([FromBody] GuardarCentroCostoRequestDto solicitud)
    {
        CentroCostoDetalleDto centro = await _crear.ExecuteAsync(solicitud);
        return CreatedAtAction(nameof(Obtener), new { id = centro.Id }, centro);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    public async Task<ActionResult<CentroCostoDetalleDto>> Actualizar(int id, [FromBody] GuardarCentroCostoRequestDto solicitud)
    {
        return Ok(await _actualizar.ExecuteAsync(id, solicitud));
    }

    [HttpPost("{id:int}/desactivar")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<ActionResult<CentroCostoDetalleDto>> Desactivar(int id)
    {
        return Ok(await _desactivar.ExecuteAsync(id));
    }

    [HttpPost("{id:int}/reactivar")]
    [Authorize(Roles = "FINANZAS,ADMIN")]
    [PermitirCuerpoVacio]
    public async Task<ActionResult<CentroCostoDetalleDto>> Reactivar(int id)
    {
        return Ok(await _reactivar.ExecuteAsync(id));
    }
}
