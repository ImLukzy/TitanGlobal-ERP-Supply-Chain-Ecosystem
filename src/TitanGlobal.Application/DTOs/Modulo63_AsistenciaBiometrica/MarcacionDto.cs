namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class MarcacionDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateTime Instante { get; set; }

    public string Origen { get; set; } = string.Empty;

    public string? Dispositivo { get; set; }

    public string? NumeroRegistro { get; set; }

    public string? Observacion { get; set; }
}
