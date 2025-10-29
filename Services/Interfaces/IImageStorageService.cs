using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IImageStorageService
    {
        Task<string> UploadAsync(IFormFile file, string? fileName = null, CancellationToken cancellationToken = default);
    }
}

