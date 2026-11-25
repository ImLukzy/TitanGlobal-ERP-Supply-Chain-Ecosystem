namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class ResultadoImportacionDto
{
    public int Recibidos { get; set; }

    public int Importados { get; set; }

    public int YaImportados { get; set; }

    public int Duplicados { get; set; }

    public List<RegistroRechazadoDto> Rechazados { get; set; } = new List<RegistroRechazadoDto>();
}
