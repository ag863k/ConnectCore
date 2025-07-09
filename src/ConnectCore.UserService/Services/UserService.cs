using ConnectCore.Shared.Models;
using ConnectCore.Shared.DTOs;
using ConnectCore.UserService.Data;
using Microsoft.EntityFrameworkCore;

namespace ConnectCore.UserService.Services;

public interface IUserService
{
    Task<PagedResponse<UserDto>> GetUsersAsync(int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
    Task<ApiResponse<bool>> ActivateUserAsync(Guid id);
    Task<ApiResponse<bool>> DeactivateUserAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResponse<UserDto>> GetUsersAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = await _context.Users.Where(u => !u.IsDeleted).CountAsync();
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => MapToDto(u))
                .ToListAsync();

            return PagedResponse<UserDto>.Create(users, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return new PagedResponse<UserDto>
            {
                Success = false,
                Message = "Error retrieving users"
            };
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResult("User not found");
            }

            return ApiResponse<UserDto>.SuccessResult(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", id);
            return ApiResponse<UserDto>.FailureResult("Error retrieving user");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResult("User not found");
            }

            return ApiResponse<UserDto>.SuccessResult(MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return ApiResponse<UserDto>.FailureResult("Error retrieving user");
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
            if (existingUser != null)
            {
                return ApiResponse<UserDto>.FailureResult("User with this email already exists");
            }

            var user = new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber,
                DateOfBirth = createUserDto.DateOfBirth,
                Status = UserStatus.Active,
                Address = createUserDto.Address != null ? new Address
                {
                    Street = createUserDto.Address.Street,
                    City = createUserDto.Address.City,
                    State = createUserDto.Address.State,
                    PostalCode = createUserDto.Address.PostalCode,
                    Country = createUserDto.Address.Country
                } : null,
                CreatedBy = "API"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully with ID {UserId}", user.Id);
            return ApiResponse<UserDto>.SuccessResult(MapToDto(user), "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ApiResponse<UserDto>.FailureResult("Error creating user");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<UserDto>.FailureResult("User not found");
            }

            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == updateUserDto.Email);
                if (existingUser != null)
                {
                    return ApiResponse<UserDto>.FailureResult("User with this email already exists");
                }
                user.Email = updateUserDto.Email;
            }

            if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName;

            if (!string.IsNullOrEmpty(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName;

            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                user.PhoneNumber = updateUserDto.PhoneNumber;

            if (updateUserDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateUserDto.DateOfBirth.Value;

            if (updateUserDto.Address != null)
            {
                user.Address = new Address
                {
                    Street = updateUserDto.Address.Street,
                    City = updateUserDto.Address.City,
                    State = updateUserDto.Address.State,
                    PostalCode = updateUserDto.Address.PostalCode,
                    Country = updateUserDto.Address.Country
                };
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated successfully with ID {UserId}", id);
            return ApiResponse<UserDto>.SuccessResult(MapToDto(user), "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return ApiResponse<UserDto>.FailureResult("Error updating user");
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResult("User not found");
            }

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted successfully with ID {UserId}", id);
            return ApiResponse<bool>.SuccessResult(true, "User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return ApiResponse<bool>.FailureResult("Error deleting user");
        }
    }

    public async Task<ApiResponse<bool>> ActivateUserAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResult("User not found");
            }

            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("User activated successfully with ID {UserId}", id);
            return ApiResponse<bool>.SuccessResult(true, "User activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return ApiResponse<bool>.FailureResult("Error activating user");
        }
    }

    public async Task<ApiResponse<bool>> DeactivateUserAsync(Guid id)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
            if (user == null)
            {
                return ApiResponse<bool>.FailureResult("User not found");
            }

            user.Status = UserStatus.Inactive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("User deactivated successfully with ID {UserId}", id);
            return ApiResponse<bool>.SuccessResult(true, "User deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return ApiResponse<bool>.FailureResult("Error deactivating user");
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Status = user.Status.ToString(),
            Address = user.Address != null ? new AddressDto
            {
                Street = user.Address.Street,
                City = user.Address.City,
                State = user.Address.State,
                PostalCode = user.Address.PostalCode,
                Country = user.Address.Country
            } : null,
            CreatedAt = user.CreatedAt
        };
    }
}
