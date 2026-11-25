namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class CapacitacionEmpleadoDto
{
    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public int Anio { get; set; }

    public decimal Horas { get; set; }

    public List<CursoEmpleadoDto> Cursos { get; set; } = new List<CursoEmpleadoDto>();

    public List<CertificacionEmpleadoDto> Certificaciones { get; set; } = new List<CertificacionEmpleadoDto>();
}
