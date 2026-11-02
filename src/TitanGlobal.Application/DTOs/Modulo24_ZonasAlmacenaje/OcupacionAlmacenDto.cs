namespace TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;

public class OcupacionAlmacenDto
{
    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public decimal CapacidadAlmacenM3 { get; set; }

    public decimal CapacidadAsignadaM3 { get; set; }

    public decimal PorcentajeAsignado { get; set; }
}
