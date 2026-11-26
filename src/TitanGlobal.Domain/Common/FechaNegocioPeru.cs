namespace TitanGlobal.Domain.Common;

public static class FechaNegocioPeru
{
    public static DateOnly DesdeUtc(DateTime instanteUtc)
    {
        return DateOnly.FromDateTime(FechasUtc.Normalizar(instanteUtc).AddHours(-5));
    }
}
