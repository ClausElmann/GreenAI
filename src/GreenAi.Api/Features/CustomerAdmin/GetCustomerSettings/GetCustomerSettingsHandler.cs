using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.CustomerAdmin.GetCustomerSettings;

public record GetCustomerSettingsQuery : IRequest<Result<CustomerSettingsRow>>, IRequireAuthentication, IRequireProfile;

public record CustomerSettingsRow(int Id, string Name);

public sealed class GetCustomerSettingsHandler(IDbSession db, ICurrentUser user)
    : IRequestHandler<GetCustomerSettingsQuery, Result<CustomerSettingsRow>>
{
    public async Task<Result<CustomerSettingsRow>> Handle(GetCustomerSettingsQuery _, CancellationToken ct)
    {
        var sql = SqlLoader.Load<GetCustomerSettingsHandler>("GetCustomerSettings.sql");
        var row = await db.QuerySingleOrDefaultAsync<CustomerSettingsRow>(sql, new { CustomerId = user.CustomerId.Value });

        return row is null
            ? Result<CustomerSettingsRow>.Fail("NOT_FOUND", "Customer not found.")
            : Result<CustomerSettingsRow>.Ok(row);
    }
}
