using Serilog.Sinks.MSSqlServer;
using System.Data;

namespace GreenAi.Api.SharedKernel.Logging;

/// <summary>
/// Definerer ekstra kolonner i Logs-tabellen ud over Serilogs defaults.
/// </summary>
public static class SerilogColumnOptions
{
    public static ColumnOptions Build()
    {
        var options = new ColumnOptions();

        // Fjern XML Properties-kolonne — vi bruger JSON
        options.Store.Remove(StandardColumn.Properties);
        options.Store.Add(StandardColumn.LogEvent);

        options.LogEvent.DataLength = -1;
        options.LogEvent.ExcludeAdditionalProperties = false;

        // Ekstra kolonner
        options.AdditionalColumns =
        [
            new SqlColumn { ColumnName = "SourceContext", DataType = SqlDbType.NVarChar, DataLength = 256, AllowNull = true },
            new SqlColumn { ColumnName = "TraceId",       DataType = SqlDbType.NVarChar, DataLength = 64,  AllowNull = true },
            new SqlColumn { ColumnName = "UserId",        DataType = SqlDbType.Int,                        AllowNull = true },
            new SqlColumn { ColumnName = "CustomerId",    DataType = SqlDbType.Int,                        AllowNull = true },
        ];

        return options;
    }
}
