using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;

namespace GreenAi.Api.Features.Auth.SelectCustomer;

public sealed record SelectCustomerCommand(int CustomerId)
    : IRequest<Result<SelectCustomerResponse>>, IRequireAuthentication;
