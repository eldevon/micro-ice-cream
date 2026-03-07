using UserService.Data;
using UserService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UserService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IUserService _userService;

        public AuthService(
            UserDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _userService = userService;
        }

        public async Task<AuthResponse?> AuthenticateAsync(LoginRequest request)
        {
            try
            {
                // Find user by username or email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        (u.Username == request.UsernameOrEmail || 
                         u.Email == request.UsernameOrEmail) && 
                        u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Login failed for {UsernameOrEmail}: user not found", 
                        request.UsernameOrEmail);
                    return null;
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed for {UsernameOrEmail}: invalid password", 
                        request.UsernameOrEmail);
                    return null;
                }

                // Update last login
                await _userService.UpdateLastLoginAsync(user.Id);

                // Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("User {Username} logged in successfully", user.Username);

                return new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user");
                throw;
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);

                if (existingUser != null)
                {
                    throw new InvalidOperationException("User with this email or username already exists");
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create user
                var user = await _userService.CreateUserAsync(request, passwordHash);

                // Generate email verification token (simplified)
                var verificationToken = GenerateEmailVerificationToken(user.Id);

                // In production, send email with verification link
                _logger.LogInformation("Email verification token for user {UserId}: {Token}", 
                    user.Id, verificationToken);

                // Generate JWT token for immediate login
                var dbUser = await _context.Users.FindAsync(user.Id);
                var token = GenerateJwtToken(dbUser!);

                return new AuthResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    Role = user.Role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                throw;
            }
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            try
            {
                // In production, validate token and verify email
                // This is a simplified version
                var userId = DecodeEmailVerificationToken(token);
                
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsEmailVerified = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Email verified for user {UserId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return false;
            }
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

                if (user == null)
                {
                    // Don't reveal that user doesn't exist
                    return true;
                }

                // Generate reset token
                var resetToken = GeneratePasswordResetToken(user.Id);

                // In production, send email with reset link
                _logger.LogInformation("Password reset token for user {UserId}: {Token}", 
                    user.Id, resetToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                // In production, validate token
                var userId = DecodePasswordResetToken(token);
                
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user {UserId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSecretKeyHere_MakeItLongAndSecure_AtLeast32Characters");
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                
                // Check if user still exists and is active
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    return user != null && user.IsActive;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSecretKeyHere_MakeItLongAndSecure_AtLeast32Characters");
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateEmailVerificationToken(int userId)
        {
            // In production, use a proper token generation method
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"verify:{userId}:{Guid.NewGuid()}"));
        }

        private int DecodeEmailVerificationToken(string token)
        {
            // In production, properly validate and decode token
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':');
            return int.Parse(parts[1]);
        }

        private string GeneratePasswordResetToken(int userId)
        {
            // In production, use a proper token generation method
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"reset:{userId}:{Guid.NewGuid()}"));
        }

        private int DecodePasswordResetToken(string token)
        {
            // In production, properly validate and decode token
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split(':');
            return int.Parse(parts[1]);
        }
    }
}