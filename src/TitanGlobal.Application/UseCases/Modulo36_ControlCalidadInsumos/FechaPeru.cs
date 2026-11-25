namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

// «Hoy» del módulo en hora de Perú (UTC-5, sin horario de verano), la misma regla que CalculadoraJornada (módulo 63).
// Con UTC, a partir de las 19:00 en Perú ya sería el día siguiente y se aceptaría una inspección con fecha de mañana.
public static class FechaPeru
{
    public const int DesfaseHoras = -5;

    public static DateOnly DiaDe(DateTime instanteUtc)
    {
        return DateOnly.FromDateTime(instanteUtc.AddHours(DesfaseHoras));
    }

    public static DateOnly Hoy()
    {
        return DiaDe(DateTime.UtcNow);
    }
}
