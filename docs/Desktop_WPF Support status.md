When porting a Silverlight solution to WPF there are currently a number of issues to be aware of.

## Recommendations 

The recommendation is that you start with the following 2 steps before starting your migration

1. Refactor your project so that you separate your code generation to a project without XAML
2. Make sure that you use the latest version of OpenRiaServices and that you manually configure a RiaDomainClientFactory for DomainContext.DomainClientFactory.

##  Problems / Issues

1. You get compile time errors if you mix OpenRia codegen with some types of XAML pages/resources.
   The best approach is to get all your code generation to happen in a separate class library which is then referenced by the rest of your application.
2. You need to wait creating your WebContext until Application Startup (or you will get the wrong SyncronizationContext) instead of in Application constructor.
3. You will probably need to set the DomainContext property manually when creating the FormsAuthentication to your AuthenticationContext



