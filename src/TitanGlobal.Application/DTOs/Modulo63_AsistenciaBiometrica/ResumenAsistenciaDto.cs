namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class ResumenAsistenciaDto
{
    public int Jornadas { get; set; }

    public int Asistencias { get; set; }

    public int Tardanzas { get; set; }

    public int SinMarcacion { get; set; }

    public int Faltas { get; set; }

    public int MinutosTardanza { get; set; }

    public int MinutosExtraPendientes { get; set; }

    public int MinutosExtraAprobados { get; set; }
}
