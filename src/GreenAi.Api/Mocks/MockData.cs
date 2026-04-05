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
}
