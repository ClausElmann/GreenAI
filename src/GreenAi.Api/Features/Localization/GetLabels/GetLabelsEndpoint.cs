using GreenAi.Api.SharedKernel.Localization;

namespace GreenAi.Api.Features.Localization.GetLabels;

/// <summary>
/// Returns all labels for the given language as a flat dictionary.
/// Used by the frontend at startup to bootstrap its translation table.
///
/// Unknown languageId returns an empty dictionary (no error) —
/// the frontend falls back to key names, which is the designed fail-open behaviour.
/// </summary>
public static class GetLabelsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/localization/{languageId:int}", async (
            int languageId,
            ILocalizationService localization,
            CancellationToken ct) =>
        {
            var labels = await localization.GetAllAsync(languageId, ct);
            return Microsoft.AspNetCore.Http.Results.Ok(labels);
        });
    }
}
