namespace TitanGlobal.Api.Middlewares;

public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
