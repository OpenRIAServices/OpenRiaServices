# Simple Structs Specification

## Feature description

Simple structs are user-defined value types that OpenRiaServices can treat similarly to predefined primitive/simple types in DomainService operation signatures and key validation.

The primary use case is strongly typed keys (for example, a struct wrapping a single primitive value) while preserving existing behavior for complex types.

## Type requirements

A type is considered a supported simple struct when it meets all of the following:

- It is a `struct` (value type), not an enum.
- It is public/visible.
- It is non-generic.
- It has one or more public readable instance properties (non-indexers).
- Every such property type is a predefined simple type supported by OpenRiaServices.

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
- Supported collections and dictionaries that use supported simple structs as element/generic argument types

### Key validation

Entity key members may use supported simple structs when key-specific requirements are met (`IEquatable<T>` + shared-type-only in phase 1).

### Validation

Validation attributes applied directly to an operation parameter or entity property are evaluated normally. Validation attributes on properties inside a simple struct are not recursively evaluated for operation parameters or entity properties.

### Entity properties

An entity property may be a supported simple struct, including a key member that meets the additional key requirements.

Collections and dictionaries of simple structs are supported in operation signatures, but are not currently supported as entity properties.

### Out of scope

- OData hosting support
- Collections and dictionaries of simple structs as entity properties

## Serialization and conversion behavior

No new Parse/TryParse-based mechanism is required for phase 1.
Existing query-string conversion behavior remains in effect, including existing JSON fallback behavior in WebHttp conversion flows.

Simple structs must be compatible with the configured transport serializer. For the default DataContract serialization, annotate the struct and its writable properties with `[DataContract]` and `[DataMember]`:

```csharp
[DataContract]
public struct CustomerKey : IEquatable<CustomerKey>
{
    public CustomerKey(int value) => Value = value;

    [DataMember]
    public int Value { get; set; }

    // IEquatable<CustomerKey> implementation omitted
}
```

## Planned work

### Phase 2: client generation support

- Add generation of non-shared simple structs to client proxy code generation.
  - Validate none of its public properties may be marked with `[Exclude]`.
  - Review type discovery and handling to be more similar to ComplexType handling (including validation of simple types).
- Support for entity properties that are collections of simple structs.
  - When collections of simple structs are added to entities, client collections will need to be readonly or observable. An observable collection may be exposed by a getter-only property, but mutations must mark the entity as modified.
- Expand test coverage for generated (non-shared) simple struct scenarios.
- Add handling for structs that do not implement "=="
  * Gnerate comparisons via EqualityComparer<T>.Default or require and validate the "==" operator is present on the struct.
- Review serialization comment
  - "The newly accepted shape is not guaranteed to survive the existing transport serializers. In particular, the representative readonly struct types in this PR expose only getter-only properties; DataContractJsonSerializer/DataContractSerializer do not serialize ordinary getter-only properties, so these values round-trip as defaults even though this method accepts them. Please either add transport support that reconstructs these immutable structs (and exercise an actual request/response round trip) or restrict the predicate to shapes the active serializers can round-trip."
