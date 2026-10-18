# Coding guidelines

Goal: minimize the mental overhead needed to read, change and trust the code.

## Structure

- **Feature first**: group code by feature, not by technical layer. A feature's UI, logic and data access live together.
- **Shallow call paths**: from a feature's entry point, the actual implementation should be 2-3 hops away maximum.
- **Locality**: code that changes together lives together.

## Code

- **Flat control flow**: prefer early returns over nested `if`/loops.
- **No nested calls**: name intermediate results instead of `a(b(c(x)))`.
- **Self-explanatory names**: if a name needs a comment, rename it.
- **Explicit over implicit**: no hidden side effects, no clever tricks.
- **Comments explain why**, not what.

## Scope

- **Every feature has a cost**: code to maintain, test and understand. Before adding or changing one, check that it serves the project's goals.
- **Removing is progress**: delete unused code and features.
- **Challenge decisions**: question whether a new functionality is relevant to the project.

## Tests

- **Test what would hurt**: cover business rules and anything that's hard to spot in production. Skip trivial code like getters, mappings, framework glue and plain UI layout.
- **One reason to fail**: test behavior, not implementation. Each test checks one behavior, and its name describes that behavior (`rejects_order_when_stock_is_empty`).
- **A bug fix comes with a test**: reproduce the bug first, then fix it.
- **Tests have a cost too**: if a test breaks often for no real reason, or guards code that no longer matters, delete it.
- **Mock only the edges**: fake what's slow or external (network, payment), not your own code.
- **Fast and deterministic**: a flaky test is worse than no test. Fix it or delete it.
- **Coverage is not a goal**: use it to find untested critical paths, never as a target to hit.

> When a rule hurts readability in a specific case, readability wins. Explain the exception in a comment.
