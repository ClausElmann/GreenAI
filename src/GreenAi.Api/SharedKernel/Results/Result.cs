namespace GreenAi.Api.SharedKernel.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(Error error) { IsSuccess = false; Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);
    public static Result<T> Fail(string code, string message) => new(new Error(code, message));
}

public record Error(string Code, string Message);
