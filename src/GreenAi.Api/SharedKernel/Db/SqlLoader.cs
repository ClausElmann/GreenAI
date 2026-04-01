using System.Reflection;

namespace GreenAi.Api.SharedKernel.Db;

public static class SqlLoader
{
    /// <summary>
    /// Indlæs SQL-fil ved fuldt resource-navn.
    /// </summary>
    public static string Load(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded SQL resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Indlæs SQL-fil fra samme namespace som T.
    /// Konvention: T er repository-klassen, filnavn er SQL-filens navn.
    /// Eksempel: SqlLoader.Load&lt;LoginRepository&gt;("FindUserByEmail.sql")
    /// → GreenAi.Api.Features.Auth.Login.FindUserByEmail.sql
    /// </summary>
    public static string Load<T>(string fileName)
    {
        var ns = typeof(T).Namespace
            ?? throw new InvalidOperationException($"Type {typeof(T).Name} has no namespace");
        var resourceName = $"{ns}.{fileName}";
        return Load(resourceName);
    }
}

