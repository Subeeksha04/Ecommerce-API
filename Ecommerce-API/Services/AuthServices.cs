using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce_API.Services
{
    public class AuthServices
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<Models.User> _userManager;
        private readonly SignInManager<Models.User> _signInManager;

        public AuthServices(IConfiguration configuration, UserManager<Models.User> userManager, SignInManager<Models.User> signInManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Register a new user using ASP.NET Core Identity and return a JWT for the created user
        public async Task<string> RegisterAsync(string userName, string email, string password, Models.Enums.UserRole role = Models.Enums.UserRole.Guest)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("Username is required.", nameof(userName));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password is required.", nameof(password));

            var existingUser = await _userManager.FindByNameAsync(userName);
            if (existingUser is not null) throw new InvalidOperationException("A user with the specified username already exists.");

            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail is not null) throw new InvalidOperationException("A user with the specified email already exists.");

            var user = new Models.User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                RoleType = role,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                PhoneNumber = null,
                Address = null
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var err = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {err}");
            }

            // Optionally update role field (application-specific)
            user = await _userManager.FindByIdAsync(user.Id.ToString()) ?? user;
            user.RoleType = role;
            await _userManager.UpdateAsync(user);

            return GenerateJwtToken(user);
        }

        // Login an existing user by username and password. Returns JWT if successful, otherwise null.
        public async Task<string?> LoginAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password)) return null;

            var user = await _userManager.FindByNameAsync(userName);
            if (user is null) return null;

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid) return null;

            return GenerateJwtToken(user);
        }

        // Generate a JWT for the given user. Requires configuration values under "Jwt" section
        // Jwt:Key (required) - signing key
        // Jwt:Issuer (optional)
        // Jwt:Audience (optional)
        // Jwt:ExpiryMinutes (optional, default 60)
        public string GenerateJwtToken(Models.User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            var keyString = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is not configured (Jwt:Key).");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutes = 60;
            if (int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var cfgMinutes)) expiryMinutes = cfgMinutes;

            // Ensure the signing key is at least 256 bits (32 bytes). If not, derive a 256-bit key by hashing the provided secret.
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            if (keyBytes.Length < 32)
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                keyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Role, user.RoleType.ToString()),
                new Claim("role", user.RoleType.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
