namespace TitanGlobal.Application.Exceptions;

public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
