namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class CalcularJornadasRequestDto
{
    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public int? EmpleadoId { get; set; }
}
