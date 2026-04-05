namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Lightweight representation of a customer available for selection during login.
/// Used when the user has memberships in more than one customer.
/// </summary>
public sealed record CustomerSummary(int CustomerId, string Name);
