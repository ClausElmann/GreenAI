using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Dapper implementation of IRefreshTokenWriter.
/// Loads SQL from SharedKernel.Auth via SqlLoader namespace convention.
/// Register as Scoped.
/// </summary>
public sealed class RefreshTokenWriter : IRefreshTokenWriter
{
    private readonly IDbSession _db;

    public RefreshTokenWriter(IDbSession db) => _db = db;

    public Task SaveAsync(
        UserId userId,
        CustomerId customerId,
        ProfileId profileId,
        string token,
        DateTimeOffset expiresAt,
        int languageId)
        => _db.ExecuteAsync(
            SqlLoader.Load<RefreshTokenWriter>("SaveRefreshToken.sql"),
            new
            {
                UserId     = userId.Value,
                CustomerId = customerId.Value,
                ProfileId  = profileId.Value,
                Token      = token,
                ExpiresAt  = expiresAt,
                LanguageId = languageId
            });
}
