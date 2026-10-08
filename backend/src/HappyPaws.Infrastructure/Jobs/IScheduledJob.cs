namespace HappyPaws.Infrastructure.Jobs;

/// <summary>
/// One piece of recurring work. Resolved in a fresh scope for every run, so it gets its own
/// <c>DbContext</c> and nothing it tracked leaks into the next run.
/// </summary>
public interface IScheduledJob
{
    Task RunAsync(CancellationToken cancellationToken);
}
