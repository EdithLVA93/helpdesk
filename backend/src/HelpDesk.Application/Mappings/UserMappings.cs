using HelpDesk.Application.DTOs.Users;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Mappings;

internal static class UserMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}
