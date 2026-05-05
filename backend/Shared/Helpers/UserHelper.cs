public static class UserHelper
{
    public static UserResponse ToEntity(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Status = user.Status,
        };
    }
}