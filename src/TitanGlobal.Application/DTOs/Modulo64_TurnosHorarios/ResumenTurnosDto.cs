namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class ResumenTurnosDto
{
    public int TotalTurnos { get; set; }

    public int Activos { get; set; }

    public int Fijos { get; set; }

    public int Rotativos { get; set; }

    public int EmpleadosConTurnoHoy { get; set; }

    public int EmpleadosActivosSinTurnoHoy { get; set; }
}
