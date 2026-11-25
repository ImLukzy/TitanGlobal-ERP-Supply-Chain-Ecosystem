namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class RegistrarEntregaEppRequestDto
{
    public int? EquipoId { get; set; }

    public int? EmpleadoId { get; set; }

    public DateOnly? FechaEntrega { get; set; }

    public int? Cantidad { get; set; }
}
