namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class LoteListadoDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public DateTime? FechaVencimiento { get; set; }

    public int Cantidad { get; set; }

    public string Estado { get; set; } = string.Empty;

    public bool Vencido { get; set; }
}
