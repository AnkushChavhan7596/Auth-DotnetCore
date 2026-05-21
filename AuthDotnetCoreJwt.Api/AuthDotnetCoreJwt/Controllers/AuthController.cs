using AuthDotnetCoreJwt.Models.Dto.Auth;
using AuthDotnetCoreJwt.Models.Dto.Common;
using AuthDotnetCoreJwt.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        // resend verification and register email verification email to this call
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

        // POST : {apibaseurl}/api/auth/resend-verification (email-verfication)
        [HttpPost]
        [Route("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequestDto req)
        {
            var result = await _authRepository.ResendVerificationAsync(req.Email);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: {apibaseurl}/api/auth/change-password
        [HttpPost]
        [Route("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await _authRepository.ChangePasswordAsync(userId, model);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        // POST: {apibaseurl}/api/auth/forgot-password
        [HttpPost]
        [Route("forgot-password")]
        [Authorize]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            var result = await _authRepository.ForgotPasswordAsync(model.Email);

            return Ok(result);
        }

        // forgot passward reset link to this call
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            var result = await _authRepository.ResetPasswordAsync(model);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
