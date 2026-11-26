namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class DiaProgramadoDto
{
    public DateOnly Fecha { get; set; }

    public int? TurnoId { get; set; }

    public string? TurnoCodigo { get; set; }

    public bool Laborable { get; set; }

    public TimeOnly? HoraEntrada { get; set; }

    public TimeOnly? HoraSalida { get; set; }

    public bool CruzaMedianoche { get; set; }

    public int? ToleranciaMinutos { get; set; }

    public string? Observacion { get; set; }
}
