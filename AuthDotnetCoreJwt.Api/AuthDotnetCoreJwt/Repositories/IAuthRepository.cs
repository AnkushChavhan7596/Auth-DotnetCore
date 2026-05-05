using AuthDotnetCoreJwt.Models.Domain;
using AuthDotnetCoreJwt.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AuthDotnetCoreJwt.Repositories
{
    public interface IAuthRepository
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req);

        Task<AuthResponseDto> LoginAsync(LoginRequestDto req);

        Task<AuthResponseDto> ConfirmEmailAsync(string email, string token);

        Task<AuthResponseDto> ResendVerificationAsync(string email);
    }
}
