namespace AuthDotnetCoreJwt.Models.Dto
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public AppUserDto? User { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
