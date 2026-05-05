namespace InterviewService.Application.Abstractions.Utilities;

public interface IUnitOfWork : IAsyncDisposable
{
    Task SaveChangesAsync();
}
