namespace WorkBase.Shared.Exceptions;

public abstract class WorkBaseException(string message) : Exception(message)
{
    public abstract string ErrorCode { get; }
}

public sealed class NotFoundException(string entityName, object id)
    : WorkBaseException($"{entityName} with id '{id}' was not found.")
{
    public override string ErrorCode => "NotFound";
    public string EntityName { get; } = entityName;
    public object EntityId { get; } = id;
}

public sealed class DomainException(string code, string message)
    : WorkBaseException(message)
{
    public override string ErrorCode => code;
}

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : WorkBaseException(message)
{
    public override string ErrorCode => "Forbidden";
}

public sealed class ConflictException(string message)
    : WorkBaseException(message)
{
    public override string ErrorCode => "Conflict";
}
