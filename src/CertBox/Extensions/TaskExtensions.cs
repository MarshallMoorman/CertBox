namespace CertBox;

public static class TaskExtensions
{
    public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        if (completedTask == task)
        {
            cts.Cancel();
            await task; // Propagate exceptions
        }
        else
        {
            throw new TimeoutException("Operation timed out.");
        }
    }
}