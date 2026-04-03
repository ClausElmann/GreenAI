using GreenAi.Api.Features.Api.V1.Auth.Token;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GreenAi.Tests.Features.Api.V1.Auth.Token;

public sealed class GetApiTokenHandlerTests
{
    private static JwtTokenService CreateJwtService() =>
        new(Options.Create(new JwtOptions
        {
            SecretKey = "test-secret-key-that-is-long-enough-for-hmac256",
            Issuer = "greenai-test",
            Audience = "greenai-test",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 30
        }));

    private static ApiTokenUserRecord ValidApiUser()
    {
        var (hash, salt) = PasswordHasher.Hash("correct-password");
        return new ApiTokenUserRecord(
            Id: 1,
            Email: "api@example.com",
            PasswordHash: hash,
            PasswordSalt: salt,
            IsLockedOut: false,
            FailedLoginCount: 0,
            LanguageId: 1);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsInvalidCredentials()
    {
        var repo   = Substitute.For<IGetApiTokenRepository>();
        var writer = Substitute.For<IRefreshTokenWriter>();
        repo.FindUserAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((ApiTokenUserRecord?)null);

        var handler = new GetApiTokenHandler(repo, CreateJwtService(), writer);
        var result  = await handler.Handle(
            new GetApiTokenCommand("api@example.com", "pass", 1, 1),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_LockedOutUser_ReturnsAccountLocked()
    {
        var repo   = Substitute.For<IGetApiTokenRepository>();
        var writer = Substitute.For<IRefreshTokenWriter>();
        var user   = ValidApiUser() with { IsLockedOut = true };
        repo.FindUserAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>()).Returns(user);

        var handler = new GetApiTokenHandler(repo, CreateJwtService(), writer);
        var result  = await handler.Handle(
            new GetApiTokenCommand("api@example.com", "correct-password", 1, 1),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var repo   = Substitute.For<IGetApiTokenRepository>();
        var writer = Substitute.For<IRefreshTokenWriter>();
        var user   = ValidApiUser();
        repo.FindUserAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>()).Returns(user);

        var handler = new GetApiTokenHandler(repo, CreateJwtService(), writer);
        var result  = await handler.Handle(
            new GetApiTokenCommand("api@example.com", "correct-password", 1, 1),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var repo   = Substitute.For<IGetApiTokenRepository>();
        var writer = Substitute.For<IRefreshTokenWriter>();
        var user   = ValidApiUser();
        repo.FindUserAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>()).Returns(user);

        var handler = new GetApiTokenHandler(repo, CreateJwtService(), writer);
        var result  = await handler.Handle(
            new GetApiTokenCommand("api@example.com", "wrong-password", 1, 1),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error!.Code);
    }
}
