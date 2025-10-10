using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IImageSearchService
    {
        Task<string?> SearchImageUrlAsync(string dishName, string? description = null);
    }
}
