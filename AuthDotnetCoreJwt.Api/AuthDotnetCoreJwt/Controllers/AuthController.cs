using AuthDotnetCoreJwt.Models.Dto.Auth;
using AuthDotnetCoreJwt.Models.Dto.Common;
using AuthDotnetCoreJwt.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthDotnetCoreJwt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        public AuthController(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        // POST : {apibaseurl}/api/auth/register
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
                });

            var response = await _authRepository.RegisterAsync(request);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // POST : {apibaseurl}/api/auth/login
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
                });

            var response = await _authRepository.LoginAsync(request);

            if (!response.Success)
                return Unauthorized(response);

            return Ok(response);
        }

        // GET : {apibaseurl}/api/auth/confirm-email
        [HttpGet]
        [Route("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            var result = await _authRepository.ConfirmEmailAsync(email, token);

            if (!result.Success)
                return BadRequest(result);

            return Ok(new
            {
                success = true,
                message = "Email verified successfully"
            });
        }

        // POST : {apibaseurl}/api/auth/resend-verification
        [HttpPost]
        [Route("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequestDto req)
        {
            var result = await _authRepository.ResendVerificationAsync(req.Email);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
