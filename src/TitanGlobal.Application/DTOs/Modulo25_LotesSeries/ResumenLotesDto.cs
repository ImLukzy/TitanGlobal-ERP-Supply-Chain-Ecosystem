namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class ResumenLotesDto
{
    public int TotalLotes { get; set; }

    public int Disponibles { get; set; }

    public int EnCuarentena { get; set; }

    public int Bloqueados { get; set; }

    public int Vencidos { get; set; }

    public int PorVencer { get; set; }
}
