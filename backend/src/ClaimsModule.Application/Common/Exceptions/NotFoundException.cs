namespace ClaimsModule.Application.Common.Exceptions;

/// <summary>Maps to HTTP 404.</summary>
public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.");
