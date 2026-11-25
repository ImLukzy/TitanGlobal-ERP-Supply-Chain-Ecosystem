namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class HorasEmpleadoDto
{
    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public int Cursos { get; set; }

    public decimal Horas { get; set; }
}
