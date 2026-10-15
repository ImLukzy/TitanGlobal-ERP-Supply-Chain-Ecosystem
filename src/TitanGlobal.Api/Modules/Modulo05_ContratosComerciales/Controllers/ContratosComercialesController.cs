using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.Dtos;
using TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.UseCases;

namespace TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.Controllers;

[ApiController]
[Route("api/v1/contratos-comerciales")]
public class ContratosComercialesController : ControllerBase
{
    private readonly ContratosComercialesUseCase _casoDeUso;

    public ContratosComercialesController(ContratosComercialesUseCase casoDeUso)
    {
        _casoDeUso = casoDeUso;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int? clienteId,
        [FromQuery] bool? vencidos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaContratosDto resultado = await _casoDeUso.ListarAsync(
            busqueda, estado, clienteId, vencidos, pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenContratosDto resultado = await _casoDeUso.ResumenAsync();
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        ContratoDetalleDto resultado = await _casoDeUso.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarContratoRequestDto solicitud)
    {
        ContratoCreadoDto resultado = await _casoDeUso.CrearAsync(solicitud);
        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarContratoRequestDto solicitud)
    {
        ContratoCreadoDto resultado = await _casoDeUso.ActualizarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/activar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        ContratoDetalleDto resultado = await _casoDeUso.ActivarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/finalizar")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Finalizar(int id)
    {
        ContratoDetalleDto resultado = await _casoDeUso.FinalizarAsync(id);
        return Ok(resultado);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarContratoRequestDto solicitud)
    {
        ContratoDetalleDto resultado = await _casoDeUso.CancelarAsync(id, solicitud);
        return Ok(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _casoDeUso.EliminarAsync(id);
        return NoContent();
    }
}
