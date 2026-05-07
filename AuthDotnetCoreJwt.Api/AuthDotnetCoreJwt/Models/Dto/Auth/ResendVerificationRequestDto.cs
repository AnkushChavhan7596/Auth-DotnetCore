using System.ComponentModel.DataAnnotations;

namespace AuthDotnetCoreJwt.Models.Dto.Auth
{
    public class ResendVerificationRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
