namespace GreenAi.E2E.Governance;

/// <summary>
/// Calculates a weighted governance score from a list of rule results.
///
/// Weights: critical = 50, major = 15, minor = 5.
/// Score = round(earnedScore / maxScore × 100).
/// Returns 100 when the results list is empty (vacuously passing).
/// </summary>
public static class GovernanceScorer
{
    public static (int Score, int Critical, int Major, int Minor) Score(
        List<GovernanceRuleResult> results)
    {
        int maxScore    = 0;
        int earnedScore = 0;
        int critical    = 0;
        int major       = 0;
        int minor       = 0;

        foreach (var r in results)
        {
            int weight = r.Severity switch
            {
                "critical" => 50,
                "major"    => 15,
                "minor"    => 5,
                _          => 0,
            };

            maxScore += weight;

            if (r.Passed)
            {
                earnedScore += weight;
            }
            else
            {
                if (r.Severity == "critical") critical++;
                if (r.Severity == "major")    major++;
                if (r.Severity == "minor")    minor++;
            }
        }

        var score = maxScore == 0 ? 100 : (int)Math.Round((double)earnedScore / maxScore * 100);
        return (score, critical, major, minor);
    }
}
