# Simple Structs Specification

## Feature description

Simple structs are user-defined value types that OpenRiaServices can treat similarly to predefined primitive/simple types in DomainService operation signatures and key validation.

The primary use case is strongly typed keys (for example, a struct wrapping a single primitive value) while preserving existing behavior for complex types.

## Type requirements

A type is considered a supported simple struct when it meets all of the following:

- It is a `struct` (value type), not an enum.
- It is public/visible.
- It is non-generic.
- It is not a framework/system assembly type.
- It has exactly one public readable instance property (non-indexer).
- That property type is a predefined simple type supported by OpenRiaServices.

Additional requirements when used as an entity key member:

- The struct must implement `IEquatable<T>`.
- In phase 1, the struct must be shared/available on the client (shared-type-only support).

## Current support

### In scope

- ASP.NET Core hosting (primary target).
- WCF hosting paths where the existing converters and serialization behavior apply.

### Operation signatures

Supported simple structs are accepted where predefined types are accepted in:

- Query method parameters
- Custom method scalar parameters
- Invoke method parameters and return values

### Key validation

Entity key members may use supported simple structs when key-specific requirements are met (`IEquatable<T>` + shared-type-only in phase 1).

### Out of scope

- OData hosting support

## Serialization and conversion behavior

No new Parse/TryParse-based mechanism is required for phase 1.
Existing query-string conversion behavior remains in effect, including existing JSON fallback behavior in WebHttp conversion flows.

## Planned work

### Phase 2: client generation support

- Add generation of non-shared simple structs to client proxy code generation.
- Keep compatibility with existing complex type generation and behavior.
- Expand test coverage for generated (non-shared) simple struct scenarios.
