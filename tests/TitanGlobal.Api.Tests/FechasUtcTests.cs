using TitanGlobal.Api.Configuration;

namespace TitanGlobal.Api.Tests;

public class FechasUtcTests
{
    [Fact]
    public void NormalizarTreatsUnspecifiedDateAsUtcWithoutChangingInstant()
    {
        DateTime date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        DateTime normalized = FechasUtc.Normalizar(date);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(date.Ticks, normalized.Ticks);
    }

    [Fact]
    public void NormalizarConvertsLocalDateToUtc()
    {
        DateTime localDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        DateTime expectedUtc = localDate.ToUniversalTime();

        DateTime normalized = FechasUtc.Normalizar(localDate);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(expectedUtc, normalized);
    }

    [Fact]
    public void NormalizarKeepsUtcDateAndInstant()
    {
        DateTime utcDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTime normalized = FechasUtc.Normalizar(utcDate);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(utcDate, normalized);
    }
}
