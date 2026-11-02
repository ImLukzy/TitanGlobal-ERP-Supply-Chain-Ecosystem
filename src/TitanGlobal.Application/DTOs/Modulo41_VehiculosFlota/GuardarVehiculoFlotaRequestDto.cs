namespace TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;

public class GuardarVehiculoFlotaRequestDto
{
    public string Placa { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int Anio { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal CapacidadKg { get; set; }

    public bool Activo { get; set; } = true;

    public string? Observaciones { get; set; }
}
