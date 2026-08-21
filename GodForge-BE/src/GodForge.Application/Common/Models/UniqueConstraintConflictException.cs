namespace GodForge.Application.Common.Models;

public sealed class UniqueConstraintConflictException : Exception
{
    public UniqueConstraintKind Constraint { get; }

    public UniqueConstraintConflictException(
        string message,
        UniqueConstraintKind constraint,
        Exception? innerException = null)
        : base(message, innerException)
        => Constraint = constraint;
}
