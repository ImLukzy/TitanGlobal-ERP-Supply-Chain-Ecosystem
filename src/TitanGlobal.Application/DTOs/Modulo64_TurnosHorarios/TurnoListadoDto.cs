namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class TurnoListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public TimeOnly HoraEntrada { get; set; }

    public TimeOnly HoraSalida { get; set; }

    public bool CruzaMedianoche { get; set; }

    public int ToleranciaMinutos { get; set; }

    public List<int> DiasLaborables { get; set; } = new List<int>();

    public int? DiasTrabajo { get; set; }

    public int? DiasDescanso { get; set; }

    public DateOnly? FechaInicioCiclo { get; set; }

    public bool Activo { get; set; }
}
