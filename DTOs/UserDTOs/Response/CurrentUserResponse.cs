namespace DTOs.UserDTOs.Response
{
    public class CurrentUserResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumbers { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }

    }
}
