using BusinessObjects;
using BusinessObjects.Common;
using DTOs.AiMenuDTOs.Request;
using DTOs.AiMenuDTOs.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Interfaces;
using AutoMapper;

namespace Services.Implementations
{
    public class AiMenuService : BaseService<AiRecipe, Guid>, IAiMenuService
    {
        private readonly IAiRecipeRepository _aiRecipeRepository;
        private readonly IAIService _aiService;
        private readonly IImageSearchService _imageSearchService;
        private readonly IMapper _mapper;
        private readonly ILogger<AiMenuService> _logger;

        public AiMenuService(
            IAiRecipeRepository aiRecipeRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IAIService aiService,
            IImageSearchService imageSearchService,
            IMapper mapper,
            ILogger<AiMenuService> logger)
            : base(aiRecipeRepository, currentUserService, unitOfWork, currentTime)
        {
            _aiRecipeRepository = aiRecipeRepository;
            _aiService = aiService;
            _imageSearchService = imageSearchService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResult<GenerateRecipeResponse>> GenerateRecipesAsync(GenerateRecipeRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<GenerateRecipeResponse>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                // Validate vegetables
                if (!request.Vegetables.Any())
                {
                    return ApiResult<GenerateRecipeResponse>.Failure(new ArgumentException("At least one vegetable is required"));
                }

                // Generate recipe using AI
                var aiRecipe = await GenerateRecipeWithAIAsync(request, currentUserId.Value);
                if (aiRecipe == null)
                {
                    return ApiResult<GenerateRecipeResponse>.Failure(new Exception("Failed to generate recipe"));
                }

                // Tìm ảnh thật cho món ăn
                var imageUrl = await _imageSearchService.SearchImageUrlAsync(aiRecipe.DishName, aiRecipe.Description);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    aiRecipe.ImageUrl = imageUrl;
                }

                // Save recipe to database
                var savedRecipe = await CreateAsync(aiRecipe);
                var recipeResponse = _mapper.Map<AiRecipeResponse>(savedRecipe);

                var response = new GenerateRecipeResponse
                {
                    Recipe = recipeResponse
                };

                return ApiResult<GenerateRecipeResponse>.Success(response, "Recipes generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recipes for user {UserId}", _currentUserService.GetUserId());
                return ApiResult<GenerateRecipeResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<GetUserRecipesResponse>> GetUserRecipesAsync(GetUserRecipesRequest request)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<GetUserRecipesResponse>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                var recipes = await _aiRecipeRepository.GetUserRecipesAsync(
                    currentUserId.Value,
                    request.Count,
                    request.SearchTerm,
                    request.FromDate,
                    request.ToDate);

                var response = new GetUserRecipesResponse(recipes);

                return ApiResult<GetUserRecipesResponse>.Success(response, "User recipes retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipes for user {UserId}", _currentUserService.GetUserId());
                return ApiResult<GetUserRecipesResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<AiRecipeResponse>> GetRecipeByIdAsync(Guid recipeId)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<AiRecipeResponse>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                var recipe = await _aiRecipeRepository.GetByIdAsync(recipeId);
                if (recipe == null || recipe.UserId != currentUserId.Value || recipe.IsDeleted)
                {
                    return ApiResult<AiRecipeResponse>.Failure(new KeyNotFoundException("Recipe not found"));
                }

                return ApiResult<AiRecipeResponse>.Success(_mapper.Map<AiRecipeResponse>(recipe), "Recipe retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe {RecipeId} for user {UserId}", recipeId, _currentUserService.GetUserId());
                return ApiResult<AiRecipeResponse>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<AiRecipeResponse>>> GetRecentRecipesAsync(int count = 5)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<List<AiRecipeResponse>>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                var recipes = await _aiRecipeRepository.GetRecentRecipesAsync(currentUserId.Value, count);
                return ApiResult<List<AiRecipeResponse>>.Success(recipes, "Recent recipes retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent recipes for user {UserId}", _currentUserService.GetUserId());
                return ApiResult<List<AiRecipeResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<List<AiRecipeResponse>>> GetRecipesByVegetablesAsync(List<string> vegetables, int count = 5)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<List<AiRecipeResponse>>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                var recipes = await _aiRecipeRepository.GetRecipesByVegetablesAsync(currentUserId.Value, vegetables, count);
                return ApiResult<List<AiRecipeResponse>>.Success(recipes, "Recipes by vegetables retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipes by vegetables for user {UserId}", _currentUserService.GetUserId());
                return ApiResult<List<AiRecipeResponse>>.Failure(ex);
            }
        }

        public async Task<ApiResult<bool>> DeleteRecipeAsync(Guid recipeId)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<bool>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                var recipe = await _aiRecipeRepository.GetByIdAsync(recipeId);
                if (recipe == null || recipe.UserId != currentUserId.Value || recipe.IsDeleted)
                {
                    return ApiResult<bool>.Failure(new KeyNotFoundException("Recipe not found"));
                }

                var result = await DeleteAsync(recipeId);
                return ApiResult<bool>.Success(result, "Recipe deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId} for user {UserId}", recipeId, _currentUserService.GetUserId());
                return ApiResult<bool>.Failure(ex);
            }
        }

        public async Task<ApiResult<int>> GetUserRecipeCountAsync()
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (currentUserId == null)
                {
                    return ApiResult<int>.Failure(new UnauthorizedAccessException("User not authenticated"));
                }

                var count = await _aiRecipeRepository.GetUserRecipeCountAsync(currentUserId.Value);
                return ApiResult<int>.Success(count, "User recipe count retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe count for user {UserId}", _currentUserService.GetUserId());
                return ApiResult<int>.Failure(ex);
            }
        }

        private async Task<AiRecipe?> GenerateRecipeWithAIAsync(GenerateRecipeRequest request, Guid userId)
        {
            try
            {
                var prompt = BuildPrompt(request);
                var aiResponse = await _aiService.GenerateTextAsync(prompt);
                
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    var recipe = ParseAIResponse(aiResponse, request, userId);
                    return recipe;
                }
                else
                {
                    _logger.LogWarning("AI service returned empty response, generating mock recipe");
                    return GenerateMockRecipe(request, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI service");
                return GenerateMockRecipe(request, userId);
            }
        }

        private string BuildPrompt(GenerateRecipeRequest request)
        {
            var vegetables = string.Join(", ", request.Vegetables);
            var prompt = $@"Bạn là một đầu bếp chuyên nghiệp chuyên về ẩm thực chay và thuần chay.

Hãy tạo 1 công thức nấu ăn sáng tạo sử dụng các loại rau củ sau: {vegetables}.

Cho công thức này, hãy cung cấp:
1. Tên món ăn (sáng tạo và hấp dẫn)
2. Mô tả (2-3 câu về món ăn)
3. Nguyên liệu (danh sách chi tiết với số lượng)
4. Hướng dẫn từng bước (các bước được đánh số)
5. Thời gian nấu ước tính
6. Mẹo nấu ăn (tùy chọn)
7. URL hình ảnh (để trống, hệ thống sẽ tự động tìm ảnh phù hợp)

QUAN TRỌNG: Định dạng phản hồi dưới dạng JSON object với cấu trúc chính xác này:
{{
  ""dishName"": ""Tên món ăn"",
  ""description"": ""Mô tả món ăn"",
  ""ingredients"": [""nguyên liệu 1"", ""nguyên liệu 2""],
  ""instructions"": [""bước 1"", ""bước 2""],
  ""estimatedCookingTime"": ""30 phút"",
  ""cookingTips"": ""Mẹo nấu ăn"",
         ""imageUrl"": """"
}}

Sở thích ăn uống: {request.DietaryPreferences ?? "Không có yêu cầu đặc biệt"}
Trình độ nấu ăn: {request.CookingSkillLevel ?? "Mọi trình độ"}

LƯU Ý: 
- Để trống imageUrl, hệ thống sẽ tự động tìm ảnh phù hợp
- Chỉ cần tập trung vào tạo công thức nấu ăn chất lượng

Vui lòng chỉ phản hồi với JSON object, không có thêm văn bản hoặc giải thích nào khác.";

            return prompt;
        }

        private AiRecipe? ParseAIResponse(string aiContent, GenerateRecipeRequest request, Guid userId)
        {
            try
            {
                // Clean the response - remove any markdown formatting or extra text
                var cleanContent = aiContent.Trim();
                
                // Try to extract JSON object from the response
                var jsonStart = cleanContent.IndexOf('{');
                var jsonEnd = cleanContent.LastIndexOf('}') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = cleanContent.Substring(jsonStart, jsonEnd - jsonStart);
                    var aiRecipe = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    
                    if (aiRecipe != null)
                    {
                        var recipe = new AiRecipe
                        {
                            DishName = aiRecipe.dishName?.ToString() ?? "Món ăn không xác định",
                            Description = aiRecipe.description?.ToString() ?? "Không có mô tả",
                            Ingredients = JsonConvert.SerializeObject(aiRecipe.ingredients?.ToObject<List<string>>() ?? new List<string>()),
                            Instructions = JsonConvert.SerializeObject(aiRecipe.instructions?.ToObject<List<string>>() ?? new List<string>()),
                            EstimatedCookingTime = aiRecipe.estimatedCookingTime?.ToString() ?? "Không xác định",
                            CookingTips = aiRecipe.cookingTips?.ToString(),
                            ImageUrl = aiRecipe.imageUrl?.ToString(),
                            InputVegetables = JsonConvert.SerializeObject(request.Vegetables),
                            UserId = userId,
                            AiModel = "Groq",
                            GeneratedAt = _currentTime.GetVietnamTime()
                        };
                        
                        return recipe;
                    }
                }
                
                // If no recipe was parsed, generate mock recipe
                _logger.LogWarning("No recipe could be parsed from AI response, generating mock recipe");
                return GenerateMockRecipe(request, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response: {Content}", aiContent);
                return GenerateMockRecipe(request, userId);
            }
        }

        private AiRecipe GenerateMockRecipe(GenerateRecipeRequest request, Guid userId)
        {
            var vegetables = request.Vegetables;
            
            var recipe = new AiRecipe
            {
                DishName = $"Món {string.Join(" & ", vegetables.Take(2))} thơm ngon",
                Description = $"Một món ăn đầy hương vị và bổ dưỡng với {string.Join(", ", vegetables)}. Hoàn hảo cho một bữa ăn lành mạnh.",
                Ingredients = JsonConvert.SerializeObject(new List<string>
                {
                    $"{vegetables.FirstOrDefault()} - 2 chén",
                    "Dầu ô liu - 2 muỗng canh",
                    "Tỏi - 3 tép",
                    "Muối và tiêu vừa ăn",
                    "Rau thơm tươi - 1 muỗng canh"
                }),
                Instructions = JsonConvert.SerializeObject(new List<string>
                {
                    "Đun nóng dầu ô liu trong chảo lớn ở lửa vừa",
                    $"Thêm {vegetables.FirstOrDefault()} và nấu trong 5 phút",
                    "Thêm tỏi và nấu thêm 1 phút",
                    "Nêm muối, tiêu và rau thơm",
                    "Dọn nóng và thưởng thức!"
                }),
                EstimatedCookingTime = "20-25 phút",
                CookingTips = "Để có kết quả tốt nhất, hãy sử dụng rau củ tươi và không nấu quá chín.",
                ImageUrl = "https://images.unsplash.com/photo-1540420773420-3366772f4999?w=400&h=300&fit=crop",
                InputVegetables = JsonConvert.SerializeObject(vegetables),
                UserId = userId,
                AiModel = "Groq-Fallback",
                GeneratedAt = _currentTime.GetVietnamTime()
            };
            
            return recipe;
        }

    }
}
