using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;
namespace TitanGlobal.Api.Controllers.Modulo49_LogisticaInversa;

[ApiController]
[Route("api/v1/logistica-inversa")]
public class LogisticaInversaController : ControllerBase
{
    private readonly ListarRetornosUseCase _listar;
    private readonly ObtenerRetornoUseCase _obtener;
    private readonly CrearRetornoUseCase _crear;
    private readonly ActualizarRetornoUseCase _actualizar;
    private readonly EliminarRetornoUseCase _eliminar;
    private readonly RecogerRetornoUseCase _recoger;
    private readonly RecibirRetornoUseCase _recibir;
    private readonly CancelarRetornoUseCase _cancelar;
    private readonly ResumenRetornosUseCase _resumen;
    public LogisticaInversaController(ListarRetornosUseCase listar, ObtenerRetornoUseCase obtener, CrearRetornoUseCase crear, ActualizarRetornoUseCase actualizar, EliminarRetornoUseCase eliminar, RecogerRetornoUseCase recoger, RecibirRetornoUseCase recibir, CancelarRetornoUseCase cancelar, ResumenRetornosUseCase resumen)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _eliminar = eliminar;
        _recoger = recoger;
        _recibir = recibir;
        _cancelar = cancelar;
        _resumen = resumen;
    }
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? estado, [FromQuery] string? tipo, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listar.ExecuteAsync(busqueda, estado, tipo, pagina, tamanoPagina));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _obtener.ExecuteAsync(id));
    }
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarRetornoRequestDto datos)
    {
        int resultado = await _crear.ExecuteAsync(datos);
        return Ok(new { id = resultado });
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarRetornoRequestDto datos)
    {
        int resultado = await _actualizar.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminar.ExecuteAsync(id);
        return NoContent();
    }
    [HttpPost("{id:int}/recogida")]
    public async Task<IActionResult> Recoger(int id, [FromBody] RegistrarCantidadRetornoRequestDto datos)
    {
        int resultado = await _recoger.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpPost("{id:int}/recepcion")]
    public async Task<IActionResult> Recibir(int id, [FromBody] RegistrarCantidadRetornoRequestDto datos)
    {
        int resultado = await _recibir.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpPost("{id:int}/cancelacion")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarRetornoRequestDto datos)
    {
        int resultado = await _cancelar.ExecuteAsync(id, datos);
        return Ok(new { id = resultado });
    }
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _resumen.ExecuteAsync());
    }
}
