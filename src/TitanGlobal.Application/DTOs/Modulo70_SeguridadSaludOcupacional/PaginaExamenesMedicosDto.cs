namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class PaginaExamenesMedicosDto
{
    public List<ExamenMedicoDto> Items { get; set; } = new List<ExamenMedicoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
