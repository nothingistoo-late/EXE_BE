using System.Threading;
using System.Threading.Tasks;

namespace ChatBoxAI.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default);
    }
}


