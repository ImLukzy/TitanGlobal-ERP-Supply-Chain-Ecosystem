namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class FleteTerceroDetalleDto : FleteTerceroListadoDto
{
    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaCancelacion { get; set; }
}
