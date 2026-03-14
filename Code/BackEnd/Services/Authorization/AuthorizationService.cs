namespace Services.Authorization;

public interface IAuthorizationService
{
    Task<string> GetAuthorizationInfoAsync();
}

public class AuthorizationService : IAuthorizationService
{
    public Task<string> GetAuthorizationInfoAsync()
    {
        return Task.FromResult("Authorization service placeholder");
    }
}
