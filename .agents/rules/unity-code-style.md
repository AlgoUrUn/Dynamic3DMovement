---
trigger: always_on
---

# Unity C# Code Style

Apply these conventions to new or edited C# code in this Unity project. Match nearby code when it is more specific.

## Naming
- Classes, structs, enums, methods, properties, public fields, and constants: `PascalCase`
- Interfaces: `IPascalCase`
- Private and protected fields: `_camelCase`
- Local variables and parameters: `camelCase`
- Serialized private fields: `[SerializeField] private Type _fieldName;`

## Syntax And Formatting
- Use Allman braces.
- Write explicit access modifiers; do not rely on implicit `private`.
- Use `var` only when the type is obvious from the right-hand side, such as `new TypeName(...)`.
- Prefer `if` / `else if` / `else` over `switch` to match the project convention.
- Keep methods short and extract helpers when a block becomes difficult to scan.

## Unity Practices
- Remove unused empty Unity lifecycle methods such as `Start` or `Update`.
- Cache repeated component lookups in `Awake` or `Start`; do not call `GetComponent<T>()` every frame.
- For UnityEngine.Object references, prefer explicit `!= null` checks over null-conditional operators.
- Keep runtime code free of `UnityEditor` references unless guarded by `#if UNITY_EDITOR`.
- Prefer direct references for required scene/prefab dependencies; use `Resources.Load` only for truly dynamic loading.

## Class Layout
1. Constants and public fields
2. `[SerializeField] private` fields
3. Private and protected fields
4. Properties
5. Unity lifecycle methods (`Awake`, `Start`, `Update`, etc.)
6. Public methods
7. Private and protected methods

## Comments
- Write comments in English.
- Prefer clear names over comments.
- Comment why code exists or why a non-obvious approach is required, not what each line does.
- Use XML documentation (`///`) for public APIs when the behavior is not obvious from the signature.
