using System.Threading;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
        Task<string> ChatAsync(string message, CancellationToken cancellationToken = default);
        Task<string> GenerateWishAsync(string Receiver, string occasion, string mainWish, string custom, CancellationToken cancellationToken = default);

    }

    // Keep old interface for backward compatibility
    public interface IHuggingFaceService : IAIService
    {
    }
}

