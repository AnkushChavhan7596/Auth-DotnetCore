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
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(
            UserManager<AppUser> userManager,
            IConfiguration config,
            ILogger<AuthRepository> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
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
                UserName = req.Email
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

            var token = await GenerateJwtToken(user);

            return await GenerateAuthResponse(user, token);
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

            var token = await GenerateJwtToken(user);

            return await GenerateAuthResponse(user, token);
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
    }
}
