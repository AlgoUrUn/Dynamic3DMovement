---
trigger: always_on
---

# Unity C# Code Style Rules

## 1. Naming
* Class, Struct, Enum, Method, Property, Public Field, Const: `PascalCase`
* Interface: `IPascalCase`
* Private/Protected Field: `_camelCase`
* Local Variable, Parameter: `camelCase`

## 2. Syntax & Formatting
* Braces: Allman style (새 줄에 중괄호 작성).
* Modifier: 모든 접근제어자 명시 (암시적 `private` 허용 안 함).
* `var`: `new` 키워드 등 타입이 명확할 때만 사용.

## 3. Unity Specific
* Inspector: `[SerializeField] private` 사용.
* Lifecycle: 사용하지 않는 빈 Unity 메서드(Start, Update 등) 삭제.
* Caching: `GetComponent<T>()`는 `Awake`/`Start`에서만 사용 (매 프레임 호출 금지).
* Null Check: Unity Object에 대해 `?.` 연산자 지양, 명시적 `!= null` 검사 권장.

## 4. Class Structure Order
1. `public` Fields / `const`
2. `[SerializeField] private` Fields
3. `private` Fields
4. Properties
5. Unity Methods (`Awake`, `Start`, `Update`...)
6. `public` Methods
7. `private` / `protected` Methods

## 5. Comments (주석): '어떻게' 동작하는지가 아니라, '왜' 그렇게 작성했는지를 주석으로 남김. 코드가 스스로 설명되도록 명확한 네이밍을 우선하며, public API나 복잡한 로직에는 XML 주석(`///`)을 사용함