using System.ComponentModel.DataAnnotations;

namespace AuthDotnetCoreJwt.Models.Dto
{
    public class ResendVerificationRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
