namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class ExamenMedicoDto
{
    public int Id { get; set; }

    public int EmpleadoId { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public string Resultado { get; set; } = string.Empty;

    public string? Restricciones { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public string? Clinica { get; set; }

    public bool Vencido { get; set; }
}
