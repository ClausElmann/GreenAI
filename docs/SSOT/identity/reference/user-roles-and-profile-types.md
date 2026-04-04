# UserRole + ProfileType — green-ai

> **Autoritet:** SSOT for UserRole-capabilities og ProfileType-klassifikationer.
> **Kilde:** Analyseret fra sms-service — ikke kopi. green-ai bruger `UserRoleNames` (string-konvention, ikke enum int).

---

## UserRole

En UserRole er et **globalt admin/UI capability-flag per bruger** — IKKE per customer.

```
DoesUserHaveRoleAsync(userId, UserRoleNames.ManageUsers)
→ true/false — gater adgang til administrative funktioner
```

**Forskel fra ProfileRole:** UserRole = hvad brugeren *må administrere*. ProfileRole = hvad profilen *må sende*.

### Alle UserRoles

| Navn | ID (sms-service) | Beskrivelse |
|------|-----------------|-------------|
| `SuperAdmin` | 1 | Fuld systemadgang — kun green-ai-ansatte. Bypasser de fleste autorisationstjek. |
| `ManageUsers` | 2 | Administrér brugere (opret, rediger, deaktivér) |
| `ManageProfiles` | 3 | Administrér profiler (opret, rediger, slet) |
| `CanCreateScheduledBroadcasts` | 13 | Opret schedulerede udsendelser |
| `ManageReports` | 14 | Adgang til rapport-modul |
| `ManageMessages` | 15 | Administrér beskabeskeder og skabeloner |
| `ManageBenchmarks` | 17 | Administrér benchmark-data |
| `ManageCustomer` | 19 | Administrér kundeindstillinger |
| `Benchmark` | 21 | Adgang til benchmark-visning |
| `CustomerSetup` | 22 | Opsætning af nye kunder |
| `Searching` | 23 | Adgang til søgefunktioner |
| `WEBMessages` | 24 | Webbesked-modul |
| `StandardReceivers` | 25 | Standardmodtager-administration |
| `SubscriptionModule` | 26 | Abonnement-modul |
| `MessageTemplates` | 27 | Beskedskabeloner |
| `CanSetupStatstidende` | 28 | Opsætning af Statstidende-integration |
| `API` | 29 | Maskin-til-maskin API-adgang (bruges til `POST /api/v1/auth/token`) |
| `CanSetupSubscriptionReminders` | 30 | Opsætning af abonnementspåmindelser |
| `Protected` | 31 | Bruger er låst — ingen ændringer tilladt |
| `TwoFactorAuthenticate` | 32 | Kræver 2FA ved login (fase 2) |
| `ADLogin` | 33 | Bruger autentificeres via Active Directory (fase 2) |
| `RequiresApproval` | 34 | Broadcasts kræver godkendelse før afsendelse |
| `WeatherWarning` | 35 | Adgang til vejrvarslingsopsætning |
| `AlwaysTestMode` | 36 | Alle udsendelser er altid i testmodus |
| `LimitedUser` | 37 | Begrænset bruger — reduceret adgang |
| `CanManageCriticalAddresses` | 38 | Administrér kritiske adresser |
| `CanSendSingleSmsAndEmail` | 39 | Enkelt SMS/email direkte (uden broadcast) |
| `CanManageScenarios` | 40 | Administrér scenarier |

### green-ai implementeringsregler

```
✅ Rollerne sammenlignes som strings (UserRoleNames konstanter)
✅ SuperAdmin-tjek: IsUserSuperAdminAsync(userId) — delegerer til DoesUserHaveRoleAsync
✅ API-rolle: bruges til maskin-til-maskin token (POST /api/v1/auth/token)
✅ Protected-rolle: bruger med Protected-rolle må aldrig ændres via admin-UI
❌ UserRole giver IKKE automatisk ProfileRole
❌ Aldrig hardcode rolle-strings — brug UserRoleNames-konstanterne
```

```csharp
// green-ai UserRoleNames (string-konstanter, ikke enum int)
public static class UserRoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string ManageUsers = "ManageUsers";
    public const string ManageProfiles = "ManageProfiles";
    public const string API = "API";
    public const string ManageCustomer = "ManageCustomer";
    public const string RequiresApproval = "RequiresApproval";
    public const string AlwaysTestMode = "AlwaysTestMode";
    public const string Protected = "Protected";
    // Tilføj løbende når handler kræver dem
}
```

**Fase 1 (SLICE-001/002):** Kun `SuperAdmin` og `API` nødvendige. Øvrige tilføjes progressivt.

---

## ProfileType

ProfileType klassificerer forsyningstypen/formålet for en profil. Bruges til:
- Visuel gruppering i UI
- Eventuelle type-specifikke afsendelsesregler

| Navn (green-ai) | ID | Original dansk betegnelse | Beskrivelse |
|----|---|---|---|
| `None` | 0 | Ingen | Uklassificeret / ikke sat |
| `Water` | 2 | Vand | Vandforsyning |
| `WasteWater` | 3 | Spildevand | Spildevandsforsyning |
| `DistrictHeating` | 4 | Fjernvarme | Fjernvarmeforsyning |
| `Electricity` | 5 | El | Elforsyning |
| `Renovation` | 6 | Renovation | Renovation/affald |
| `Envitrix` | 7 | Envitrix | Envitrix-platform |
| `Broadband` | 8 | Bredbånd | Bredbåndsudbyder |
| `TV` | 9 | TV | TV-/kabeludbyder |
| `Housing` | 10 | Boligselskab | Boligselskab |
| `CityGas` | 11 | Bygas | Bygasforsyning |
| `System` | 13 | System | Systembrugerprofil (interne operationer) |
| `Standard` | 14 | Standard | Standard — ikke forsyningsspecifik |

**Note om ID 12:** Ikke tildelt i kildekoden — spring over i seed-data.

### green-ai implementeringsregler

```
✅ ProfileType enum bruges til klassifikation — ingen adfærdsforskell i fase 1
✅ Seed alle 13 typer i ProfileTypes-tabel ved opstart
✅ Brug engelske enum-navne i C# (ikke danske) — green-ai konvention
❌ Ingen forretningslogik baseret på ProfileType i fase 1
```

```csharp
public enum ProfileType
{
    None = 0,
    Water = 2,
    WasteWater = 3,
    DistrictHeating = 4,
    Electricity = 5,
    Renovation = 6,
    Envitrix = 7,
    Broadband = 8,
    TV = 9,
    Housing = 10,
    CityGas = 11,
    System = 13,
    Standard = 14
}
```
