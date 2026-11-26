using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

namespace TitanGlobal.Api.Controllers.Modulo70_SeguridadSaludOcupacional;

[ApiController]
[Route("api/v1/examenes-medicos")]
public class ExamenesMedicosController : ControllerBase
{
    private readonly AdministrarExamenesMedicosUseCase _administrarExamenes;

    public ExamenesMedicosController(AdministrarExamenesMedicosUseCase administrarExamenes)
    {
        _administrarExamenes = administrarExamenes;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? empleadoId,
        [FromQuery] string? tipo,
        [FromQuery] string? resultado,
        [FromQuery] bool? vencidos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        PaginaExamenesMedicosDto respuesta = await _administrarExamenes.ListarAsync(empleadoId, tipo, resultado, vencidos, pagina, tamanoPagina);
        return Ok(respuesta);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarExamenMedicoRequestDto solicitud)
    {
        ExamenMedicoIdDto resultado = await _administrarExamenes.RegistrarAsync(solicitud);
        return Ok(resultado);
    }
}
