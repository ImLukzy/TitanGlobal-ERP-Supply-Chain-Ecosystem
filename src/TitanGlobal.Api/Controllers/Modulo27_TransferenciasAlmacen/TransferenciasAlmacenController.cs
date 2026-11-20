using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;
using TitanGlobal.Application.UseCases.Modulo27_TransferenciasAlmacen;

namespace TitanGlobal.Api.Controllers.Modulo27_TransferenciasAlmacen;

[ApiController]
[Route("api/v1/transferencias-almacen")]
public class TransferenciasAlmacenController : ControllerBase
{
    private readonly ListarTransferenciasUseCase _listarTransferenciasUseCase;

    private readonly ObtenerTransferenciaUseCase _obtenerTransferenciaUseCase;

    private readonly RegistrarTransferenciaUseCase _registrarTransferenciaUseCase;

    private readonly ResumenTransferenciasUseCase _resumenTransferenciasUseCase;

    public TransferenciasAlmacenController(
        ListarTransferenciasUseCase listarTransferenciasUseCase,
        ObtenerTransferenciaUseCase obtenerTransferenciaUseCase,
        RegistrarTransferenciaUseCase registrarTransferenciaUseCase,
        ResumenTransferenciasUseCase resumenTransferenciasUseCase)
    {
        _listarTransferenciasUseCase = listarTransferenciasUseCase;
        _obtenerTransferenciaUseCase = obtenerTransferenciaUseCase;
        _registrarTransferenciaUseCase = registrarTransferenciaUseCase;
        _resumenTransferenciasUseCase = resumenTransferenciasUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? productoId,
        [FromQuery] int? almacenOrigenId,
        [FromQuery] int? almacenDestinoId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaTransferenciasDto resultado = await _listarTransferenciasUseCase.ExecuteAsync(
            productoId,
            almacenOrigenId,
            almacenDestinoId,
            fechaDesde,
            fechaHasta,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenTransferenciasDto resultado = await _resumenTransferenciasUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        TransferenciaDetalleDto resultado = await _obtenerTransferenciaUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] GuardarTransferenciaRequestDto solicitud)
    {
        TransferenciaCreadaDto resultado = await _registrarTransferenciaUseCase.ExecuteAsync(solicitud);

        return Ok(resultado);
    }
}
