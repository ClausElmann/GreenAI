using GreenAi.Api.Features.Auth.RefreshToken;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GreenAi.Tests.Features.Auth;

public sealed class RefreshTokenHandlerTests
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

    private static RefreshTokenRecord ValidRecord() =>
        new(Id: 99, UserId: 1, CustomerId: 10, LanguageId: 1, ProfileId: 100, Email: "user@example.com");

    [Fact]
    public async Task Handle_ValidRefreshToken_ReturnNewTokens()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        var jwt = CreateJwtService();
        var record = ValidRecord();

        repository.FindValidTokenAsync("valid-token").Returns(record);
        repository.RevokeTokenAsync(Arg.Any<int>()).Returns(Task.CompletedTask);
        repository.SaveNewTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>())
            .Returns(Task.CompletedTask);

        var handler = new RefreshTokenHandler(repository, jwt);
        var result = await handler.Handle(new RefreshTokenCommand("valid-token"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_RevokesOldToken()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        var jwt = CreateJwtService();
        var record = ValidRecord();

        repository.FindValidTokenAsync("valid-token").Returns(record);
        repository.RevokeTokenAsync(Arg.Any<int>()).Returns(Task.CompletedTask);
        repository.SaveNewTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>())
            .Returns(Task.CompletedTask);

        var handler = new RefreshTokenHandler(repository, jwt);
        await handler.Handle(new RefreshTokenCommand("valid-token"), TestContext.Current.CancellationToken);

        await repository.Received(1).RevokeTokenAsync(record.Id);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_SavesNewTokenWithSameProfileId()
    {
        // RULE: ProfileId must be propagated from stored record through token rotation.
        var repository = Substitute.For<IRefreshTokenRepository>();
        var jwt = CreateJwtService();
        var record = ValidRecord();

        repository.FindValidTokenAsync("valid-token").Returns(record);
        repository.RevokeTokenAsync(Arg.Any<int>()).Returns(Task.CompletedTask);
        repository.SaveNewTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>())
            .Returns(Task.CompletedTask);

        var handler = new RefreshTokenHandler(repository, jwt);
        await handler.Handle(new RefreshTokenCommand("valid-token"), TestContext.Current.CancellationToken);

        await repository.Received(1).SaveNewTokenAsync(
            new UserId(record.UserId),
            new CustomerId(record.CustomerId),
            new ProfileId(record.ProfileId),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            record.LanguageId);
    }

    [Fact]
    public async Task Handle_InvalidOrExpiredToken_ReturnsError()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        var jwt = CreateJwtService();

        repository.FindValidTokenAsync(Arg.Any<string>()).Returns((RefreshTokenRecord?)null);

        var handler = new RefreshTokenHandler(repository, jwt);
        var result = await handler.Handle(new RefreshTokenCommand("expired-or-used-token"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_REFRESH_TOKEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidToken_DoesNotRevokeOrSave()
    {
        var repository = Substitute.For<IRefreshTokenRepository>();
        var jwt = CreateJwtService();

        repository.FindValidTokenAsync(Arg.Any<string>()).Returns((RefreshTokenRecord?)null);

        var handler = new RefreshTokenHandler(repository, jwt);
        await handler.Handle(new RefreshTokenCommand("bad-token"), TestContext.Current.CancellationToken);

        await repository.DidNotReceive().RevokeTokenAsync(Arg.Any<int>());
        await repository.DidNotReceive().SaveNewTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }
}
