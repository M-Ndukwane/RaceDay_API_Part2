
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthController(
            RaceDayDbContext context,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }


[HttpPost("register")]
public async Task<IActionResult> Register(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var requestedRole = request.Role.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            // Only allow the two supported roles.
            if (!string.Equals(requestedRole, "Participant",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(requestedRole, "Organiser",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Role must be Participant or Organiser."
                });
            }

            // Find the requested role in the existing database.
            var roleName = string.Equals(
                requestedRole,
                "Organiser",
                StringComparison.OrdinalIgnoreCase)
                    ? "Organiser"
                    : "Participant";

            var role = await _context.Roles
                .SingleOrDefaultAsync(r => r.RoleName == roleName);

            if (role == null)
            {
                return Problem(
                    $"The {roleName} role is missing from the database.");
            }

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber?.Trim(),
                RolesID = role.RoleID,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Registration successful.",
                user.UsersID,
                user.FirstName,
                user.LastName,
                user.Email,
                Role = role.RoleName
            });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .SingleOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            PasswordVerificationResult passwordResult;

            try
            {
                passwordResult = _passwordHasher.VerifyHashedPassword(
                    user, user.PasswordHash, request.Password);
            }
            catch (FormatException)
            {
                passwordResult = PasswordVerificationResult.Failed;
            }

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var jwtKey = _configuration["Jwt:Key"]!;
            var jwtIssuer = _configuration["Jwt:Issuer"]!;
            var jwtAudience = _configuration["Jwt:Audience"]!;

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier, user.UsersID.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(60);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var tokenString =
                new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Login successful.",
                accessToken = tokenString,
                tokenType = "Bearer",
                expiresAt,
                user = new
                {
                    user.UsersID,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    Role = user.Role.RoleName
                }
            });
        }
        // Change password for the logged-in user
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto request)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UsersID == userId);

            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.CurrentPassword);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return BadRequest(new
                {
                    message = "Your current password is incorrect."
                });
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                return BadRequest(new
                {
                    message = "Your new password must be different from your current password."
                });
            }

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Password changed successfully."
            });
        }
    }
}
