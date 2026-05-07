namespace InterviewService.Application.Abstractions.Utilities;

/// <summary>
/// Represents an explicit async commit boundary for staged persistence work.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Task SaveChangesAsync();
}
