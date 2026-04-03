# Governance — SSOT

**Last Updated:** 2026-04-03

---

## Index

| File | Type | Topic |
|------|------|-------|
| [MASTER_BUILD_PLAN.md](MASTER_BUILD_PLAN.md) | plan | Phases, ordering, dependencies for all system work |
| [RED_THREAD_REGISTRY.md](RED_THREAD_REGISTRY.md) | registry | Core invariants — ALL code must bind to these |
| [SSOT_GAP_PLAN.md](SSOT_GAP_PLAN.md) | backlog | Missing SSOT files, priority, what they block |
| [EXECUTION_PROTOCOL.md](EXECUTION_PROTOCOL.md) | protocol | Autonomy engine — how Copilot must work |
| [ENFORCEMENT_PROTOCOL.md](ENFORCEMENT_PROTOCOL.md) | protocol | HARD STOP conditions, pre/runtime/post gates |
| [FIRST_VERTICAL_SLICE.md](FIRST_VERTICAL_SLICE.md) | plan | login → frontpage → customer-admin execution plan |
| [GOVERNANCE_MAP.md](GOVERNANCE_MAP.md) | map | Canonical vs deprecated sources — conflict resolution priority |
| [SELF_OPTIMIZATION_ENGINE.md](SELF_OPTIMIZATION_ENGINE.md) | meta | EXECUTE→VALIDATE→LEARN→STANDARDIZE loop |
| [EXECUTION_MEMORY.md](EXECUTION_MEMORY.md) | log | Completed task log — append after every task |
| [PATTERN_EXTRACTION.md](PATTERN_EXTRACTION.md) | protocol | When/how to extract repeated logic into SSOT |
| [ERROR_DETECTION.md](ERROR_DETECTION.md) | protocol | 6 signals + 3 audit rules with fix actions |
| [AUTO_IMPROVEMENT.md](AUTO_IMPROVEMENT.md) | protocol | Promote discovered improvements into SSOT |
| [ANTI_PATTERN_REGISTRY.md](ANTI_PATTERN_REGISTRY.md) | registry | Confirmed anti-patterns — APR_001–APR_008 |
| [ai-boundaries.md](ai-boundaries.md) | rule | What AI may/may not do autonomously *(pending)* |
| [ssot-update-protocol.md](ssot-update-protocol.md) | rule | When SSOT must be updated *(pending)* |
| [code-review-checklist.md](code-review-checklist.md) | checklist | Pre-merge validation *(pending)* |

---

## Core Rule

```
IF NOT IN SSOT → DOES NOT EXIST
IF EXISTS IN SSOT → MUST BE FOLLOWED
```

---

## Enforcement Entry Point

```
AI_WORK_CONTRACT.md → EXECUTION_PROTOCOL.md → RED_THREAD_REGISTRY.md → [ssot_source]
```
