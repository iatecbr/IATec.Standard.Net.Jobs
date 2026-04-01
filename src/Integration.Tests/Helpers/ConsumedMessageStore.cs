using Application.Features.Assets.Events;

namespace Integration.Tests.Helpers;

public sealed class ConsumedMessageStore
{
    private readonly List<ProcessAssetEvent> _messages = [];
    private readonly object _lock = new();

    public void Add(ProcessAssetEvent message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    public async Task<ProcessAssetEvent?> WaitForMessageAsync(Guid assetId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            lock (_lock)
            {
                var found = _messages.FirstOrDefault(m => m.AssetId == assetId);
                if (found is not null) return found;
            }

            await Task.Delay(250);
        }

        return null;
    }
}
