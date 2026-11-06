namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class SerieListadoDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int? LoteId { get; set; }

    public string NumeroSerie { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
}
