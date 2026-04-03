# SELF_OPTIMIZATION_ENGINE

```yaml
id: self_optimization_engine
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/governance/SELF_OPTIMIZATION_ENGINE.md

purpose: Meta-protocol that governs HOW the AI improves its own execution over time.
loop: EXECUTION → VALIDATION → LEARNING → STANDARDIZATION

components:
  - id: execution_memory      → docs/SSOT/governance/EXECUTION_MEMORY.md
  - id: pattern_extraction    → docs/SSOT/governance/PATTERN_EXTRACTION.md
  - id: error_detection       → docs/SSOT/governance/ERROR_DETECTION.md
  - id: auto_improvement      → docs/SSOT/governance/AUTO_IMPROVEMENT.md
  - id: red_thread_registry   → docs/SSOT/governance/RED_THREAD_REGISTRY.md
  - id: execution_protocol    → docs/SSOT/governance/EXECUTION_PROTOCOL.md

optimization_loop:

  - step: 1_execute
    action: run task following EXECUTION_PROTOCOL.md
    input: user_task
    output: code + test result

  - step: 2_validate
    action: check output against bound red_threads
    tool: dotnet build + dotnet test
    signals:
      - build_warnings > 0     → red_thread:zero_warnings violated → fix immediately
      - test_failures > 0      → classify via debug-protocol.md → fix root cause
      - ssot_reference_missing → red_thread:bypass_ssot violated → create SSOT first
      - pattern_repeated_2x    → trigger pattern_extraction (step 3b)

  - step: 3a_learn
    action: append to EXECUTION_MEMORY.md after each completed task
    required_fields: [task, pattern_used, issues, improvements_found]

  - step: 3b_extract
    action: IF same logic appears 2+ times → create pattern SSOT
    trigger: pattern_repeated_2x signal from step 2
    protocol: PATTERN_EXTRACTION.md

  - step: 4_standardize
    action: update ssot_source file with discovered pattern
    rule: improvement becomes SSOT before it is used a 3rd time
    forbidden: using experimental pattern without SSOT

fail_conditions:
  - logic repeated without creating SSOT (2+ occurrences)
  - error code used not in ResultExtensions.cs
  - inline assumption without SSOT reference
  - task completed without EXECUTION_MEMORY entry
  - test failure without root cause classification

success_state:
  - all tasks reference minimum 1 red_thread
  - all patterns have ssot_source
  - EXECUTION_MEMORY.md has entry for every completed task
  - 0 compiler warnings
  - all tests pass
```
