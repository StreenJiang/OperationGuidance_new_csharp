using System.Threading;
using System.Threading.Tasks;

namespace OperationGuidance_new.Utils {
    public interface IMessageQueue<T> {
        ValueTask EnqueueAsync(T message, CancellationToken ct = default);
        bool TryComplete();
        Task WaitForDrainAsync(CancellationToken ct);
    }
}
