namespace backend_api.Common;

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
