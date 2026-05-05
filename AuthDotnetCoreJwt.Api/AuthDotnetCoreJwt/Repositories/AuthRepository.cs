using AuthDotnetCoreJwt.Models.Domain;
using AuthDotnetCoreJwt.Models.Dto;
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
        private readonly IEmailRepository _emailRepository;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            UserManager<AppUser> userManager,
            IConfiguration config,
            ILogger<AuthRepository> logger,
            IEmailRepository emailRepository)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
            _emailRepository = emailRepository;
        }


        // =========================
        // REGISTER
        // =========================
        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto req)
        {
            var existingUser = await _userManager.FindByEmailAsync(req.Email);

            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
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
                return new AuthResponseDto
                {
                    Success = false,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            // assign default role
            await _userManager.AddToRoleAsync(user, "User");

            //var token = await GenerateJwtToken(user);

            // Generate email verification url
            var verifyUrl = await GetEmailVerificationUrl(user);

            await _emailRepository.SendEmailAsync(
                user.Email,
                "Verify your email",
                $"Click here to verify your email: {verifyUrl}"
            );

            return new AuthResponseDto
            {
                Success = true,
                Errors = new List<string> { "Registration successful. Please verify your email." }
            };
        }

        // =========================
        // LOGIN
        // =========================
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for {Email}", req.Email);

                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid credentials" }
                };
            }

            var isValid = await _userManager.CheckPasswordAsync(user, req.Password);

            if (!isValid)
            {
                _logger.LogWarning("Invalid password attempt for {Email}", req.Email);

                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid credentials" }
                };
            }

            if (!user.EmailConfirmed)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Email not verified" }
                };
            }

            var token = await GenerateJwtToken(user);

            return await GenerateAuthResponse(user, token);
        }

        // =========================
        // Email Confirmation
        // =========================
        public async Task<AuthResponseDto> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid request" }
                };
            }

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid or expired token" }
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Errors = new List<string> { "Email verified successfully" }
            };
        }

        // =========================
        // Resend Confirmation
        // =========================
        public async Task<AuthResponseDto> ResendVerificationAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Email is required" }
                };
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // security best practice (do not reveal user existence)
                return new AuthResponseDto
                {
                    Success = true,
                    Errors = new List<string> { "If email exists, verification link has been sent." }
                };
            }

            if (user.EmailConfirmed)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Errors = new List<string> { "Email is already verified." }
                };
            }

            // Generate email verification url
            var verifyUrl = await GetEmailVerificationUrl(user);

            await _emailRepository.SendEmailAsync(
                user.Email,
                "Verify your email",
                $"Click here to verify your email: {verifyUrl}"
            );

            return new AuthResponseDto
            {
                Success = true,
                Errors = new List<string> { "Verification email sent." }
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
        // COMMON RESPONSE BUILDER
        // =========================
        private async Task<AuthResponseDto> GenerateAuthResponse(AppUser user, string token)
        {
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

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                ExpiresAt = expiry,
                User = userDto
            };
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
    }
}
