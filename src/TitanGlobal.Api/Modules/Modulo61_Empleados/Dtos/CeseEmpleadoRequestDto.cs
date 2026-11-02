namespace TitanGlobal.Api.Modules.Modulo61_Empleados.Dtos;

public class CeseEmpleadoRequestDto
{
    public DateOnly? FechaCese { get; set; }

    public string? Motivo { get; set; }
}
