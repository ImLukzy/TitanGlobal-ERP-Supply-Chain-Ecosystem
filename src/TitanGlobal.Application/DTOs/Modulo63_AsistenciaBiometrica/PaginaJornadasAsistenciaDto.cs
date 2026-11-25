namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class PaginaJornadasAsistenciaDto
{
    public List<JornadaAsistenciaDto> Items { get; set; } = new List<JornadaAsistenciaDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
