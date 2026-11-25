namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class PaginaAfiliacionesPensionDto
{
    public List<AfiliacionPensionDto> Items { get; set; } = new List<AfiliacionPensionDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
