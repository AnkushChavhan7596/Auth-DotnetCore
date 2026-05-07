using AuthDotnetCoreJwt.Models.Domain;
using AuthDotnetCoreJwt.Models.Dto.Auth;
using AuthDotnetCoreJwt.Models.Dto.Common;
using AuthDotnetCoreJwt.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthDotnetCoreJwt.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            UserManager<AppUser> userManager,
            IConfiguration config,
            ILogger<AuthRepository> logger,
            IEmailService emailService)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
            _emailService = emailService;
        }


        // =========================
        // REGISTER
        // =========================
        public async Task<ApiResponseDto<object>> RegisterAsync(RegisterRequestDto req)
        {
            var existingUser = await _userManager.FindByEmailAsync(req.Email);

            if (existingUser != null)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = new List<string> { "User already exists" }
                };
            }

            var user = new AppUser { 
                FullName = req.FullName,
                Email = req.Email,
                UserName = req.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, req.Password);

            if (!result.Succeeded)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // assign default role
            await _userManager.AddToRoleAsync(user, "User");

            // Generate email verification url
            var verifyUrl = await GetEmailVerificationUrl(user);

            await _emailService.SendEmailAsync(
                user.Email,
                "Verify your email",
                $"Click here to verify your email: {verifyUrl}"
            );

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "Registration successful. Please verify your email."
            };
        }

        // =========================
        // LOGIN
        // =========================
        public async Task<ApiResponseDto<object>> LoginAsync(LoginRequestDto req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for {Email}", req.Email);

                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Login failed",
                    Errors = new List<string> { "Invalid credentials" }
                };
            }

            var isValid = await _userManager.CheckPasswordAsync(user, req.Password);

            if (!isValid)
            {
                _logger.LogWarning("Invalid password attempt for {Email}", req.Email);

                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Login failed",
                    Errors = new List<string> { "Invalid credentials" }
                };
            }

            if (!user.EmailConfirmed)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Login failed",
                    Errors = new List<string> { "Email not verified" }
                };
            }

            var token = await GenerateJwtToken(user);

            var roles = await _userManager.GetRolesAsync(user);
            var expiryDays = int.Parse(_config["Jwt:ExpiryInDays"] ?? "7");
            var expiry = DateTime.UtcNow.AddDays(expiryDays);

            var userDto = new AppUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles.ToList()
            };

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "Login Successful",
                Data = new {
                    Token = token,
                    ExpiresAt = expiry,
                    User = userDto
                }
            };
        }

        // =========================
        // Email Confirmation
        // =========================
        public async Task<ApiResponseDto<object>> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Email verification failed",
                    Errors = new List<string> { "Invalid request" }
                };
            }

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Email verification failed",
                    Errors = new List<string> { "Invalid or token expired" }
                };
            }

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "Email verified successfully"
            };
        }

        // =========================
        // Resend Confirmation
        // =========================
        public async Task<ApiResponseDto<object>> ResendVerificationAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Email verification failed",
                    Errors = new List<string> { "Email is required" }
                };
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Verification email processed",
                    Errors = new List<string> { "If email exists, verification link has been sent." }
                };
            }

            if (user.EmailConfirmed)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Email already verified",
                    Errors = new List<string> { "Email is already verified." }
                };
            }

            // Generate email verification url
            var verifyUrl = await GetEmailVerificationUrl(user);

            await _emailService.SendEmailAsync(
                user.Email,
                "Verify your email",
                $"Click here to verify your email: {verifyUrl}"
            );

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "Verification email sent successfully",
            };
        }

        // =========================
        // CHANGE PASSWORD
        // =========================
        public async Task<ApiResponseDto<object>> ChangePasswordAsync(string userId, ChangePasswordDto model)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "Invalid user" }
                };
            };

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Password change failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "Password changed successfully"
            };
        }

        // =========================
        // FORGOT PASSWORD
        // =========================
        public async Task<ApiResponseDto<object>> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "If email exists, reset link sent"
                };
            }

            var resetUrl = await GetResetPasswordUrl(user);

            await _emailService.SendEmailAsync(
                email,
                "Reset Password",
                $"Click here to reset password: {resetUrl}"
            );

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "If email exists, reset link sent"
            };
        }

        // =========================
        // RESET PASSWORD
        // =========================
        public async Task<ApiResponseDto<object>> ResetPasswordAsync(ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = new List<string> { "User not found" }
                };
            }

            var decodedToken = Uri.UnescapeDataString(model.Token);

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                model.NewPassword
            );

            if (!result.Succeeded)
            {
                return new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Reset failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            return new ApiResponseDto<object>
            {
                Success = true,
                Message = "Password reset successfully"
            };
        }

        // =========================
        // JWT GENERATION
        // =========================
        private async Task<string> GenerateJwtToken(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(int.Parse(_config["Jwt:ExpiryInDays"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // =========================
        // Get Email Verification URL
        // =========================
        private async Task<string> GetEmailVerificationUrl(AppUser? user) {
            var emailConfirmationtoken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(emailConfirmationtoken);

            var verifyUrl = $"{_config["App:BaseUrl"]}/api/auth/confirm-email?email={user.Email}&token={encodedToken}";
            return verifyUrl;
        }

        // =========================
        // Get Reset Password URL
        // =========================
        private async Task<string> GetResetPasswordUrl(AppUser? user)
        {
             var resetPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = Uri.EscapeDataString(resetPasswordToken);

            var resetUrl =
                $"{_config["App:BaseUrl"]}/reset-password?email={user.Email}&token={encodedToken}";
            return resetUrl;
        }
    }
}
