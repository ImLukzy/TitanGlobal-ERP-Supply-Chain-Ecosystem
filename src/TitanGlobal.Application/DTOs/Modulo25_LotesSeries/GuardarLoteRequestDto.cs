namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class GuardarLoteRequestDto
{
    public int ProductoId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public DateTime? FechaFabricacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Cantidad { get; set; }

    public string Estado { get; set; } = "Disponible";
}
