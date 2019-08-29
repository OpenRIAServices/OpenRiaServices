# Open Ria  Services Tooling

The Tooling provides templates for both items and projects using Open Ria Services as well as tooling support for configuring code generation options.

Item Templates for "Domain Service" and "Authentication Service" for C# should work as expected, **other will probably give errors**
Project Template "Open Ria Services Library" for C# should work, **other will probably give errors**

The tooling is only tested with OpenRiaServices 4.6.0+ and on for server nuget packages.

Some of the errors (about invalid extension GUID should be solvable by also installing the tooling for VS 2015)

# Open Ria Services Project Link

Right click any project and choose *Manage Open Ria Services Project Link* to bring up a dialog
which hels you configure the code generation settings.
Use it to select which project contains your *DomainServices*.

![Manage OpenRiaServices Project Link Context Menu](images/ManageOpenRiaServicesProjectLinkContextMenu.png "Manage OpenRiaServices Project Link Context Menu")


Once selected you will be presented with the *Manage OpenRiaServices Project Link* Dialog which allows you to select your web project.
![Manage OpenRiaServices Project Link Dialog](images/ManageOpenRiaServicesProjectLink.png "Configure code generation settings here")


# Item Templates

## New DomainService

Allows you to scaffold an new DomainService with all CRUD methods for the entities you select.

## New AuthenticationService

Creates a new AuthenticationService for logging in using AspNetMembership authentication

# Project Templates
