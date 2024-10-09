using System.Security.Claims;

namespace HermesWebApi
{
    public interface IUserService
    {
        string GetUserId();
    }
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Method to get the userID from the current request's JWT token
        public string GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue("userID"); // Or use the correct claim type like "sub"
        }
    }
}
