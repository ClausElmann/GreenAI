using Dapper;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.CustomerAdmin.GetCustomerSettings;

public record GetCustomerSettingsQuery : IRequest<Result<CustomerSettingsRow>>;

public record CustomerSettingsRow(int Id, string Name);

public sealed class GetCustomerSettingsHandler(IDbSession db, ICurrentUser user)
    : IRequestHandler<GetCustomerSettingsQuery, Result<CustomerSettingsRow>>
{
    public async Task<Result<CustomerSettingsRow>> Handle(GetCustomerSettingsQuery _, CancellationToken ct)
    {
        if (!user.IsAuthenticated || !HasCustomerId())
            return Result<CustomerSettingsRow>.Fail("NO_CUSTOMER", "No customer selected.");

        var sql = SqlLoader.Load<GetCustomerSettingsHandler>("GetCustomerSettings.sql");
        var row = await db.QuerySingleOrDefaultAsync<CustomerSettingsRow>(sql, new { CustomerId = user.CustomerId.Value });

        return row is null
            ? Result<CustomerSettingsRow>.Fail("CUSTOMER_NOT_FOUND", "Customer not found.")
            : Result<CustomerSettingsRow>.Ok(row);
    }

    private bool HasCustomerId()
    {
        try { _ = user.CustomerId; return true; }
        catch (InvalidOperationException) { return false; }
    }
}
