namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class EntregaEppDto
{
    public int Id { get; set; }

    public int EquipoId { get; set; }

    public string EquipoCodigo { get; set; } = string.Empty;

    public string EquipoNombre { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public DateOnly FechaEntrega { get; set; }

    public int Cantidad { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public bool Vencida { get; set; }
}
