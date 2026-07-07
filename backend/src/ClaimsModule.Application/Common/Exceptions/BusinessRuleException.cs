namespace ClaimsModule.Application.Common.Exceptions;

/// <summary>Thrown by handlers when a domain/business rule blocks the request - maps to HTTP 422.</summary>
public class BusinessRuleException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public BusinessRuleException(string key, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]> { [key] = [message] };
    }

    public BusinessRuleException(IDictionary<string, string[]> errors)
        : base("One or more business rules were violated.")
    {
        Errors = errors;
    }
}
