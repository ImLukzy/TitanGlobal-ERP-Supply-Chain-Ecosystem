using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;
using TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

namespace TitanGlobal.Api.Controllers.Modulo70_SeguridadSaludOcupacional;

[ApiController]
[Route("api/v1/examenes-medicos")]
public class ExamenesMedicosController : ControllerBase
{
    private readonly ISender _sender;

    public ExamenesMedicosController(ISender sender)
    {
        _sender = sender;
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
        PaginaExamenesMedicosDto respuesta = await _sender.Send(new ListarExamenesMedicosQuery
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Resultado = resultado,
            Vencidos = vencidos,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
        return Ok(respuesta);
    }

    [HttpPost]
    [Authorize(Roles = "RRHH,ADMIN")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarExamenMedicoRequestDto solicitud)
    {
        ExamenMedicoIdDto resultado = await _sender.Send(new RegistrarExamenMedicoCommand
        {
            Solicitud = solicitud
        });
        return Ok(resultado);
    }
}
