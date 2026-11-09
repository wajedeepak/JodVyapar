namespace JodVyapar.Api.Common.Results;

/// <summary>Outcome of a feature-service call: a value, "not found", or field errors (camelCase field name to message).</summary>
public sealed class ServiceResult<T>
{
    private ServiceResult(T? value, bool isNotFound, IReadOnlyDictionary<string, string>? errors)
    {
        Value = value;
        IsNotFound = isNotFound;
        Errors = errors;
    }

    public T? Value { get; }
    public bool IsNotFound { get; }
    public IReadOnlyDictionary<string, string>? Errors { get; }

    public static ServiceResult<T> Ok(T value) => new(value, false, null);
    public static ServiceResult<T> NotFound() => new(default, true, null);
    public static ServiceResult<T> Invalid(string field, string message) => new(default, false, new Dictionary<string, string> { [field] = message });
    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string> errors) => new(default, false, errors);
}

/// <summary>Result value of an action that returns no body.</summary>
public readonly record struct NoValue;
