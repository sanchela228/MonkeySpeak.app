using System;
using System.Threading.Tasks;

namespace App.System.Calls.Infrastructure;

public interface ITransport : IAsyncDisposable
{
    event Action<ReadOnlyMemory<byte>> OnData;
    Task SendAsync(ReadOnlyMemory<byte> data);
    Task CloseAsync();
}
