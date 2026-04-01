using System.Security.Claims;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// ICurrentUser implementation that reads from the authenticated ClaimsPrincipal.
/// Register as Scoped. Never read HttpContext.User directly outside this class.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public UserId UserId =>
        new(int.Parse(Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!));

    public CustomerId CustomerId =>
        new(int.Parse(Principal!.FindFirstValue(GreenAiClaims.CustomerId)!));

    public ProfileId ProfileId =>
        new(int.Parse(Principal!.FindFirstValue(GreenAiClaims.ProfileId)!));

    public int LanguageId =>
        int.Parse(Principal!.FindFirstValue(GreenAiClaims.LanguageId)!);

    public string Email =>
        Principal!.FindFirstValue(ClaimTypes.Email)!;

    public bool IsImpersonating =>
        Principal?.FindFirstValue(GreenAiClaims.ImpersonatedUserId) is not null;

    public UserId? OriginalUserId
    {
        get
        {
            var value = Principal?.FindFirstValue(GreenAiClaims.ImpersonatedUserId);
            return value is not null ? new UserId(int.Parse(value)) : null;
        }
    }
}
