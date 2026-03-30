# GreenAI

## Stack
- .NET 10 / Blazor Server
- Dapper + SQL Server
- MediatR + FluentValidation
- Custom JWT auth
- Vertical slice architecture

## Quick start
```
dotnet run --project src/GreenAi.Api
```

Database migrations køres automatisk ved opstart via DbUp.

### Krav
- .NET 10 SDK
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)

### Connection string
Konfigureres i `src/GreenAi.Api/appsettings.Development.json`.
