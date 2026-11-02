namespace TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;

public class ZonaListadoDto
{
    public int Id { get; set; }

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Temperatura { get; set; } = string.Empty;

    public decimal CapacidadM3 { get; set; }

    public bool Activa { get; set; }
}
