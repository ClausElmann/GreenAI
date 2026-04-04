using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.Logout;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Result<LogoutResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IDbSession _db;

    public LogoutHandler(ICurrentUser currentUser, IDbSession db)
    {
        _currentUser = currentUser;
        _db          = db;
    }

    public async Task<Result<LogoutResponse>> Handle(LogoutCommand request, CancellationToken ct)
    {
        var sql = SqlLoader.Load<LogoutHandler>("DeleteRefreshTokens.sql");
        await _db.ExecuteAsync(sql, new { UserId = _currentUser.UserId.Value });
        return Result<LogoutResponse>.Ok(new LogoutResponse());
    }
}
