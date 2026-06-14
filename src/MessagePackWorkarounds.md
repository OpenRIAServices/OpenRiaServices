# Serialization Of Class hierarchies does not work

Consider the following class hierarchy.
Where A is annotated with `KnownTypeAttribute` for `B` and `C`.
```
A -> B -> C
```

A serializer for A can properly serialize and deserialize all 3 classes.
A serializer for B does not work for objects of type `C`.

Desired behaviour:
 * Serializer for `A` can serialize/deserialize all 3 types (with a single discriminator)
 * Serializer for `B` can serialize/deserialize B and C
 * Can serialize/deserialize objects of known type based on "object" serializer
 * Should work with existing code gen (basetype has all "known type" attribytes)
 * No usage of surrogates/marshalling types

Problems:
* Using DerivedShapeMapping have the following issues
   * Serialization of B using shape A suddenly use 2 discriminators `["B", ["B", ...] ]`
   * Serialization of B`s derived classses does not pick the most specific instance , a bug??
      * There is some sorting of DerivedShapeMapping based on inheritance,
        but it can still pick the base instance instead of the actual type.
        This is especially bad when B is abstract.
        See `AI_Detail` class where derived types were serialized as `AI_Detail` insted of actual type.
* Cannot add custom converter for types using built in 

Workarounds:
* Added a `ObjectConverter` class on client
  * The `ObjectConverter` class has a custom "ReflectionTypeShapeProvider" with special assemblies specified
    Also "unwrapping" of `IUnionTypeShape<T>` is needed to get the underlying "object serializer"
     * This allows caching of the "ObjectSerializers"
* Added `SurrogateConverter` class on server
   * Create dynamic classes/instances with same name and properties (without KnownTypeAttributes)
* To get Queries, Invoke and method parameters to work
  *  See test : Inherit_Run_CUD_Delete_Derived which failes since abstract class cannot be deserialized
      *  Maybe always serialize/deserialize using "base type" serializer ??
           * MethodParametersConverter (Client)
           * ChangeSetEntryConverter (Client) (entity actions)
           * MessagePackHttpDomainClient.GetResponseEnvelopeType*  (client)
           * MethodParametersConverter (server)
           * MessagePackRequestSerializer.WriteQueryResponseAsync (server)
           * MessagePackRequestSerializer.CreateInvokeResponseEnvelope (server)

# Serializing using

# Object and Union converter for same type cannot coexist

Problems
* Cannot get "object" converter for a specific Type, the converter is "always" UnionTypeShape
* Converter Cache does dot distinguish betweeen `IUniontTypeShape<T>` and `IObjectTypeShape<T>`

# Other bugs

PolyType does not handle TestDomainServices.MockReport correctly, it generate a constructor takeing 6 arguments meaning
serialization callbacks will note be executed.

Resulting in failed test

* Addin a ctor with required parameters still does not resolve the issue.
* adding required parameters to ctor leads to duplicate parameters
