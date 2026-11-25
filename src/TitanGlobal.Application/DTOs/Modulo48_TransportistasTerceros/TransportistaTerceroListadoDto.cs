namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class TransportistaTerceroListadoDto
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public string DocumentoFiscal { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public string TipoServicio { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
