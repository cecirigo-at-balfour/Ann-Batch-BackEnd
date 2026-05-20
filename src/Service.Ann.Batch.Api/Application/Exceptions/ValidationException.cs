using FluentValidation.Results;

namespace Service.Ann.Batch.Api.Application.Exceptions;

public class ValidationException : Exception
{
    public ValidationException() : base("One or more validations occurred.")
    {
        Errors = [];
    }

    public List<string> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        foreach (var failure in failures)
        {
            Errors.Add(failure.ErrorMessage);
        }
    }

    public ValidationException(string message) : base(message)
    {
        Errors = [];
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = [];
    }
}