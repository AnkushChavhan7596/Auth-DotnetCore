using AuthDotnetCoreJwt.Models.Domain;
using AuthDotnetCoreJwt.Models.Dto.Auth;
using AuthDotnetCoreJwt.Models.Dto.Common;
using Microsoft.AspNetCore.Mvc;

namespace AuthDotnetCoreJwt.Repositories
{
    public interface IAuthRepository
    {
        Task<ApiResponseDto<object>> RegisterAsync(RegisterRequestDto req);

        Task<ApiResponseDto<object>> LoginAsync(LoginRequestDto req);

        Task<ApiResponseDto<object>> ConfirmEmailAsync(string email, string token);

        Task<ApiResponseDto<object>> ResendVerificationAsync(string email);

        Task<ApiResponseDto<object>> ChangePasswordAsync(string userId, ChangePasswordDto model);

        Task<ApiResponseDto<object>> ForgotPasswordAsync(string email);

        Task<ApiResponseDto<object>> ResetPasswordAsync(ResetPasswordDto model);
    }
}
