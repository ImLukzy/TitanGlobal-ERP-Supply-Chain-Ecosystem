using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Controllers;

[ApiController]
[Route("api/v1/ordenes-abastecimiento")]
public class OrdenesAbastecimientoController : ControllerBase
{
    private readonly AnularOrdenAbastecimientoUseCase _anularOrdenAbastecimientoUseCase;

    private readonly ActualizarOrdenAbastecimientoUseCase _actualizarOrdenAbastecimientoUseCase;

    private readonly CrearOrdenAbastecimientoUseCase _crearOrdenAbastecimientoUseCase;

    private readonly GenerarOrdenDesdeSolicitudUseCase _generarOrdenDesdeSolicitudUseCase;

    private readonly ListarOrdenesAbastecimientoUseCase _listarOrdenesAbastecimientoUseCase;

    private readonly ObtenerOrdenAbastecimientoUseCase _obtenerOrdenAbastecimientoUseCase;

    private readonly RecibirOrdenAbastecimientoUseCase _recibirOrdenAbastecimientoUseCase;

    private readonly ResumenOrdenesAbastecimientoUseCase _resumenOrdenesAbastecimientoUseCase;

    public OrdenesAbastecimientoController(
        AnularOrdenAbastecimientoUseCase anularOrdenAbastecimientoUseCase,
        ActualizarOrdenAbastecimientoUseCase actualizarOrdenAbastecimientoUseCase,
        CrearOrdenAbastecimientoUseCase crearOrdenAbastecimientoUseCase,
        GenerarOrdenDesdeSolicitudUseCase generarOrdenDesdeSolicitudUseCase,
        ListarOrdenesAbastecimientoUseCase listarOrdenesAbastecimientoUseCase,
        ObtenerOrdenAbastecimientoUseCase obtenerOrdenAbastecimientoUseCase,
        RecibirOrdenAbastecimientoUseCase recibirOrdenAbastecimientoUseCase,
        ResumenOrdenesAbastecimientoUseCase resumenOrdenesAbastecimientoUseCase)
    {
        _anularOrdenAbastecimientoUseCase = anularOrdenAbastecimientoUseCase;
        _actualizarOrdenAbastecimientoUseCase = actualizarOrdenAbastecimientoUseCase;
        _crearOrdenAbastecimientoUseCase = crearOrdenAbastecimientoUseCase;
        _generarOrdenDesdeSolicitudUseCase = generarOrdenDesdeSolicitudUseCase;
        _listarOrdenesAbastecimientoUseCase = listarOrdenesAbastecimientoUseCase;
        _obtenerOrdenAbastecimientoUseCase = obtenerOrdenAbastecimientoUseCase;
        _recibirOrdenAbastecimientoUseCase = recibirOrdenAbastecimientoUseCase;
        _resumenOrdenesAbastecimientoUseCase = resumenOrdenesAbastecimientoUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaOrdenesAbastecimientoDto>> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? proveedorId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaOrdenesAbastecimientoDto resultado = await _listarOrdenesAbastecimientoUseCase.ExecuteAsync(
            busqueda,
            estado,
            proveedorId,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenOrdenesAbastecimientoDto>> Resumen()
    {
        ResumenOrdenesAbastecimientoDto resultado = await _resumenOrdenesAbastecimientoUseCase.ExecuteAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdenAbastecimientoDetalleDto>> Obtener(int id)
    {
        OrdenAbastecimientoDetalleDto resultado = await _obtenerOrdenAbastecimientoUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<OrdenAbastecimientoCreadaDto>> Crear(
        [FromBody] GuardarOrdenAbastecimientoRequestDto solicitud)
    {
        OrdenAbastecimientoCreadaDto resultado = await _crearOrdenAbastecimientoUseCase.ExecuteAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPost("desde-solicitud/{solicitudId:int}")]
    public async Task<ActionResult<OrdenAbastecimientoCreadaDto>> GenerarDesdeSolicitud(
        int solicitudId,
        [FromBody] GenerarDesdeSolicitudRequestDto solicitud)
    {
        OrdenAbastecimientoCreadaDto resultado = await _generarOrdenDesdeSolicitudUseCase.ExecuteAsync(
            solicitudId,
            solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrdenAbastecimientoCreadaDto>> Actualizar(
        int id,
        [FromBody] GuardarOrdenAbastecimientoRequestDto solicitud)
    {
        OrdenAbastecimientoCreadaDto resultado = await _actualizarOrdenAbastecimientoUseCase.ExecuteAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/recibir")]
    [PermitirCuerpoVacio]
    public async Task<ActionResult<OrdenAbastecimientoDetalleDto>> Recibir(int id)
    {
        OrdenAbastecimientoDetalleDto resultado = await _recibirOrdenAbastecimientoUseCase.ExecuteAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/anular")]
    public async Task<ActionResult<OrdenAbastecimientoDetalleDto>> Anular(
        int id,
        [FromBody] AnularOrdenRequestDto solicitud)
    {
        OrdenAbastecimientoDetalleDto resultado = await _anularOrdenAbastecimientoUseCase.ExecuteAsync(id, solicitud);
        return Ok(resultado);
    }
}
