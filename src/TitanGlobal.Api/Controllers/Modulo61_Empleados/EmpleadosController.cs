using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Application.UseCases.Modulo61_Empleados;

namespace TitanGlobal.Api.Controllers.Modulo61_Empleados;

[ApiController]
[Route("api/v1/empleados")]
public class EmpleadosController : ControllerBase
{
    private readonly AdministrarEmpleadosUseCase _administrarEmpleados;

    public EmpleadosController(AdministrarEmpleadosUseCase administrarEmpleados)
    {
        _administrarEmpleados = administrarEmpleados;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaEmpleadosDto resultado = await _administrarEmpleados.ListarAsync(
            busqueda,
            estado,
            pagina,
            tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        EmpleadoDetalleDto resultado = await _administrarEmpleados.ObtenerAsync(id);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarEmpleadoRequestDto solicitud)
    {
        int id = await _administrarEmpleados.CrearAsync(solicitud);
        return Ok(new EmpleadoIdDto { Id = id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarEmpleadoRequestDto solicitud)
    {
        int actualizado = await _administrarEmpleados.ActualizarAsync(id, solicitud);
        return Ok(new EmpleadoIdDto { Id = actualizado });
    }

    [HttpPost("{id:int}/cese")]
    public async Task<IActionResult> Cesar(int id, [FromBody] CeseEmpleadoRequestDto solicitud)
    {
        int cesado = await _administrarEmpleados.CesarAsync(id, solicitud);
        return Ok(new EmpleadoIdDto { Id = cesado });
    }
}
