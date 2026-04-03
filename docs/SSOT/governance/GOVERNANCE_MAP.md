# GOVERNANCE_MAP

```yaml
id: governance_map
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/GOVERNANCE_MAP.md

canonical_authority: docs/SSOT/governance/
rule: IF CONFLICT BETWEEN SOURCES → docs/SSOT/governance/ WINS.

# ---------------------------------------------------------------------------
# CANONICAL FILES (single source of truth)
# ---------------------------------------------------------------------------

canonical:

  - path: docs/SSOT/governance/RED_THREAD_REGISTRY.md
    type: registry
    content: all system invariants — binding on all code

  - path: docs/SSOT/governance/EXECUTION_PROTOCOL.md
    type: protocol
    content: 7-step execution loop + 10 forbidden actions

  - path: docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md
    type: protocol
    content: pre/runtime/post gates + STOP conditions

  - path: docs/SSOT/governance/SSOT_GAP_PLAN.md
    type: backlog
    content: missing SSOT files ranked by sprint priority

  - path: docs/SSOT/governance/MASTER_BUILD_PLAN.md
    type: plan
    content: phases + dependencies + blocking relationships

  - path: docs/SSOT/governance/FIRST_VERTICAL_SLICE.md
    type: plan
    content: login → frontpage → customer-admin slice contracts + done_when

  - path: docs/SSOT/governance/SELF_OPTIMIZATION_ENGINE.md
    type: meta
    content: EXECUTE→VALIDATE→LEARN→STANDARDIZE loop

  - path: docs/SSOT/governance/EXECUTION_MEMORY.md
    type: log
    content: completed task log — append after every task

  - path: docs/SSOT/governance/PATTERN_EXTRACTION.md
    type: protocol
    content: when/how to extract repeated logic into SSOT

  - path: docs/SSOT/governance/ERROR_DETECTION.md
    type: protocol
    content: 6 signals + 3 audit rules — detect and fix violations

  - path: docs/SSOT/governance/AUTO_IMPROVEMENT.md
    type: protocol
    content: promote discovered improvements into SSOT

  - path: AI_WORK_CONTRACT.md
    type: session-start
    content: trigger table → first tool mapping, absolute rules, tech stack

  - path: .github/copilot-instructions.md
    type: session-start
    content: SESSION START ritual, SSOT-MAP, copy-paste commands

# ---------------------------------------------------------------------------
# UNIQUE FILES — not duplicated, keep as-is
# ---------------------------------------------------------------------------

unique_keep:

  - path: ai-governance/12_DECISION_REGISTRY.json
    reason: locked architectural decisions with dates and rationale — unique, not a pattern
    content: IDENTITY_MODEL, LANGUAGE_PLACEMENT, ENDPOINT_STRUCTURE, TEST_DATABASE (locked=true)
    rule: decisions with locked=true are immutable without explicit user instruction

  - path: ai-governance/11_EXECUTION_STATE.json
    reason: live execution state tracker (current_step, step_log) — operational, not governance

  - path: ai-governance/03_PROMPT_HEADER.txt
    reason: used by analysis-tool pipeline as prompt prefix — not governance

  - path: ai-governance/07_AUDIT_PING_PONG_PROTOCOL.md
    reason: analysis-tool-specific audit protocol — check if still active

# ---------------------------------------------------------------------------
# DEPRECATED FILES — content duplicated in canonical sources
# ---------------------------------------------------------------------------
# ACTION: files below are DEPRECATED — do NOT delete (need user approval)
# INSTEAD: deprecation header added to each file pointing to canonical source
# ---------------------------------------------------------------------------

deprecated:

  - path: ai-governance/00_SYSTEM_RULES.json
    deprecated_by:
      - docs/SSOT/governance/RED_THREAD_REGISTRY.md  (non_negotiables, tenant_rules)
      - AI_WORK_CONTRACT.md (tech stack)
    action: add deprecation header — pending user approval to delete
    overlap_fields: [runtime, architecture_style, non_negotiables, tenant_rules]

  - path: ai-governance/01_ARCHITECTURE_GUIDE.md
    deprecated_by:
      - docs/SSOT/backend/README.md
      - docs/SSOT/backend/patterns/handler-pattern.md
      - docs/SSOT/backend/patterns/endpoint-pattern.md
    action: add deprecation header
    overlap_sections: [Folder Structure, Data Access, Authentication, Multi-Tenancy]

  - path: ai-governance/04_ANTI_PATTERNS.json
    deprecated_by:
      - docs/SSOT/governance/RED_THREAD_REGISTRY.md (violation actions)
      - docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md (forbidden list)
      - individual SSOT pattern files (anti_patterns sections)
    action: add deprecation header
    unique_content: none detected — all patterns covered by RED_THREAD_REGISTRY

  - path: ai-governance/05_EXECUTION_RULES.json
    deprecated_by:
      - docs/SSOT/governance/EXECUTION_PROTOCOL.md
      - docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md
    action: add deprecation header
    unique_content:
      - tools_register_first rule → add to EXECUTION_PROTOCOL.md (pending)
      - powershell_path_rule → already in user memory

  - path: ai-governance/06_BACKEND_WORKFLOW_RULES.json
    deprecated_by:
      - docs/SSOT/governance/EXECUTION_PROTOCOL.md
      - docs/SSOT/backend/README.md
    action: add deprecation header

  - path: ai-governance/02_FEATURE_TEMPLATE.md
    deprecated_by:
      - docs/SSOT/backend/README.md (Feature Folder Structure)
      - docs/SSOT/backend/patterns/handler-pattern.md
    action: check content — may have unique template code, verify before deprecating
    status: PENDING_REVIEW

  - path: ai-governance/13_VALIDATION_RULES.json
    deprecated_by:
      - docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md (pre/runtime/post gates)
      - docs/SSOT/governance/RED_THREAD_REGISTRY.md (invariants)
    action: add deprecation header
    unique_content:
      - icurrentuser_sole_auth_contract → already in docs/SSOT/identity/current-user.md
      - decision_registry_check → keep in ai-governance/12_DECISION_REGISTRY.json

# ---------------------------------------------------------------------------
# ANALYSIS-TOOL ai-governance (separate project)
# ---------------------------------------------------------------------------

analysis_tool_governance:
  canonical: analysis-tool project does not share green-ai's SSOT governance
  rule: analysis-tool/ai-governance/ files govern the analysis-tool pipeline only
  overlap_with_green_ai: 00_SYSTEM_RULES.json, 01_ARCHITECTURE_GUIDE.md, 06_EXECUTION_RULES.json
  action: NO CHANGE — analysis-tool maintains its own governance context
  reason: analysis-tool is a Python project with different stack and tooling

# ---------------------------------------------------------------------------
# RESOLUTION PRIORITY ORDER
# ---------------------------------------------------------------------------

conflict_resolution:
  priority:
    1: docs/SSOT/governance/ENFORCEMENT_PROTOCOL.md (HARD STOP conditions)
    2: docs/SSOT/governance/RED_THREAD_REGISTRY.md  (invariants)
    3: docs/SSOT/governance/EXECUTION_PROTOCOL.md   (execution loop)
    4: AI_WORK_CONTRACT.md                           (trigger table)
    5: area SSOT files (backend/, identity/, testing/ etc.)
    6: ai-governance/*.json (DEPRECATED — only 12_DECISION_REGISTRY.json has authority)
```
