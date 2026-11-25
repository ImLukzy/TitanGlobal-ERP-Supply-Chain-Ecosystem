namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class GuardarAccionCorrectivaRequestDto
{
    public string? Descripcion { get; set; }

    public string? Responsable { get; set; }

    public DateOnly? FechaCompromiso { get; set; }
}
