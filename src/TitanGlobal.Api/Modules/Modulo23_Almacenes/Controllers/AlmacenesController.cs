using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Api.Modules.Modulo23_Almacenes.Dtos;
using TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.Controllers;

[ApiController]
[Route("api/v1/almacenes")]
public class AlmacenesController : ControllerBase
{
    private readonly ListarAlmacenesUseCase _listarAlmacenesUseCase;

    private readonly ObtenerAlmacenUseCase _obtenerAlmacenUseCase;

    private readonly CrearAlmacenUseCase _crearAlmacenUseCase;

    private readonly ActualizarAlmacenUseCase _actualizarAlmacenUseCase;

    private readonly EliminarAlmacenUseCase _eliminarAlmacenUseCase;

    private readonly ListarCiudadesAlmacenUseCase _listarCiudadesAlmacenUseCase;

    private readonly ResumenAlmacenesUseCase _resumenAlmacenesUseCase;

    public AlmacenesController(
        ListarAlmacenesUseCase listarAlmacenesUseCase,
        ObtenerAlmacenUseCase obtenerAlmacenUseCase,
        CrearAlmacenUseCase crearAlmacenUseCase,
        ActualizarAlmacenUseCase actualizarAlmacenUseCase,
        EliminarAlmacenUseCase eliminarAlmacenUseCase,
        ListarCiudadesAlmacenUseCase listarCiudadesAlmacenUseCase,
        ResumenAlmacenesUseCase resumenAlmacenesUseCase)
    {
        _listarAlmacenesUseCase = listarAlmacenesUseCase;
        _obtenerAlmacenUseCase = obtenerAlmacenUseCase;
        _crearAlmacenUseCase = crearAlmacenUseCase;
        _actualizarAlmacenUseCase = actualizarAlmacenUseCase;
        _eliminarAlmacenUseCase = eliminarAlmacenUseCase;
        _listarCiudadesAlmacenUseCase = listarCiudadesAlmacenUseCase;
        _resumenAlmacenesUseCase = resumenAlmacenesUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? ciudad,
        [FromQuery] string? tipo,
        [FromQuery] bool? activo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        PaginaAlmacenesDto resultado = await _listarAlmacenesUseCase.ExecuteAsync(
            busqueda,
            ciudad,
            tipo,
            activo,
            pagina,
            tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("ciudades")]
    public async Task<IActionResult> Ciudades()
    {
        List<string> resultado = await _listarCiudadesAlmacenUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        ResumenAlmacenesDto resultado = await _resumenAlmacenesUseCase.ExecuteAsync();

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        AlmacenDetalleDto resultado = await _obtenerAlmacenUseCase.ExecuteAsync(id);

        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarAlmacenRequestDto solicitud)
    {
        int id = await _crearAlmacenUseCase.ExecuteAsync(solicitud);

        return Ok(new { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarAlmacenRequestDto solicitud)
    {
        int idActualizado = await _actualizarAlmacenUseCase.ExecuteAsync(id, solicitud);

        return Ok(new { Id = idActualizado });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _eliminarAlmacenUseCase.ExecuteAsync(id);

        return NoContent();
    }
}
