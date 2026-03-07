using UserService.Models;

namespace UserService.Services
{
    public interface IUserService
    {
        Task<UserProfileResponse?> GetUserByIdAsync(int id);
        Task<UserProfileResponse?> GetUserByEmailAsync(string email);
        Task<UserProfileResponse?> GetUserByUsernameAsync(string username);
        Task<UserProfileResponse> CreateUserAsync(RegisterRequest request, string passwordHash);
        Task<UserProfileResponse?> UpdateUserAsync(int id, UpdateUserProfileRequest request);
        Task<bool> DeactivateUserAsync(int id);
        Task<bool> ActivateUserAsync(int id);
        Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword);
        Task<UserProfileResponse?> UpdateLastLoginAsync(int id);

        // Address management
        Task<AddressDto> AddAddressAsync(int userId, CreateAddressRequest request);
        Task<AddressDto?> UpdateAddressAsync(int userId, int addressId, CreateAddressRequest request);
        Task<bool> DeleteAddressAsync(int userId, int addressId);
        Task<AddressDto?> SetDefaultAddressAsync(int userId, int addressId);
        Task<IEnumerable<AddressDto>> GetUserAddressesAsync(int userId);
    }
}