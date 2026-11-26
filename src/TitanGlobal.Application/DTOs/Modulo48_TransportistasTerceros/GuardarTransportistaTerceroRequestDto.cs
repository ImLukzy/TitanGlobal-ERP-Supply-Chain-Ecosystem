namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class GuardarTransportistaTerceroRequestDto
{
    public string RazonSocial { get; set; } = string.Empty;

    public string DocumentoFiscal { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string TipoServicio { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public string? Contacto { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string? Observaciones { get; set; }
}
