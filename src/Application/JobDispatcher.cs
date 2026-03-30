using Application.Contracts;

namespace Application;

public class JobDispatcher : IJobDispatcher
{
    public async Task DispatchAsync(string url)
    {
        Console.WriteLine(url);
        await Task.FromResult(0);
    }
}