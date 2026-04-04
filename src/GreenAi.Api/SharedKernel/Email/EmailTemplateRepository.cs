using Dapper;
using GreenAi.Api.SharedKernel.Db;

namespace GreenAi.Api.SharedKernel.Email;

/// <summary>
/// Repository: loads EmailTemplate rows from DB.
/// Falls back to EN (LanguageId=3) if requested language has no template.
/// </summary>
public sealed class EmailTemplateRepository
{
    private readonly IDbSession _db;

    public EmailTemplateRepository(IDbSession db) => _db = db;

    public Task<EmailTemplateRow?> FindAsync(string templateName, int languageId)
    {
        const string sql = """
            SELECT TOP 1
                [Id], [Name], [LanguageId], [Subject], [BodyHtml]
            FROM [dbo].[EmailTemplates]
            WHERE [Name] = @Name
              AND [LanguageId] IN (@LanguageId, 3)
            ORDER BY
                CASE WHEN [LanguageId] = @LanguageId THEN 0 ELSE 1 END
            """;

        return _db.QuerySingleOrDefaultAsync<EmailTemplateRow>(sql,
            new { Name = templateName, LanguageId = languageId });
    }
}

public sealed record EmailTemplateRow(
    int    Id,
    string Name,
    int    LanguageId,
    string Subject,
    string BodyHtml);
