namespace DTOs.AiMenuDTOs.Response
{
    public class GetUserRecipesResponse
    {
        public List<AiRecipeResponse> Recipes { get; set; } = new List<AiRecipeResponse>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
