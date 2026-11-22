namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class GuardarTurnoRequestDto
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public string? Tipo { get; set; }

    public TimeOnly? HoraEntrada { get; set; }

    public TimeOnly? HoraSalida { get; set; }

    public bool CruzaMedianoche { get; set; }

    public int? ToleranciaMinutos { get; set; }

    public List<int>? DiasLaborables { get; set; }

    public int? DiasTrabajo { get; set; }

    public int? DiasDescanso { get; set; }

    public DateOnly? FechaInicioCiclo { get; set; }
}
