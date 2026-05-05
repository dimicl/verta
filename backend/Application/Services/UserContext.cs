using System.Security.Claims;

public class UserContext: IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var id = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(id))
            throw new Exception("User ID not found in token");

        return int.Parse(id);
    }
}