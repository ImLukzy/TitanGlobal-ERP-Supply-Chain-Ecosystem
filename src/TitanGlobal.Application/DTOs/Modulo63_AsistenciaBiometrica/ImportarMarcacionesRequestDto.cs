namespace TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;

public class ImportarMarcacionesRequestDto
{
    public string? Dispositivo { get; set; }

    public List<RegistroRelojDto>? Registros { get; set; }
}
