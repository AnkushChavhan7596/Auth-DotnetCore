namespace AuthDotnetCoreJwt.Models.Dto.Auth
{
    public class AppUserDto
    {
        public required string Id { get; set; } = string.Empty;

        public required string Email { get; set; } = string.Empty;

        public required string FullName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
    }
}
