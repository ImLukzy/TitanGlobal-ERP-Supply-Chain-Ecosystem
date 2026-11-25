namespace TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;

public class SaldoVacacionalDto
{
    public int EmpleadoId { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public DateOnly Fecha { get; set; }

    public int MesesServicio { get; set; }

    public decimal DiasDevengados { get; set; }

    public decimal DiasGozados { get; set; }

    public decimal DiasProgramados { get; set; }

    public decimal Saldo { get; set; }
}
