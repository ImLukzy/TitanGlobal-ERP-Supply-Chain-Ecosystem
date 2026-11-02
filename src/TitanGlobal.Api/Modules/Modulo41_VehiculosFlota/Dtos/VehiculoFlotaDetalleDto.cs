namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Dtos;

public class VehiculoFlotaDetalleDto
{
    public int Id { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int Anio { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal CapacidadKg { get; set; }

    public bool Activo { get; set; }

    public string? Observaciones { get; set; }
}
