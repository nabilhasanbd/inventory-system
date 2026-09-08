namespace backend_api.Common;

public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(message, 400)
    {
    }
}
