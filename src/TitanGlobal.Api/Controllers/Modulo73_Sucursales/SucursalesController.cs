using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.UseCases.Modulo73_Sucursales;
namespace TitanGlobal.Api.Controllers.Modulo73_Sucursales;

[ApiController]
[Route("api/v1/sucursales")]
public class SucursalesController : ControllerBase
{
    private readonly ListarSucursalesUseCase _listar;
    private readonly ObtenerSucursalUseCase _obtener;
    private readonly CrearSucursalUseCase _crear;
    private readonly ActualizarSucursalUseCase _actualizar;
    private readonly EliminarSucursalUseCase _eliminar;
    private readonly ActivarSucursalUseCase _activar;
    private readonly DesactivarSucursalUseCase _desactivar;
    private readonly ResumenSucursalesUseCase _resumen;
    public SucursalesController(ListarSucursalesUseCase listar, ObtenerSucursalUseCase obtener, CrearSucursalUseCase crear, ActualizarSucursalUseCase actualizar, EliminarSucursalUseCase eliminar, ActivarSucursalUseCase activar, DesactivarSucursalUseCase desactivar, ResumenSucursalesUseCase resumen)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _eliminar = eliminar;
        _activar = activar;
        _desactivar = desactivar;
        _resumen = resumen;
    }
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] string? tipo, [FromQuery] string? pais, [FromQuery] bool? activa, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        return Ok(await _listar.ExecuteAsync(busqueda, tipo, pais, activa, pagina, tamanoPagina));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        return Ok(await _obtener.ExecuteAsync(id));
    }
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarSucursalRequestDto datos)
    {
        int resultado = await _crear.ExecuteAsync(datos);
        return Ok(new { id = resultado });
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarSucursalRequestDto datos)
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
    [HttpPost("{id:int}/activacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Activar(int id)
    {
        int resultado = await _activar.ExecuteAsync(id);
        return Ok(new { id = resultado });
    }
    [HttpPost("{id:int}/desactivacion")]
    [PermitirCuerpoVacio]
    public async Task<IActionResult> Desactivar(int id)
    {
        int resultado = await _desactivar.ExecuteAsync(id);
        return Ok(new { id = resultado });
    }
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        return Ok(await _resumen.ExecuteAsync());
    }
}
