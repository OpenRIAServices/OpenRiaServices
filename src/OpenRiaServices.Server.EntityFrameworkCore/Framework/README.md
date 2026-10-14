[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://vshymanskyy.github.io/StandWithUkraine)


## TERM OF USE

By using this project or its source code, for any purpose and in any shape or form, you grant your **agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **do not support the Russian invasion or contribute to its propaganda'**

This excludes usage by the Russian state, Russian state-owned companies, Russian education who spread propaganda instead of truth, anyone who work with the *filtration camps*, or finance the war by importing Russian oil or gas.

- You allow anonymized telemetry to collected and sent during the preview releases to gather feedback about usage



## Getting Started

1. Ensure you have setup and configured `OpenRiaServices.Hosting.AspNetCore`
2. Add a reference to *OpenRiaServices.Server.EntityFrameworkCore*
    `dotnet add package OpenRiaServices.Server.EntityFrameworkCore`
3. Add one or more domainservices. 
Given that you have a Ef Core model named `MyDbContext` with an entity `MyEntity` you add CRUD methods using something similar to below

```csharp
using using Microsoft.EntityFrameworkCore;
using OpenRiaServices.Server.EntityFrameworkCore;

[EnableClientAccess]
public class MyDomainService : DbDomainService<MyDbContext>
{
    // Not required: But it is generally a good idea to allow dependency injection of DbContext
    public MyDomainService(MyDbContext dbContext)
        : base(context)
    { }

    /* Example of CUD methods */
    public void InsertMyEntity(MyEntity entity)
        => DbContext.Entry(entity).State = EntityState.Added; // Or  DbContext.Add, but it might add related entities differently

    public void UpdateMyEntity(MyEntity entity)
        => base.AttachAsModified(entity); // This sets state to Modified and set modified status on individual properties based on client changes and if `RountTripOriginal` attribute is specified or not

    public void DeleteMyEntity(MyEntity entity)
        => DbContext.Entry(entity).State =  EntityState.Deleted;

    /* Query: 
    * Return IQueryable<> to automatically allow the client to add filtering, paging etc. 
    * The queries are performed async for best performance
    */
    [Query]
    IQueryable<MyEntity> GetMyEntities()
        => DbContext.MyEntities.OrderBy(x => x.Id); // Sort by id to get stable Skip/Take if client does paging
}
```
4. Ensure that `MyDomainService` is mapped, (See [Setup instructions in OpenRiaServices.Hosting.AspNetCore readme](https://www.nuget.org/packages/OpenRiaServices.Hosting.AspNetCore))


## Owned Entities

* *one-to-one* relations using Owned Entities are fully supported
   * Any EF Core configuration/metadata applied to the owned entity (such as Required, MaxLength etc) are part of generated client Code
* *one-to-many* relations might work, but it has not been verified at the moment

### Owned Entities without explicit key
EF Core owned entities are mapped to OpenRiaServices's [Complex Types](https://openriaservices.gitbook.io/openriaservices/ee707348/ee707356/gg602753) as long as the owned entity does not have have an explicit key.


### Owned Entities with explicit key
If an explicit key to an owned entity then they are mapped as a normal entity and the navigation property to the owned entity adds `[Composition]` 

This makes the owned entity available during Insert, Update and Delete operations and prevents if from accidentaly having all fields set to `null`.

## Complex Types

The *Complex Types* introduced in EF Core 8 are *partially supported** with some limitations.

1. The types are mapped to to OpenRiaServices's [Complex Types]
2. Any ef core configuration/metadata applied to the ComplexType (as part of fluent configuration) **IS NOT** discovered.
The `DbDomainServiceDescriptionProvider` and `DbDomainService` classes does not have any special handling of *Complex Types*.
    * Attributes on the types are discovered as expected using the normal built in reflection based attribute discovery


## Security

### Authorizing concurrency conflict disclosure

On an optimistic concurrency conflict, `DbDomainService` fetches current database
values without calling your query methods or their authorization checks. Conflict
responses may expose the entity (`StoreEntity`), differing members
(`ConflictMembers`), or delete status (`IsDeleteConflict`). A caller could exploit
this to learn data about another user's entity even when the write fails.

This hook is intended to protect applications that enforce row filtering in their
query methods (such as `Get`), since those filters do not apply to this lookup.
Database Row-Level Security (RLS), or a similar data-layer policy, is another
possible mitigation.

Override `CanDiscloseConflictAsync` to authorize disclosure. Its default returns
`true` for backward compatibility, so the mitigation is opt-in. For example, this
allows access only when the stored entity belongs to the authenticated user:

```csharp
protected override ValueTask<bool> CanDiscloseConflictAsync(
    ChangeSetEntry changeSetEntry,
    object storeEntity,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    var identity = ServiceContext.User.Identity;
    bool allowed = identity?.IsAuthenticated == true
        && storeEntity is MyEntity stored
        && stored.OwnerId == identity.Name;
    return new ValueTask<bool>(allowed);
}
```

Use trusted identity/tenant data and authorize access to every property in the
snapshot. Return `false` to deny all detailed conflict information. Async overrides
can perform authorization queries with the cancellation token.

- `storeEntity` is the database snapshot and may be `null` if the row is missing.
  Never authorize from `changeSetEntry.Entity` or `OriginalEntity`; both are client
  input. Permit disclosure of a missing row only when your server-side policy allows it.
- All snapshots are authorized before conflict details are populated. Returning
  `false` aborts the submit with a generic `UnauthorizedAccessException` through
  `DomainService.OnError`; `ResolveConflicts` is not called and the submit is not
  retried. Avoid sensitive details in custom errors.
- This hook controls disclosure, not write authorization, and runs after fetching
  database values. Enforce update/delete permissions separately.
