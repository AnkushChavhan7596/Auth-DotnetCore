using Microsoft.AspNetCore.Identity;

namespace AuthDotnetCoreJwt.Models.Domain
{
    public class AppUser : IdentityUser
    {
        public required string FullName { get; set; } = String.Empty;
    }
}
