using UserService.Data;
using UserService.Models;
using UserService.MessageBus;
using Microsoft.EntityFrameworkCore;

namespace UserService.Services
{
    public class UserService : IUserService
    {
        private readonly UserDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IMessageBus _messageBus;

        public UserService(
            UserDbContext context,
            ILogger<UserService> logger,
            IMessageBus messageBus)
        {
            _context = context;
            _logger = logger;
            _messageBus = messageBus;
        }

        public async Task<UserProfileResponse?> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

                return user != null ? MapToProfileResponse(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                throw;
            }
        }

        public async Task<UserProfileResponse?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

                return user != null ? MapToProfileResponse(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<UserProfileResponse?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                return user != null ? MapToProfileResponse(user) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        public async Task<UserProfileResponse> CreateUserAsync(RegisterRequest request, string passwordHash)
        {
            try
            {
                var user = new User
                {
                    Email = request.Email,
                    Username = request.Username,
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    AcceptsMarketing = request.AcceptsMarketing,
                    IsEmailVerified = false, // Would send verification email
                    IsActive = true,
                    Role = UserRole.Customer,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user: {Username} with ID: {UserId}", 
                    user.Username, user.Id);

                // Publish event
                await _messageBus.PublishAsync("user.created", new
                {
                    UserId = user.Id,
                    user.Email,
                    user.Username,
                    user.CreatedAt
                });

                return MapToProfileResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<UserProfileResponse?> UpdateUserAsync(int id, UpdateUserProfileRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return null;
                }

                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
                user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;
                user.AcceptsMarketing = request.AcceptsMarketing ?? user.AcceptsMarketing;
                user.ProfilePictureUrl = request.ProfilePictureUrl ?? user.ProfilePictureUrl;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated user profile: {UserId}", id);

                // Publish event
                await _messageBus.PublishAsync("user.updated", new
                {
                    UserId = id,
                    user.UpdatedAt
                });

                return await GetUserByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deactivated user: {UserId}", id);

                // Publish event
                await _messageBus.PublishAsync("user.deactivated", new { UserId = id });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Activated user: {UserId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", id);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return false;
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    return false;
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Changed password for user: {UserId}", id);

                // Publish event
                await _messageBus.PublishAsync("user.password.changed", new { UserId = id });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", id);
                throw;
            }
        }

        public async Task<UserProfileResponse?> UpdateLastLoginAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return null;
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await GetUserByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", id);
                throw;
            }
        }

        public async Task<AddressDto> AddAddressAsync(int userId, CreateAddressRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Addresses)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    throw new ArgumentException($"User with ID {userId} not found");
                }

                // If this is the first address or marked as default, update other addresses
                if (request.IsDefault || !user.Addresses.Any())
                {
                    foreach (var addr in user.Addresses.Where(a => a.IsDefault))
                    {
                        addr.IsDefault = false;
                    }
                }

                var address = new Address
                {
                    UserId = userId,
                    AddressLine1 = request.AddressLine1,
                    AddressLine2 = request.AddressLine2,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode,
                    Country = request.Country,
                    IsDefault = request.IsDefault || !user.Addresses.Any(),
                    Type = request.Type
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added address for user {UserId}", userId);

                return MapToAddressDto(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AddressDto?> UpdateAddressAsync(int userId, int addressId, CreateAddressRequest request)
        {
            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

                if (address == null)
                {
                    return null;
                }

                address.AddressLine1 = request.AddressLine1;
                address.AddressLine2 = request.AddressLine2;
                address.City = request.City;
                address.State = request.State;
                address.ZipCode = request.ZipCode;
                address.Country = request.Country;
                address.Type = request.Type;

                if (request.IsDefault && !address.IsDefault)
                {
                    // Clear default flag from other addresses
                    var otherAddresses = await _context.Addresses
                        .Where(a => a.UserId == userId && a.Id != addressId && a.IsDefault)
                        .ToListAsync();

                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefault = false;
                    }

                    address.IsDefault = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated address {AddressId} for user {UserId}", addressId, userId);

                return MapToAddressDto(address);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteAddressAsync(int userId, int addressId)
        {
            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

                if (address == null)
                {
                    return false;
                }

                _context.Addresses.Remove(address);

                // If this was the default address, set another as default
                if (address.IsDefault)
                {
                    var anotherAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.UserId == userId && a.Id != addressId);

                    if (anotherAddress != null)
                    {
                        anotherAddress.IsDefault = true;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted address {AddressId} for user {UserId}", addressId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address for user {UserId}", userId);
                throw;
            }
        }

        public async Task<AddressDto?> SetDefaultAddressAsync(int userId, int addressId)
        {
            try
            {
                // Clear current default
                var currentDefault = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                }

                // Set new default
                var newDefault = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

                if (newDefault == null)
                {
                    return null;
                }

                newDefault.IsDefault = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Set address {AddressId} as default for user {UserId}", 
                    addressId, userId);

                return MapToAddressDto(newDefault);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<AddressDto>> GetUserAddressesAsync(int userId)
        {
            try
            {
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();

                return addresses.Select(MapToAddressDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addresses for user {UserId}", userId);
                throw;
            }
        }

        private UserProfileResponse MapToProfileResponse(User user)
        {
            return new UserProfileResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Addresses = user.Addresses?.Select(MapToAddressDto).ToList() ?? new()
            };
        }

        private AddressDto MapToAddressDto(Address address)
        {
            return new AddressDto
            {
                Id = address.Id,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                ZipCode = address.ZipCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                Type = address.Type.ToString()
            };
        }
    }

    public class UpdateUserProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? AcceptsMarketing { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}