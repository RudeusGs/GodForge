namespace GodForge.Application.Common.Models;

public sealed class UniqueConstraintConflictException : Exception
{
    public string? ConstraintName { get; }

    public UniqueConstraintConflictException(string message, string? constraintName, Exception? innerException = null)
        : base(message, innerException)
        => ConstraintName = constraintName;
}
