When using the Domain Service Wizard in Visual Studio 2015, the web.config file must have a targetFramework configured of at least 4.5, otherwise Visual Studio will return an error.

<system.web>
  <httpRuntime targetFramework = 4.5 />
</system.web>

All of the default web templates included in Visual Studio 2015 will set this in their Web.Config by default. Unfortunately, Microsoft's Silverlight templates do not and the Business Application template inherits behavior from the Silverlight templates.

For those that are curious about such things, tracking down this problem was the primary reason that the Visual Studio 2015 tools took a few months more then planned to be released.