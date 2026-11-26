namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class TurnoDetalleDto : TurnoListadoDto
{
    public int TotalAsignaciones { get; set; }

    public int EmpleadosConTurnoHoy { get; set; }
}
