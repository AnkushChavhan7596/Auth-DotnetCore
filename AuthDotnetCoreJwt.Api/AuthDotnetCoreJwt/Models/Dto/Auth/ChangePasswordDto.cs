namespace AuthDotnetCoreJwt.Models.Dto.Auth
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
