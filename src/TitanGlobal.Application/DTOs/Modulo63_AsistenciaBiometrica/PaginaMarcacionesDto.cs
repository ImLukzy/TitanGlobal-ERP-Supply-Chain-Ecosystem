namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class PaginaMarcacionesDto
{
    public List<MarcacionDto> Items { get; set; } = new List<MarcacionDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
