namespace backend_api.Common;

public abstract class AppException : Exception
{
    public int StatusCode { get; }

    protected AppException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
