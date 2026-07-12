## Problem

Describe the user or developer problem addressed by this pull request.

## Approach

Explain the implementation and important trade-offs.

## Validation

Describe automated tests, manual checks, and platforms used.

## Contract and compatibility impact

State whether this changes plugin contracts, port schemas, project-file schemas, Fast Stream behavior, JSON Message behavior, or localization resources. If none, write `None`.

## Checklist

- [ ] The solution builds with warnings treated as errors.
- [ ] Existing tests pass and new behavior has tests.
- [ ] Fast Stream hot paths do not introduce CSV/JSON conversion or avoidable per-sample allocation.
- [ ] UI-facing text is localization-ready.
- [ ] Documentation and ADRs are updated where necessary.
- [ ] No secrets or real personal, biometric, health, or credential data are included.
- [ ] Public API and persistence changes include a compatibility note.
