using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Application.UseCases.Modulo16_Importaciones;

namespace TitanGlobal.Api.Controllers.Modulo16_Importaciones;

[ApiController]
[Route("api/v1/importaciones")]
public class ImportacionesController : ControllerBase
{
    private readonly ListarExpedientesUseCase _listarExpedientesUseCase;

    private readonly ObtenerExpedienteUseCase _obtenerExpedienteUseCase;

    private readonly CrearExpedienteUseCase _crearExpedienteUseCase;

    private readonly ActualizarExpedienteUseCase _actualizarExpedienteUseCase;

    private readonly EliminarExpedienteUseCase _eliminarExpedienteUseCase;

    private readonly EmbarcarExpedienteUseCase _embarcarExpedienteUseCase;

    private readonly ArribarExpedienteUseCase _arribarExpedienteUseCase;

    private readonly LiberarExpedienteUseCase _liberarExpedienteUseCase;

    private readonly CancelarExpedienteUseCase _cancelarExpedienteUseCase;

    private readonly ResumenExpedientesUseCase _resumenExpedientesUseCase;

    public ImportacionesController(
        ListarExpedientesUseCase listarExpedientesUseCase,
        ObtenerExpedienteUseCase obtenerExpedienteUseCase,
        CrearExpedienteUseCase crearExpedienteUseCase,
        ActualizarExpedienteUseCase actualizarExpedienteUseCase,
        EliminarExpedienteUseCase eliminarExpedienteUseCase,
        EmbarcarExpedienteUseCase embarcarExpedienteUseCase,
        ArribarExpedienteUseCase arribarExpedienteUseCase,
        LiberarExpedienteUseCase liberarExpedienteUseCase,
        CancelarExpedienteUseCase cancelarExpedienteUseCase,
        ResumenExpedientesUseCase resumenExpedientesUseCase)
    {
        _listarExpedientesUseCase = listarExpedientesUseCase;
        _obtenerExpedienteUseCase = obtenerExpedienteUseCase;
        _crearExpedienteUseCase = crearExpedienteUseCase;
        _actualizarExpedienteUseCase = actualizarExpedienteUseCase;
        _eliminarExpedienteUseCase = eliminarExpedienteUseCase;
        _embarcarExpedienteUseCase = embarcarExpedienteUseCase;
        _arribarExpedienteUseCase = arribarExpedienteUseCase;
        _liberarExpedienteUseCase = liberarExpedienteUseCase;
        _cancelarExpedienteUseCase = cancelarExpedienteUseCase;
        _resumenExpedientesUseCase = resumenExpedientesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] string? incoterm,
        [FromQuery] string? medioTransporte,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaExpedientesDto resultado = await _listarExpedientesUseCase.ExecuteAsync(
            busqueda,
            estado,
            incoterm,
            medioTransporte,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenExpedientesDto resultado = await _resumenExpedientesUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ExpedienteDetalleDto resultado = await _obtenerExpedienteUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearExpedienteRequestDto solicitud)
    {
        int id = await _crearExpedienteUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarExpedienteRequestDto solicitud)
    {
        int idActualizado = await _actualizarExpedienteUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarExpedienteUseCase.ExecuteAsync(id);

        return NoContent();
    }

    [HttpPost("{id:int}/embarcar")]
    public async Task<IActionResult> Embarcar(int id, [FromBody] EmbarcarExpedienteRequestDto solicitud)
    {
        int idEmbarcado = await _embarcarExpedienteUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idEmbarcado });
    }

    [HttpPost("{id:int}/arribar")]
    public async Task<IActionResult> Arribar(int id, [FromBody] ArribarExpedienteRequestDto solicitud)
    {
        int idArribado = await _arribarExpedienteUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idArribado });
    }

    [HttpPost("{id:int}/liberar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Liberar(int id)
    {
        int idLiberado = await _liberarExpedienteUseCase.ExecuteAsync(id);

        return Ok(new { Id = idLiberado });
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarExpedienteRequestDto solicitud)
    {
        int idCancelado = await _cancelarExpedienteUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idCancelado });
    }
}
