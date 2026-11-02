using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

namespace TitanGlobal.Api.Controllers.Modulo75_ImpuestosTasas;

[ApiController]
[Route("api/v1/impuestos")]
public class ImpuestosController : ControllerBase
{
    private readonly CrearImpuestoUseCase _crear;
    private readonly ListarImpuestosUseCase _listar;
    private readonly ObtenerImpuestoUseCase _obtener;
    private readonly ActualizarImpuestoUseCase _actualizar;
    private readonly DesactivarImpuestoUseCase _desactivar;
    private readonly RegistrarTasaImpuestoUseCase _registrarTasa;
    private readonly CalcularImpuestoUseCase _calcular;

    public ImpuestosController(
        CrearImpuestoUseCase crear,
        ListarImpuestosUseCase listar,
        ObtenerImpuestoUseCase obtener,
        ActualizarImpuestoUseCase actualizar,
        DesactivarImpuestoUseCase desactivar,
        RegistrarTasaImpuestoUseCase registrarTasa,
        CalcularImpuestoUseCase calcular)
    {
        _crear = crear;
        _listar = listar;
        _obtener = obtener;
        _actualizar = actualizar;
        _desactivar = desactivar;
        _registrarTasa = registrarTasa;
        _calcular = calcular;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _listar.Ejecutar());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        return Ok(await _obtener.Ejecutar(id));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] GuardarImpuestoRequestDto dto)
    {
        int id = await _crear.Ejecutar(dto);
        return Created($"/api/v1/impuestos/{id}", new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        [FromBody] GuardarImpuestoRequestDto dto)
    {
        await _actualizar.Ejecutar(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _desactivar.Ejecutar(id);
        return NoContent();
    }

    [HttpPost("{id:int}/tasas")]
    public async Task<IActionResult> RegistrarTasa(
        int id,
        [FromBody] GuardarTasaImpuestoRequestDto dto)
    {
        int tasaId = await _registrarTasa.Ejecutar(id, dto);
        return StatusCode(201, new { id = tasaId, impuestoId = id });
    }

    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular(
        [FromBody] CalcularImpuestoRequestDto dto)
    {
        return Ok(await _calcular.Ejecutar(dto));
    }
}
