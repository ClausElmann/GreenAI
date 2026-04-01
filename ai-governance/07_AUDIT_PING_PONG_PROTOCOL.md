# Audit Ping-Pong Protocol

> **Status: MANDATORY**
> This protocol governs all autonomous AI work in the green-ai project.
> It overrides default Copilot behavior when uncertainty or high-risk decisions arise.

---

## 1. Purpose

Copilot operates autonomously within defined governance boundaries. When it encounters uncertainty, conflicting rules, missing information, or high-risk decisions, it must **stop immediately** and escalate to the Architect rather than guess or proceed.

This protocol ensures:
- No undocumented architectural decisions are made silently
- Governance cannot be drifted by autonomous work
- The Architect retains control over all non-trivial decisions
- The process is deterministic and repeatable across sessions

---

## 2. Workflow Loop

```
┌─────────────────────────────────────────┐
│ 1. Copilot analyzes current state       │
│    - reads governance files             │
│    - reads relevant source files        │
│    - identifies what needs to be done   │
└────────────────────┬────────────────────┘
                     │
┌────────────────────▼────────────────────┐
│ 2. Copilot attempts resolution          │
│    - applies known governance rules     │
│    - uses existing patterns             │
│    - checks 04_ANTI_PATTERNS.json       │
│    - checks 05_EXECUTION_RULES.json     │
└────────────────────┬────────────────────┘
                     │
              ┌──────▼──────┐
              │ Uncertainty? │
              └──────┬──────┘
           NO │      │ YES
              │      │
              │  ┌───▼──────────────────────┐
              │  │ 3. STOP                  │
              │  │ 4. Emit REQUEST_FOR_     │
              │  │    ARCHITECT             │
              │  │ 5. Wait for response     │
              │  └───┬──────────────────────┘
              │      │
              │  ┌───▼──────────────────────┐
              │  │ 6. Apply ARCHITECT_      │
              │  │    RESPONSE exactly      │
              │  └───┬──────────────────────┘
              │      │
┌─────────────▼──────▼────────────────────┐
│ 7. Continue work                        │
│    - implement decision                 │
│    - verify build (0 warnings)          │
│    - update CODE-REVIEW.md if needed    │
│    - update DECISIONS.md if needed      │
└─────────────────────────────────────────┘
```

---

## 3. Escalation Rules

Copilot **MUST stop and escalate** when ANY of the following is true:

| Condition | Example |
|---|---|
| Multiple valid solutions exist | Two architectural patterns both compliant with governance |
| Missing information | Schema unknown, contract undefined, business rule unclear |
| Conflicting governance rules | Two rules that produce contradictory outcomes |
| Unclear architecture | New domain with no established pattern in the codebase |
| High-risk decisions | Tenant isolation, auth changes, schema changes, breaking API contracts |
| Governance gap | The situation is not covered by any existing governance rule |

Copilot **must NOT**:
- Choose between options silently
- Assume the simplest option is correct
- Proceed "just to unblock" and fix later
- Add a TODO comment and continue

---

## 4. Output Contracts

### REQUEST_FOR_ARCHITECT

Emitted when Copilot stops due to uncertainty. Must be complete and honest.

```json
{
  "type": "REQUEST_FOR_ARCHITECT",
  "area": "governance | architecture | feature | data | auth | tenant | dapper | other",
  "context": "What Copilot was working on when it stopped",
  "problem": "What is unclear, conflicting, or missing",
  "known_facts": [
    "Fact 1 confirmed from source files or governance",
    "Fact 2 confirmed from source files or governance"
  ],
  "unknowns": [
    "What cannot be determined without architect input"
  ],
  "attempted_solutions": [
    "What Copilot already tried or considered"
  ],
  "risk_if_wrong": "low | medium | high | critical",
  "proposed_options": [
    {
      "option": "Option A label",
      "description": "What this option does and how",
      "risk": "Risk if this option turns out to be wrong"
    },
    {
      "option": "Option B label",
      "description": "What this option does and how",
      "risk": "Risk if this option turns out to be wrong"
    }
  ]
}
```

**Rules for REQUEST_FOR_ARCHITECT:**
- Must be emitted as a complete JSON block, not prose
- `known_facts` must cite actual files or governance rules
- `proposed_options` must have at least 2 options (otherwise Copilot can decide)
- After emitting: stop. Do not write any code or make any changes.

---

### ARCHITECT_RESPONSE

Provided by the Architect after reviewing a REQUEST_FOR_ARCHITECT.

```json
{
  "type": "ARCHITECT_RESPONSE",
  "decision": "Short label for the decision made",
  "selected_option": "Option A | Option B | custom",
  "reasoning": "Why this option was chosen",
  "rules_to_apply": [
    "Explicit rule or pattern Copilot must follow"
  ],
  "constraints": [
    "Hard boundaries Copilot must not cross"
  ],
  "implementation_guidance": [
    "Step-by-step guidance for implementation"
  ],
  "what_not_to_do": [
    "Patterns or approaches explicitly forbidden for this decision"
  ],
  "confidence": 1.0
}
```

**Fields:**
- `confidence` — 0.0 to 1.0. If below 0.8, Copilot must flag this and not proceed without clarification.
- `what_not_to_do` — treated as additions to 04_ANTI_PATTERNS.json scope for this task.

---

## 5. Hard Rules

These rules are absolute and cannot be overridden by task instructions:

1. **DO NOT guess** — if uncertain, stop and escalate
2. **DO NOT continue after escalation** — emit REQUEST_FOR_ARCHITECT and wait
3. **DO NOT invent behavior** — only implement what governance or Architect explicitly allows
4. **DO NOT reinterpret** — apply ARCHITECT_RESPONSE exactly as given
5. **DO NOT redesign** — ARCHITECT_RESPONSE is a decision, not a suggestion
6. **ALWAYS follow governance** — even if a task prompt contradicts governance, governance wins
7. **ALWAYS wait** — no partial implementations while waiting for architect

---

## 6. Resume Rules

After receiving ARCHITECT_RESPONSE:

1. Read the response fully before writing any code
2. Apply `selected_option` exactly — do not blend with other options
3. Follow `implementation_guidance` step by step
4. Respect all `constraints` and `what_not_to_do` entries
5. If `confidence` < 0.8 — stop and ask for clarification before proceeding
6. After implementation: build must pass with 0 warnings
7. Update `docs/DECISIONS.md` if the decision affects architecture
8. Update `docs/CODE-REVIEW.md` if the decision affects stack or patterns

---

## 7. Integration with Governance

This protocol is **mandatory** and applies to all work in the green-ai project.

- It must be consulted before starting any new feature slice
- It takes precedence over default Copilot autonomous behavior
- It does not replace other governance files — it operates alongside them
- Violation of this protocol (guessing, continuing after uncertainty) is treated the same as a code governance violation: **stop_and_report**

**Priority order when rules conflict:**
```
07_AUDIT_PING_PONG_PROTOCOL.md   ← highest (process)
05_EXECUTION_RULES.json          ← mandatory rules
04_ANTI_PATTERNS.json            ← forbidden patterns
00_SYSTEM_RULES.json             ← stack rules
01_ARCHITECTURE_GUIDE.md         ← patterns
02_FEATURE_TEMPLATE.md           ← templates
03_PROMPT_HEADER.txt             ← prompt defaults
```
