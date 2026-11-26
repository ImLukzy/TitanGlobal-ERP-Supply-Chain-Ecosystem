namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class RegistrarExamenMedicoRequestDto
{
    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public DateOnly? Fecha { get; set; }

    public string? Resultado { get; set; }

    public string? Restricciones { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public string? Clinica { get; set; }
}
