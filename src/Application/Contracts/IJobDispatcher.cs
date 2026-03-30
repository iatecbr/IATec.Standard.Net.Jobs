namespace Application.Contracts;

public interface IJobDispatcher
{
    Task DispatchAsync(string url);
}