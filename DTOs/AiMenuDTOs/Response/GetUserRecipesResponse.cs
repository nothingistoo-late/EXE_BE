namespace DTOs.AiMenuDTOs.Response
{
    public class GetUserRecipesResponse
    {
        public List<AiRecipeResponse> Items { get; set; }

        public GetUserRecipesResponse(List<AiRecipeResponse> items)
        {
            Items = items;
        }
    }
}
