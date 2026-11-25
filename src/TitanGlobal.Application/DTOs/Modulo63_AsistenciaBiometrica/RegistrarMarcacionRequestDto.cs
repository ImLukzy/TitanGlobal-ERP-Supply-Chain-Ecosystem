namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class RegistrarMarcacionRequestDto
{
    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public DateTime? Instante { get; set; }

    public string? Observacion { get; set; }
}
