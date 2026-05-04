using AuthDotnetCoreJwt.Models.Domain;
using AuthDotnetCoreJwt.Models.Dto;

namespace AuthDotnetCoreJwt.Repositories
{
    public interface IAuthRepository
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req);

        Task<AuthResponseDto> LoginAsync(LoginRequestDto req);
    }
}
