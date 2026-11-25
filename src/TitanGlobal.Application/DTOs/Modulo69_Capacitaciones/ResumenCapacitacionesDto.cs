namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class ResumenCapacitacionesDto
{
    public int Anio { get; set; }

    public int CursosActivos { get; set; }

    public int Programadas { get; set; }

    public int Realizadas { get; set; }

    public int Inscritos { get; set; }

    public decimal HorasTotales { get; set; }

    public int CertificacionesVencidas { get; set; }
}
