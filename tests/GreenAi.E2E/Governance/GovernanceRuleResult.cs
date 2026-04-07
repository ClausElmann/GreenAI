namespace GreenAi.E2E.Governance;

/// <summary>
/// Result of a single governance rule evaluation.
/// Identity fields (RuleKey, RuleId, RuleName, Severity) are init-only.
/// Result fields (Passed, Message, Elements, Selector, SelectorType, ExecutionMs) are mutable.
/// </summary>
public sealed record GovernanceRuleResult
{
    public required string RuleKey     { get; init; }
    public required string RuleId      { get; init; }
    public required string RuleName    { get; init; }
    public required string Severity    { get; init; }  // "critical" | "major" | "minor"
    public bool            Passed      { get; set; } = true;
    public string?         Message     { get; set; }
    public List<string>    Elements    { get; set; } = [];
    public string?         Selector    { get; set; }
    public string?         SelectorType { get; set; }
    public int             ExecutionMs { get; set; }
}
