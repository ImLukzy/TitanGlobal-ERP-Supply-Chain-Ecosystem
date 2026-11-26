namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class ResumenTomasInventarioDto
{
    public int TotalTomas { get; set; }

    public int Abiertas { get; set; }

    public int Cerradas { get; set; }

    public int Anuladas { get; set; }

    public int LineasConDiferencia { get; set; }
}
