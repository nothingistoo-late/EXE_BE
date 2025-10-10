using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class ImageSearchService : IImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageSearchService> _logger;
        private readonly List<string> _fallbackImageUrls;

        public ImageSearchService(HttpClient httpClient, ILogger<ImageSearchService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Danh sách ảnh fallback từ Unsplash (ảnh thật, có thể truy cập được)
            _fallbackImageUrls = new List<string>
            {
                // Fresh vegetables & salads
                "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1551782450-a2132b4ba21d?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1563379091339-03246963d51a?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1574484284002-952d92456975?w=400&h=300&fit=crop",
                
                // Healthy meals & dishes
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ca4b?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1546833999-b9f581a1996d?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1571091718767-18b5b1457add?w=400&h=300&fit=crop",
                
                // Vietnamese & Asian dishes
                "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1563379091339-03246963d51a?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1574484284002-952d92456975?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1585032226651-759b9b3a0a4a?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1598511752355-5b0c0c4a0a4a?w=400&h=300&fit=crop",
                "https://images.unsplash.com/photo-1606491956689-2ea866880c84?w=400&h=300&fit=crop"
            };
        }

        public async Task<string?> SearchImageUrlAsync(string dishName, string? description = null)
        {
            try
            {
                // Thử tìm ảnh dựa trên tên món ăn
                var imageUrl = await TrySearchUnsplashAsync(dishName);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    return imageUrl;
                }

                // Nếu không tìm được, thử với description
                if (!string.IsNullOrEmpty(description))
                {
                    imageUrl = await TrySearchUnsplashAsync(description);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        return imageUrl;
                    }
                }

                // Fallback: chọn ảnh phù hợp với loại món ăn
                return GetImageByDishType(dishName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for image for dish: {DishName}", dishName);
                return GetRandomFallbackImage();
            }
        }

        private async Task<string?> TrySearchUnsplashAsync(string searchTerm)
        {
            try
            {
                // Tạo search term phù hợp cho Unsplash
                var cleanSearchTerm = CleanSearchTerm(searchTerm);
                var searchUrl = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(cleanSearchTerm)}&per_page=1&client_id=YOUR_ACCESS_KEY";
                
                // Note: Cần Unsplash Access Key để sử dụng API
                // Tạm thời return null để dùng fallback
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search Unsplash for: {SearchTerm}", searchTerm);
                return null;
            }
        }

        private string CleanSearchTerm(string searchTerm)
        {
            // Làm sạch search term
            var clean = searchTerm.ToLower()
                .Replace("gỏi", "salad")
                .Replace("xào", "stir fry")
                .Replace("nướng", "grilled")
                .Replace("luộc", "boiled")
                .Replace("hấp", "steamed")
                .Replace("cà rốt", "carrot")
                .Replace("bí ngòi", "zucchini")
                .Replace("cà chua", "tomato")
                .Replace("cải thìa", "bok choy")
                .Replace("rau cải", "vegetables");

            return clean;
        }

        private string GetRandomFallbackImage()
        {
            var random = new Random();
            var index = random.Next(_fallbackImageUrls.Count);
            return _fallbackImageUrls[index];
        }

        private string GetImageByDishType(string dishName)
        {
            var lowerDishName = dishName.ToLower();
            
            // Phân loại món ăn và chọn ảnh phù hợp
            if (lowerDishName.Contains("gỏi") || lowerDishName.Contains("salad"))
            {
                // Ảnh salad (index 0-5)
                var random = new Random();
                return _fallbackImageUrls[random.Next(0, 6)];
            }
            else if (lowerDishName.Contains("xào") || lowerDishName.Contains("stir") || lowerDishName.Contains("nấu"))
            {
                // Ảnh món nấu (index 6-11)
                var random = new Random();
                return _fallbackImageUrls[random.Next(6, 12)];
            }
            else if (lowerDishName.Contains("chay") || lowerDishName.Contains("vegetarian") || lowerDishName.Contains("vegan"))
            {
                // Ảnh món chay (index 12-17)
                var random = new Random();
                return _fallbackImageUrls[random.Next(12, 18)];
            }
            else
            {
                // Mặc định: chọn ngẫu nhiên
                return GetRandomFallbackImage();
            }
        }
    }
}
