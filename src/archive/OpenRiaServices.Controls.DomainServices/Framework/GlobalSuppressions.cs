// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project. 
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc. 
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File". 
// You do not need to add suppressions to this file manually. 

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ria", Justification = "Spelling is correct.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Windows.Common", Justification = "All internal utilities are in the common namespace.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "OpenRiaServices.Controls.DomainServices", Justification = "This is the specced namespace.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ria", Scope = "namespace", Target = "OpenRiaServices.Controls.DomainServices", Justification = "It's in the product name.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Scope = "type", Target = "OpenRiaServices.Controls.IPagedEntityList", Justification = "This is not an exception type, it's an internal interface.  Known VS2008 FxCop bug - fixed in VS2010.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Assemblies are delay-signed.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "OpenRiaServices.Controls.DomainDataSource.#DomainContext_Loaded(OpenRiaServices.Controls.LoadedDataEventArgs,OpenRiaServices.Controls.LoadContext)", Justification = "This is a complex method.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "OpenRiaServices.Controls.DomainDataSource.#LoadData(OpenRiaServices.Controls.LoadType)", Justification = "This is a complex method.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "OpenRiaServices.Controls.FilterDescriptor.#Value", Justification = "GetValue is not a method typically called by developers.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Scope = "member", Target = "OpenRiaServices.Controls.GroupCollectionManager.#AsINotifyPropertyChanged(System.Object)", Justification = "Switching between two types on the first check.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "e", Scope = "member", Target = "OpenRiaServices.Controls.GroupCollectionManager.#HandleGroupDescriptionChanged(System.ComponentModel.GroupDescription,System.ComponentModel.PropertyChangedEventArgs)", Justification = "This method handler needs to keep this signature.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "e", Scope = "member", Target = "OpenRiaServices.Controls.GroupCollectionManager.#HandleGroupDescriptorChanged(OpenRiaServices.Controls.GroupDescriptor,System.ComponentModel.PropertyChangedEventArgs)", Justification = "This method handler needs to keep this signature.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Scope = "member", Target = "OpenRiaServices.Controls.GroupCollectionManager.#HandlePropertyChanged(System.Object,System.ComponentModel.PropertyChangedEventArgs)", Justification = "Switching between two types on the first check.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "OpenRiaServices.Controls.Parameter.#Value", Justification = "GetValue is not a method typically called by developers.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "e", Scope = "member", Target = "OpenRiaServices.Controls.SortCollectionManager.#HandleSortDescriptorChanged(OpenRiaServices.Controls.SortDescriptor,System.ComponentModel.PropertyChangedEventArgs)", Justification = "This method handler needs to keep this signature.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Scope = "type", Target = "OpenRiaServices.Controls.EntityCollectionView", Justification = "The disposable type does not need to be disposed.")]
