namespace GreenAi.Api.Mocks;

/// <summary>
/// Static mock data for UI LOCK PHASE — no database calls.
/// Replace individual sections with real data providers when backend features are implemented.
/// </summary>

public sealed record BroadcastMock(
    int Id,
    string Subject,
    string Status,        // "Sent" | "Scheduled" | "Failed"
    int Recipients,
    int Delivered,
    int FailedCount,
    DateTime CreatedAt,
    DateTime? ScheduledAt,
    string Profile);

public sealed record DraftMock(
    int Id,
    string Subject,
    int Recipients,
    DateTime LastModified,
    string Profile);

public sealed record UserMock(
    int Id,
    string Email,
    string DisplayName,
    string Role,
    string Status,        // "Active" | "Inactive" | "Pending"
    DateTime LastLogin,
    DateTime CreatedAt);

public sealed record ActivityMock(
    string Action,
    string Subject,
    string Profile,
    DateTime At,
    string Severity);     // "success" | "error" | "warning" | "info"

public sealed record ScenarioMock(
    int    Id,
    string Name,
    string Description,
    string Profile,
    int    LastUsedDaysAgo);

public sealed record StdReceiverGroupMock(
    int    Id,
    string Name,
    int    RecipientCount,
    string Profile);

public sealed record CountryMock(int Id, string Name, string Code);

public sealed record CustomerContextMock(
    int      Id,
    string   Name,
    int      CountryId,
    int      UserCount,
    int      ProfileCount,
    DateTime LastActivity);

public sealed record ProfileContextMock(int Id, string Name, int CustomerId);

public sealed record StatusReportItemMock(
    string    RecipientName,
    string    PhoneNumber,
    string    Channel,
    string    Status,          // "Leveret" | "Fejlet" | "Afvist" | "Afventende"
    DateTime? DeliveredAt);

public static class MockData
{
    private static DateTime DaysAgo(int days, int hour = 10, int minute = 0) =>
        DateTime.Today.AddDays(-days).AddHours(hour).AddMinutes(minute);

    public static readonly IReadOnlyList<BroadcastMock> Broadcasts =
    [
        // ── Sent ──────────────────────────────────────────────────────────────────
        new(1,  "Reminder: Din tid i morgen kl. 09:30",       "Sent",      247, 241,   6, DaysAgo(0,  8,  5), null, "VetClinic Aarhus"),
        new(2,  "Flash tilbud: 30% rabat hele weekenden",     "Sent",     1843,1798,  45, DaysAgo(1, 14, 10), null, "RetailPlus"),
        new(3,  "Din ordre #4821 er afsendt",                 "Sent",      312, 310,   2, DaysAgo(1, 11, 30), null, "RetailPlus"),
        new(4,  "Vedligeholdelsesvindue: i nat kl. 23-01",    "Sent",      428, 427,   1, DaysAgo(2, 16, 15), null, "TechSupport A/S"),
        new(5,  "Ny funktion tilgængelig: Se dit dashboard",  "Sent",      891, 878,  13, DaysAgo(3,  9, 45), null, "TechSupport A/S"),
        new(6,  "Bekræftelse: Din aftale d. 7. april",        "Sent",      183, 183,   0, DaysAgo(3, 13,  0), null, "DentalGroup Nord"),
        new(7,  "Dit abonnement udløber om 3 dage",           "Sent",      564, 551,  13, DaysAgo(4, 10, 20), null, "FitnessChain DK"),
        new(8,  "Velkomst til FitnessChain – prøv gratis!",   "Sent",     1204,1189,  15, DaysAgo(5,  9,  0), null, "FitnessChain DK"),
        new(9,  "Husk: Tandlægetid fredag kl. 13:15",         "Sent",       97,  95,   2, DaysAgo(5, 15, 30), null, "DentalGroup Nord"),
        new(10, "Password nulstillet – kontakt support",      "Sent",       23,  23,   0, DaysAgo(6,  8, 55), null, "TechSupport A/S"),
        new(11, "Nyt produkt: GRØN serie nu tilgængelig",     "Sent",     2341,2287,  54, DaysAgo(7, 10, 15), null, "RetailPlus"),
        new(12, "Din hund har tid til vaccination d. 12/4",   "Sent",      156, 154,   2, DaysAgo(8, 11, 40), null, "VetClinic Aarhus"),
        new(13, "Sæsonudsalg starter mandag kl. 08:00",       "Sent",     3102,3021,  81, DaysAgo(9, 14,  5), null, "RetailPlus"),
        new(14, "Servicevindue afsluttet – alt køre normalt", "Sent",      428, 428,   0, DaysAgo(10, 9, 25), null, "TechSupport A/S"),
        new(15, "Månedlig rapport klar: April 2026",          "Sent",       89,  87,   2, DaysAgo(11, 8, 10), null, "FitnessChain DK"),

        // ── Scheduled ─────────────────────────────────────────────────────────────
        new(16, "Påmindelse: Fitness bootcamp d. 8. april",   "Scheduled",  672, 0,  0, DaysAgo(0,  7, 30), DateTime.Today.AddDays(3).AddHours(8),  "FitnessChain DK"),
        new(17, "Påsketilbud: Fri fragt på alle varer",       "Scheduled", 2847, 0,  0, DaysAgo(0,  9,  0), DateTime.Today.AddDays(1).AddHours(6),  "RetailPlus"),
        new(18, "Opdatering: Ny app version kl. 02:00",       "Scheduled", 1204, 0,  0, DaysAgo(0, 10, 15), DateTime.Today.AddDays(2).AddHours(2),  "TechSupport A/S"),
        new(19, "Velkomst: Nye kunder i april 2026",          "Scheduled",   47, 0,  0, DaysAgo(0, 11, 45), DateTime.Today.AddDays(4).AddHours(9),  "VetClinic Aarhus"),
        new(20, "Kvartalsnyt: Q1 resultater til alle",        "Scheduled",  312, 0,  0, DaysAgo(0, 12, 20), DateTime.Today.AddDays(7).AddHours(8),  "DentalGroup Nord"),

        // ── Failed ─────────────────────────────────────────────────────────────────
        new(21, "Kampagne: Gratis prøveperiode",              "Failed",    445, 112, 333, DaysAgo(1, 13, 10), null, "FitnessChain DK"),
        new(22, "Urgent: Bekræft din e-mail nu",              "Failed",   1243,  87,1156, DaysAgo(3, 11,  5), null, "TechSupport A/S"),
        new(23, "Black friday teaser – klar til dig!",        "Failed",   3891, 201,3690, DaysAgo(6, 15, 30), null, "RetailPlus"),
        new(24, "Sommerkampagne 2026 v1",                     "Failed",    234,   0, 234, DaysAgo(12, 9, 15), null, "DentalGroup Nord"),
        new(25, "Reminder batch – manglende numre",           "Failed",     89,   0,  89, DaysAgo(15, 10, 0), null, "VetClinic Aarhus"),
    ];

    public static IReadOnlyList<BroadcastMock> SentBroadcasts =>
        Broadcasts.Where(b => b.Status == "Sent").OrderByDescending(b => b.CreatedAt).ToList();

    public static IReadOnlyList<BroadcastMock> ScheduledBroadcasts =>
        Broadcasts.Where(b => b.Status == "Scheduled").OrderBy(b => b.ScheduledAt).ToList();

    public static IReadOnlyList<BroadcastMock> FailedBroadcasts =>
        Broadcasts.Where(b => b.Status == "Failed").OrderByDescending(b => b.CreatedAt).ToList();

    public static readonly IReadOnlyList<DraftMock> Drafts =
    [
        new(1, "Black friday teaser v2",               847, DateTime.Now.AddHours(-1).AddMinutes(-23),  "RetailPlus"),
        new(2, "Påskeåbent – tilpas tekst inden send", 312, DateTime.Now.AddHours(-4).AddMinutes(-15),  "VetClinic Aarhus"),
        new(3, "Ny kampagne: Maj fitness challenge",  1204, DateTime.Now.AddDays(-1).AddHours(-2),       "FitnessChain DK"),
        new(4, "Servicevindue – næste uge",            428, DateTime.Now.AddDays(-2).AddHours(-5),       "TechSupport A/S"),
        new(5, "Velkomstbesked nye kunder (UDKAST)",    23, DateTime.Now.AddDays(-5).AddHours(-3),       "DentalGroup Nord"),
    ];

    public static readonly IReadOnlyList<UserMock> Users =
    [
        new(1,  "claus.elmann@gmail.com",         "Claus Elmann",      "SuperAdmin",    "Active",   DateTime.Now.AddMinutes(-5),        DaysAgo(180)),
        new(2,  "anna.nielsen@retailplus.dk",      "Anna Nielsen",      "ManageUsers",   "Active",   DaysAgo(0,  9,  0),                 DaysAgo(90)),
        new(3,  "mikkel.sorensen@fitchain.dk",     "Mikkel Sørensen",   "CanBroadcast",  "Active",   DaysAgo(1, 14, 20),                 DaysAgo(60)),
        new(4,  "line.madsen@vetclinic.dk",        "Line Madsen",       "CanBroadcast",  "Active",   DaysAgo(0, 11, 40),                 DaysAgo(45)),
        new(5,  "peter.jensen@techsupport.dk",     "Peter Jensen",      "ManageUsers",   "Active",   DaysAgo(2, 16, 10),                 DaysAgo(120)),
        new(6,  "sara.christensen@dental.dk",      "Sara Christensen",  "CanBroadcast",  "Active",   DaysAgo(1, 10, 55),                 DaysAgo(30)),
        new(7,  "tobias.larsen@retailplus.dk",     "Tobias Larsen",     "Viewer",        "Active",   DaysAgo(3, 15,  5),                 DaysAgo(20)),
        new(8,  "maja.pedersen@fitchain.dk",       "Maja Pedersen",     "CanBroadcast",  "Inactive", DaysAgo(14, 9, 25),                 DaysAgo(200)),
        new(9,  "erik.hansen@techsupport.dk",      "Erik Hansen",       "ManageUsers",   "Active",   DaysAgo(0,  8, 35),                 DaysAgo(75)),
        new(10, "emilie.thomsen@vetclinic.dk",     "Emilie Thomsen",    "Viewer",        "Active",   DaysAgo(5, 13, 45),                 DaysAgo(15)),
        new(11, "david.andersen@dental.dk",        "David Andersen",    "CanBroadcast",  "Pending",  DaysAgo(30, 9,  0),                 DaysAgo(1)),
        new(12, "nanna.olsen@retailplus.dk",       "Nanna Olsen",       "Viewer",        "Inactive", DaysAgo(45, 10, 30),                DaysAgo(180)),
        new(13, "kasper.nielsen@fitchain.dk",      "Kasper Nielsen",    "CanBroadcast",  "Active",   DaysAgo(0,  7, 15),                 DaysAgo(55)),
        new(14, "ida.mortensen@techsupport.dk",    "Ida Mortensen",     "ManageUsers",   "Pending",  DaysAgo(7, 11, 50),                 DaysAgo(3)),
        new(15, "bo.kristensen@vetclinic.dk",      "Bo Kristensen",     "Viewer",        "Active",   DaysAgo(2, 14, 40),                 DaysAgo(40)),
    ];

    public static readonly IReadOnlyList<ActivityMock> Activities =
    [
        new("Sendt",      "Reminder: Din tid i morgen kl. 09:30",   "VetClinic Aarhus", DateTime.Now.AddHours(-2),                "success"),
        new("Planlagt",   "Påmindelse: Fitness bootcamp d. 8/4",    "FitnessChain DK",  DateTime.Now.AddHours(-3).AddMinutes(-15),"info"),
        new("Sendt",      "Flash tilbud: 30% rabat hele weekenden", "RetailPlus",        DateTime.Now.AddHours(-26),               "success"),
        new("Fejlet",     "Kampagne: Gratis prøveperiode",          "FitnessChain DK",  DateTime.Now.AddHours(-27),               "error"),
        new("Sendt",      "Din ordre #4821 er afsendt",             "RetailPlus",        DateTime.Now.AddHours(-37),               "success"),
        new("Kladde",     "Black friday teaser v2",                 "RetailPlus",        DateTime.Now.AddHours(-1).AddMinutes(-23),"warning"),
        new("Planlagt",   "Påsketilbud: Fri fragt på alle varer",   "RetailPlus",        DateTime.Now.AddHours(-9),                "info"),
        new("Sendt",      "Vedligeholdelsesvindue: i nat kl. 23",   "TechSupport A/S",  DateTime.Now.AddDays(-2),                 "success"),
        new("Sendt",      "Ny funktion tilgængelig: dashboard",     "TechSupport A/S",  DateTime.Now.AddDays(-3),                 "success"),
        new("Annulleret", "Kampagne pause – venter på godkend.",    "DentalGroup Nord", DateTime.Now.AddDays(-4),                 "warning"),
    ];

    // ── KPI aggregates ─────────────────────────────────────────────────────────
    public static int KpiMessagesToday =>
        SentBroadcasts
            .Where(b => b.CreatedAt >= DateTime.Today.AddDays(-1))
            .Sum(b => b.Delivered);

    public static int KpiFailed        => FailedBroadcasts.Sum(b => b.FailedCount);
    public static int KpiActiveProfiles => 5;
    public static int KpiDrafts        => Drafts.Count;

    // ── Unapproved broadcasts ──────────────────────────────────────────────────
    // Separate from Broadcasts list — awaiting approver sign-off before dispatch.
    private static readonly IReadOnlyList<BroadcastMock> _unapproved =
    [
        new(26, "Kritisk varsling: Vandforsyning afbrudt d. 8. april",  "Unapproved", 3247, 0, 0, DaysAgo(0,  7, 30), null, "TechSupport A/S"),
        new(27, "Markedsføring Q2 2026 – klar til godkendelse",          "Unapproved", 1843, 0, 0, DaysAgo(0,  9, 15), null, "RetailPlus"),
        new(28, "Sæsonkampagne: Maj 2026 – klar til godkendelse",        "Unapproved",  542, 0, 0, DaysAgo(0, 10, 45), null, "FitnessChain DK"),
    ];

    public static IReadOnlyList<BroadcastMock> UnapprovedBroadcasts => _unapproved;

    // ── Scenarios ─────────────────────────────────────────────────────────────
    public static readonly IReadOnlyList<ScenarioMock> Scenarios =
    [
        new(1, "Kvartalsinformation",   "SMS + e-mail til alle profiler hvert kvartal",         "VetClinic Aarhus",  7),
        new(2, "Driftsbesked",          "Planlagt vedligeholdelse. Standardtekst klar.",         "TechSupport A/S",  14),
        new(3, "Flash-tilbud weekend",  "Tilbud sendes lørdag kl. 07:00. Adresser gemt.",       "RetailPlus",        2),
        new(4, "Nyt produkt-launch",    "Produktlancering med link til landingpage.",            "RetailPlus",       21),
        new(5, "Aftale-reminder",       "Automatisk SMS-reminder 24 timer før aftale.",         "DentalGroup Nord",  1),
    ];

    // ── Standard receiver groups ───────────────────────────────────────────────
    public static readonly IReadOnlyList<StdReceiverGroupMock> StdReceivers =
    [
        new(1, "Alle aktive kunder",   12482, "VetClinic Aarhus"),
        new(2, "Nyhedsbrevsmodtagere", 8341,  "RetailPlus"),
        new(3, "Premium-kunder",       2103,  "RetailPlus"),
        new(4, "Drifts-kontakter",     428,   "TechSupport A/S"),
        new(5, "Tandlæge-patienter",   1872,  "DentalGroup Nord"),
        new(6, "Fitness-abonnenter",   4291,  "FitnessChain DK"),
    ];

    // ── Super-admin context data ───────────────────────────────────────────────
    public static readonly IReadOnlyList<CountryMock> Countries =
    [
        new(1, "Danmark",  "DK"),
        new(2, "Norge",    "NO"),
        new(3, "Sverige",  "SE"),
        new(4, "Finland",  "FI"),
    ];

    public static readonly IReadOnlyList<CustomerContextMock> Customers =
    [
        new(1,  "RetailPlus A/S",          1,  24, 6,  DaysAgo(0,  9, 0)),
        new(2,  "TechSupport A/S",         1,  11, 3,  DaysAgo(0, 14, 0)),
        new(3,  "FitnessChain DK",         1,  18, 4,  DaysAgo(1,  8, 0)),
        new(4,  "DentalGroup Nord",        1,   7, 2,  DaysAgo(2, 10, 0)),
        new(5,  "VetClinic Aarhus",        1,   9, 3,  DaysAgo(0, 11, 0)),
        new(6,  "NorgesVarsel AS",         2,  14, 5,  DaysAgo(0, 13, 0)),
        new(7,  "Oslo Handel AS",          2,   8, 2,  DaysAgo(3,  9, 0)),
        new(8,  "Nordic Fitness AB",       3,  12, 3,  DaysAgo(1, 15, 0)),
        new(9,  "Stockholm Tandvård AB",   3,   6, 2,  DaysAgo(4, 11, 0)),
        new(10, "Helsinki Services Oy",    4,   5, 2,  DaysAgo(5,  9, 0)),
    ];

    public static readonly IReadOnlyList<ProfileContextMock> Profiles =
    [
        new(1,  "Retail DK øst",       1), new(2,  "Retail DK vest",      1),
        new(3,  "Retail online",        1), new(4,  "Tech support tier-1", 2),
        new(5,  "Tech support tier-2",  2), new(6,  "Tech enterprise",     2),
        new(7,  "Fitness Kbh",          3), new(8,  "Fitness Aarhus",      3),
        new(9,  "Fitness nationwide",   3), new(10, "Dental primær",       4),
        new(11, "Dental specialist",    4), new(12, "VetClinic primær",    5),
        new(13, "VetClinic mobil",      5), new(14, "VetClinic ekspert",   5),
        new(15, "NorgesVarsel nord",    6), new(16, "NorgesVarsel sør",    6),
        new(17, "Oslo butikker",        7), new(18, "Oslo online",         7),
    ];

    // ── Super-admin helpers ───────────────────────────────────────────────────
    public static IReadOnlyList<CustomerContextMock> GetCustomersForCountry(int countryId) =>
        Customers.Where(c => c.CountryId == countryId).ToList();

    public static IReadOnlyList<ProfileContextMock> GetProfilesForCustomer(int customerId) =>
        Profiles.Where(p => p.CustomerId == customerId).ToList();

    // ── Broadcast helpers ─────────────────────────────────────────────────────
    public static BroadcastMock? GetBroadcast(int id) =>
        Broadcasts.FirstOrDefault(b => b.Id == id) ?? _unapproved.FirstOrDefault(b => b.Id == id);

    public static IReadOnlyList<BroadcastMock> AllBroadcasts =>
        Broadcasts.Concat(_unapproved).ToList();

    public static IReadOnlyList<BroadcastMock> FilterBroadcasts(DateTime? from, DateTime? to, string? search)
    {
        var q = AllBroadcasts.AsEnumerable();
        if (from.HasValue)  q = q.Where(b => b.CreatedAt >= from.Value);
        if (to.HasValue)    q = q.Where(b => b.CreatedAt <  to.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(b => b.Subject.Contains(search, StringComparison.OrdinalIgnoreCase)
                           || b.Profile.Contains(search, StringComparison.OrdinalIgnoreCase));
        return q.OrderByDescending(b => b.CreatedAt).ToList();
    }

    // ── Send method helpers ───────────────────────────────────────────────────
    private static readonly string[] _sendMethodKeys =
        ["by-address", "by-excel", "by-map", "by-level", "std-receivers", "by-municipality"];

    public static string GetSendMethodKey(int broadcastId) =>
        _sendMethodKeys[broadcastId % _sendMethodKeys.Length];

    public static string GetSendMethodLabel(string key) => key switch
    {
        "by-address"      => "Adresser",
        "by-excel"        => "Excel-upload",
        "by-map"          => "Kort",
        "by-level"        => "Niveau",
        "std-receivers"   => "Standard-modtagere",
        "by-municipality" => "Kommune",
        _                 => key,
    };

    // ── Mock status-rapport (delivery-level rows for a broadcast) ─────────────
    public static IReadOnlyList<StatusReportItemMock> GetStatusReport(int broadcastId)
    {
        var rng = new Random(broadcastId);
        var names   = new[] { "Lars Jensen", "Mette Nielsen", "Søren Andersen", "Anne Christensen", "Peter Hansen", "Lene Pedersen", "Niels Thomsen", "Maria Rasmussen", "Henrik Larsen", "Sofie Jørgensen" };
        var phones  = new[] { "+4522334455", "+4533445566", "+4544556677", "+4555667788", "+4566778899", "+4577889900", "+4588990011", "+4599001122", "+4511223344", "+4512345678" };
        return Enumerable.Range(0, 8).Select(i =>
        {
            var status = rng.Next(10) < 8 ? "Leveret" : rng.Next(2) == 0 ? "Fejlet" : "Afvist";
            return new StatusReportItemMock(
                names[i % names.Length],
                phones[i % phones.Length],
                "SMS",
                status,
                status == "Leveret"
                    ? (DateTime?)DaysAgo(0, 8, 0).AddMinutes(rng.Next(60))
                    : null);
        }).ToList();
    }
}
