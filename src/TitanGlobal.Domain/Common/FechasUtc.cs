namespace TitanGlobal.Domain.Common;

public static class FechasUtc
{
    public static DateTime Normalizar(DateTime fecha)
    {
        if (fecha.Kind == DateTimeKind.Utc)
        {
            return fecha;
        }

        if (fecha.Kind == DateTimeKind.Local)
        {
            return fecha.ToUniversalTime();
        }

        return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
    }
}
