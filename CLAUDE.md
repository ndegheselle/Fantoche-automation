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

> When a rule hurts readability in a specific case, readability wins. Explain the exception in a comment.
