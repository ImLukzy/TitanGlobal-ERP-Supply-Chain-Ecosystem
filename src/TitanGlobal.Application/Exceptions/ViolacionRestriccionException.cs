namespace TitanGlobal.Application.Exceptions;

public class ViolacionRestriccionException : Exception
{
    public ViolacionRestriccionException(
        TipoViolacionRestriccion tipo,
        string? restriccion,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Tipo = tipo;
        Restriccion = restriccion;
    }

    public TipoViolacionRestriccion Tipo { get; }

    public string? Restriccion { get; }
}
