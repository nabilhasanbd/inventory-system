namespace backend_api.Common;

public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
