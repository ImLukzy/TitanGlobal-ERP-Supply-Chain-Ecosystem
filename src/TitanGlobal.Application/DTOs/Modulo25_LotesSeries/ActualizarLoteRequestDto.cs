namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class ActualizarLoteRequestDto
{
    public DateTime? FechaFabricacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Cantidad { get; set; }

    public string Estado { get; set; } = "Disponible";
}
