# validator-pattern

```yaml
id: validator_pattern
type: pattern
version: 1.0.0
last_updated: 2026-04-03
ssot_source: docs/SSOT/backend/patterns/validator-pattern.md
red_thread: result_pattern → docs/SSOT/governance/RED_THREAD_REGISTRY.md

purpose: >
  FluentValidation validators are invoked automatically via ValidationBehavior pipeline.
  Validation failures return Result<T>.Fail("VALIDATION_ERROR", ...) — never throw.
  No manual .Validate() calls are needed in handlers.

pipeline_position:
  order:
    1: LoggingBehavior
    2: AuthorizationBehavior
    3: RequireProfileBehavior
    4: ValidationBehavior       ← runs AbstractValidator<TCommand> here
  behavior_file: src/GreenAi.Api/SharedKernel/Pipeline/ValidationBehavior.cs
  on_failure: return Result<TResponse>.Fail("VALIDATION_ERROR", "combined error messages")
  on_no_validators: pass-through (no error)

contracts:

  - name: AbstractValidator<TCommand>
    file_pattern: Features/[Domain]/[Feature]/[Feature]Validator.cs
    shape: |
      public sealed class [Feature]Validator : AbstractValidator<[Feature]Command>
      {
          public [Feature]Validator()
          {
              RuleFor(x => x.Field)
                  .NotEmpty()
                  .MaximumLength(N);
          }
      }
    registration: auto-discovered — no manual DI registration needed
    rule: validator class MUST be in same namespace as its command

  - name: common_rules
    rules:
      string_required:  .NotEmpty().MaximumLength(256)
      email:            .NotEmpty().EmailAddress().MaximumLength(256)
      password:         .NotEmpty().MinimumLength(6).MaximumLength(512)
      password_confirm: .Equal(x => x.NewPassword).WithMessage("Passwords do not match.")
      positive_id:      .GreaterThan(0)

golden_sample: src/GreenAi.Api/Features/Auth/Login/LoginValidator.cs

rules:
  MUST:
    - One validator class per Command
    - File: [Feature]Validator.cs in same folder as [Feature]Command.cs
    - Extends AbstractValidator<TCommand>
    - sealed class
    - Validation rules defined in constructor only
    - Error code on validation failure is always VALIDATION_ERROR (set by pipeline — not by validator)
  MUST_NOT:
    - Manually call .Validate() in handlers — pipeline handles this
    - Throw exceptions in validators
    - Validate internal business rules (wrong credentials, tenant access) — those go in handlers as Result.Fail

anti_patterns:

  - detect: handler calls _validator.Validate(command) manually
    why_wrong: double validation — ValidationBehavior already runs it
    fix: remove manual call — pipeline runs it automatically

  - detect: validator checks ICurrentUser.CustomerId or queries DB
    why_wrong: validators run before authorization — no identity context yet
    fix: move DB/identity checks to handler as Result.Fail guard

  - detect: validator returns false/null instead of using FluentValidation API
    why_wrong: pipeline cannot collect and aggregate errors this way
    fix: RuleFor(...).Must(...).WithMessage("...")

enforcement:

  - where: Features/**/Validator.cs
    how: class name must match [Feature]Validator pattern

  - where: Features/**/Handler.cs
    how: handler must NOT contain IValidator<T> constructor parameter
```
