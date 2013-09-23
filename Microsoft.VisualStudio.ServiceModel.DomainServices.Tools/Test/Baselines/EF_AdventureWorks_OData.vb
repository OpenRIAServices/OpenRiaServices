
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports AdventureWorksModel
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data
Imports System.Linq
Imports OpenRiaServices.DomainServices.EntityFramework
Imports OpenRiaServices.DomainServices.Hosting
Imports OpenRiaServices.DomainServices.Server

Namespace BizLogic.Test
    
    'Implements application logic using the AdventureWorksEntities context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class EF_AdventureWorks_OData
        Inherits LinqToEntitiesDomainService(Of AdventureWorksEntities)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Addresses' query.
        <Query(IsDefault:=true)>  _
        Public Function GetAddresses() As IQueryable(Of Address)
            Return Me.ObjectContext.Addresses
        End Function
        
        Public Sub InsertAddress(ByVal address As Address)
            If ((address.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(address, EntityState.Added)
            Else
                Me.ObjectContext.Addresses.AddObject(address)
            End If
        End Sub
        
        Public Sub UpdateAddress(ByVal currentAddress As Address)
            Me.ObjectContext.Addresses.AttachAsModified(currentAddress, Me.ChangeSet.GetOriginal(currentAddress))
        End Sub
        
        Public Sub DeleteAddress(ByVal address As Address)
            If ((address.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(address, EntityState.Deleted)
            Else
                Me.ObjectContext.Addresses.Attach(address)
                Me.ObjectContext.Addresses.DeleteObject(address)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'AddressTypes' query.
        <Query(IsDefault:=true)>  _
        Public Function GetAddressTypes() As IQueryable(Of AddressType)
            Return Me.ObjectContext.AddressTypes
        End Function
        
        Public Sub InsertAddressType(ByVal addressType As AddressType)
            If ((addressType.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(addressType, EntityState.Added)
            Else
                Me.ObjectContext.AddressTypes.AddObject(addressType)
            End If
        End Sub
        
        Public Sub UpdateAddressType(ByVal currentAddressType As AddressType)
            Me.ObjectContext.AddressTypes.AttachAsModified(currentAddressType, Me.ChangeSet.GetOriginal(currentAddressType))
        End Sub
        
        Public Sub DeleteAddressType(ByVal addressType As AddressType)
            If ((addressType.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(addressType, EntityState.Deleted)
            Else
                Me.ObjectContext.AddressTypes.Attach(addressType)
                Me.ObjectContext.AddressTypes.DeleteObject(addressType)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'AWBuildVersions' query.
        <Query(IsDefault:=true)>  _
        Public Function GetAWBuildVersions() As IQueryable(Of AWBuildVersion)
            Return Me.ObjectContext.AWBuildVersions
        End Function
        
        Public Sub InsertAWBuildVersion(ByVal aWBuildVersion As AWBuildVersion)
            If ((aWBuildVersion.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(aWBuildVersion, EntityState.Added)
            Else
                Me.ObjectContext.AWBuildVersions.AddObject(aWBuildVersion)
            End If
        End Sub
        
        Public Sub UpdateAWBuildVersion(ByVal currentAWBuildVersion As AWBuildVersion)
            Me.ObjectContext.AWBuildVersions.AttachAsModified(currentAWBuildVersion, Me.ChangeSet.GetOriginal(currentAWBuildVersion))
        End Sub
        
        Public Sub DeleteAWBuildVersion(ByVal aWBuildVersion As AWBuildVersion)
            If ((aWBuildVersion.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(aWBuildVersion, EntityState.Deleted)
            Else
                Me.ObjectContext.AWBuildVersions.Attach(aWBuildVersion)
                Me.ObjectContext.AWBuildVersions.DeleteObject(aWBuildVersion)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'BillOfMaterials' query.
        <Query(IsDefault:=true)>  _
        Public Function GetBillOfMaterials() As IQueryable(Of BillOfMaterial)
            Return Me.ObjectContext.BillOfMaterials
        End Function
        
        Public Sub InsertBillOfMaterial(ByVal billOfMaterial As BillOfMaterial)
            If ((billOfMaterial.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(billOfMaterial, EntityState.Added)
            Else
                Me.ObjectContext.BillOfMaterials.AddObject(billOfMaterial)
            End If
        End Sub
        
        Public Sub UpdateBillOfMaterial(ByVal currentBillOfMaterial As BillOfMaterial)
            Me.ObjectContext.BillOfMaterials.AttachAsModified(currentBillOfMaterial, Me.ChangeSet.GetOriginal(currentBillOfMaterial))
        End Sub
        
        Public Sub DeleteBillOfMaterial(ByVal billOfMaterial As BillOfMaterial)
            If ((billOfMaterial.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(billOfMaterial, EntityState.Deleted)
            Else
                Me.ObjectContext.BillOfMaterials.Attach(billOfMaterial)
                Me.ObjectContext.BillOfMaterials.DeleteObject(billOfMaterial)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Contacts' query.
        <Query(IsDefault:=true)>  _
        Public Function GetContacts() As IQueryable(Of Contact)
            Return Me.ObjectContext.Contacts
        End Function
        
        Public Sub InsertContact(ByVal contact As Contact)
            If ((contact.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(contact, EntityState.Added)
            Else
                Me.ObjectContext.Contacts.AddObject(contact)
            End If
        End Sub
        
        Public Sub UpdateContact(ByVal currentContact As Contact)
            Me.ObjectContext.Contacts.AttachAsModified(currentContact, Me.ChangeSet.GetOriginal(currentContact))
        End Sub
        
        Public Sub DeleteContact(ByVal contact As Contact)
            If ((contact.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(contact, EntityState.Deleted)
            Else
                Me.ObjectContext.Contacts.Attach(contact)
                Me.ObjectContext.Contacts.DeleteObject(contact)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ContactCreditCards' query.
        <Query(IsDefault:=true)>  _
        Public Function GetContactCreditCards() As IQueryable(Of ContactCreditCard)
            Return Me.ObjectContext.ContactCreditCards
        End Function
        
        Public Sub InsertContactCreditCard(ByVal contactCreditCard As ContactCreditCard)
            If ((contactCreditCard.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(contactCreditCard, EntityState.Added)
            Else
                Me.ObjectContext.ContactCreditCards.AddObject(contactCreditCard)
            End If
        End Sub
        
        Public Sub UpdateContactCreditCard(ByVal currentContactCreditCard As ContactCreditCard)
            Me.ObjectContext.ContactCreditCards.AttachAsModified(currentContactCreditCard, Me.ChangeSet.GetOriginal(currentContactCreditCard))
        End Sub
        
        Public Sub DeleteContactCreditCard(ByVal contactCreditCard As ContactCreditCard)
            If ((contactCreditCard.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(contactCreditCard, EntityState.Deleted)
            Else
                Me.ObjectContext.ContactCreditCards.Attach(contactCreditCard)
                Me.ObjectContext.ContactCreditCards.DeleteObject(contactCreditCard)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ContactTypes' query.
        <Query(IsDefault:=true)>  _
        Public Function GetContactTypes() As IQueryable(Of ContactType)
            Return Me.ObjectContext.ContactTypes
        End Function
        
        Public Sub InsertContactType(ByVal contactType As ContactType)
            If ((contactType.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(contactType, EntityState.Added)
            Else
                Me.ObjectContext.ContactTypes.AddObject(contactType)
            End If
        End Sub
        
        Public Sub UpdateContactType(ByVal currentContactType As ContactType)
            Me.ObjectContext.ContactTypes.AttachAsModified(currentContactType, Me.ChangeSet.GetOriginal(currentContactType))
        End Sub
        
        Public Sub DeleteContactType(ByVal contactType As ContactType)
            If ((contactType.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(contactType, EntityState.Deleted)
            Else
                Me.ObjectContext.ContactTypes.Attach(contactType)
                Me.ObjectContext.ContactTypes.DeleteObject(contactType)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CountryRegions' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCountryRegions() As IQueryable(Of CountryRegion)
            Return Me.ObjectContext.CountryRegions
        End Function
        
        Public Sub InsertCountryRegion(ByVal countryRegion As CountryRegion)
            If ((countryRegion.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegion, EntityState.Added)
            Else
                Me.ObjectContext.CountryRegions.AddObject(countryRegion)
            End If
        End Sub
        
        Public Sub UpdateCountryRegion(ByVal currentCountryRegion As CountryRegion)
            Me.ObjectContext.CountryRegions.AttachAsModified(currentCountryRegion, Me.ChangeSet.GetOriginal(currentCountryRegion))
        End Sub
        
        Public Sub DeleteCountryRegion(ByVal countryRegion As CountryRegion)
            If ((countryRegion.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegion, EntityState.Deleted)
            Else
                Me.ObjectContext.CountryRegions.Attach(countryRegion)
                Me.ObjectContext.CountryRegions.DeleteObject(countryRegion)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CountryRegionCurrencies' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCountryRegionCurrencies() As IQueryable(Of CountryRegionCurrency)
            Return Me.ObjectContext.CountryRegionCurrencies
        End Function
        
        Public Sub InsertCountryRegionCurrency(ByVal countryRegionCurrency As CountryRegionCurrency)
            If ((countryRegionCurrency.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegionCurrency, EntityState.Added)
            Else
                Me.ObjectContext.CountryRegionCurrencies.AddObject(countryRegionCurrency)
            End If
        End Sub
        
        Public Sub UpdateCountryRegionCurrency(ByVal currentCountryRegionCurrency As CountryRegionCurrency)
            Me.ObjectContext.CountryRegionCurrencies.AttachAsModified(currentCountryRegionCurrency, Me.ChangeSet.GetOriginal(currentCountryRegionCurrency))
        End Sub
        
        Public Sub DeleteCountryRegionCurrency(ByVal countryRegionCurrency As CountryRegionCurrency)
            If ((countryRegionCurrency.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegionCurrency, EntityState.Deleted)
            Else
                Me.ObjectContext.CountryRegionCurrencies.Attach(countryRegionCurrency)
                Me.ObjectContext.CountryRegionCurrencies.DeleteObject(countryRegionCurrency)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CreditCards' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCreditCards() As IQueryable(Of CreditCard)
            Return Me.ObjectContext.CreditCards
        End Function
        
        Public Sub InsertCreditCard(ByVal creditCard As CreditCard)
            If ((creditCard.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(creditCard, EntityState.Added)
            Else
                Me.ObjectContext.CreditCards.AddObject(creditCard)
            End If
        End Sub
        
        Public Sub UpdateCreditCard(ByVal currentCreditCard As CreditCard)
            Me.ObjectContext.CreditCards.AttachAsModified(currentCreditCard, Me.ChangeSet.GetOriginal(currentCreditCard))
        End Sub
        
        Public Sub DeleteCreditCard(ByVal creditCard As CreditCard)
            If ((creditCard.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(creditCard, EntityState.Deleted)
            Else
                Me.ObjectContext.CreditCards.Attach(creditCard)
                Me.ObjectContext.CreditCards.DeleteObject(creditCard)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Cultures' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCultures() As IQueryable(Of Culture)
            Return Me.ObjectContext.Cultures
        End Function
        
        Public Sub InsertCulture(ByVal culture As Culture)
            If ((culture.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(culture, EntityState.Added)
            Else
                Me.ObjectContext.Cultures.AddObject(culture)
            End If
        End Sub
        
        Public Sub UpdateCulture(ByVal currentCulture As Culture)
            Me.ObjectContext.Cultures.AttachAsModified(currentCulture, Me.ChangeSet.GetOriginal(currentCulture))
        End Sub
        
        Public Sub DeleteCulture(ByVal culture As Culture)
            If ((culture.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(culture, EntityState.Deleted)
            Else
                Me.ObjectContext.Cultures.Attach(culture)
                Me.ObjectContext.Cultures.DeleteObject(culture)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Currencies' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCurrencies() As IQueryable(Of Currency)
            Return Me.ObjectContext.Currencies
        End Function
        
        Public Sub InsertCurrency(ByVal currency As Currency)
            If ((currency.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(currency, EntityState.Added)
            Else
                Me.ObjectContext.Currencies.AddObject(currency)
            End If
        End Sub
        
        Public Sub UpdateCurrency(ByVal currentCurrency As Currency)
            Me.ObjectContext.Currencies.AttachAsModified(currentCurrency, Me.ChangeSet.GetOriginal(currentCurrency))
        End Sub
        
        Public Sub DeleteCurrency(ByVal currency As Currency)
            If ((currency.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(currency, EntityState.Deleted)
            Else
                Me.ObjectContext.Currencies.Attach(currency)
                Me.ObjectContext.Currencies.DeleteObject(currency)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CurrencyRates' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCurrencyRates() As IQueryable(Of CurrencyRate)
            Return Me.ObjectContext.CurrencyRates
        End Function
        
        Public Sub InsertCurrencyRate(ByVal currencyRate As CurrencyRate)
            If ((currencyRate.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(currencyRate, EntityState.Added)
            Else
                Me.ObjectContext.CurrencyRates.AddObject(currencyRate)
            End If
        End Sub
        
        Public Sub UpdateCurrencyRate(ByVal currentCurrencyRate As CurrencyRate)
            Me.ObjectContext.CurrencyRates.AttachAsModified(currentCurrencyRate, Me.ChangeSet.GetOriginal(currentCurrencyRate))
        End Sub
        
        Public Sub DeleteCurrencyRate(ByVal currencyRate As CurrencyRate)
            If ((currencyRate.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(currencyRate, EntityState.Deleted)
            Else
                Me.ObjectContext.CurrencyRates.Attach(currencyRate)
                Me.ObjectContext.CurrencyRates.DeleteObject(currencyRate)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Customers' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCustomers() As IQueryable(Of Customer)
            Return Me.ObjectContext.Customers
        End Function
        
        Public Sub InsertCustomer(ByVal customer As Customer)
            If ((customer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added)
            Else
                Me.ObjectContext.Customers.AddObject(customer)
            End If
        End Sub
        
        Public Sub UpdateCustomer(ByVal currentCustomer As Customer)
            Me.ObjectContext.Customers.AttachAsModified(currentCustomer, Me.ChangeSet.GetOriginal(currentCustomer))
        End Sub
        
        Public Sub DeleteCustomer(ByVal customer As Customer)
            If ((customer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted)
            Else
                Me.ObjectContext.Customers.Attach(customer)
                Me.ObjectContext.Customers.DeleteObject(customer)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'CustomerAddresses' query.
        <Query(IsDefault:=true)>  _
        Public Function GetCustomerAddresses() As IQueryable(Of CustomerAddress)
            Return Me.ObjectContext.CustomerAddresses
        End Function
        
        Public Sub InsertCustomerAddress(ByVal customerAddress As CustomerAddress)
            If ((customerAddress.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customerAddress, EntityState.Added)
            Else
                Me.ObjectContext.CustomerAddresses.AddObject(customerAddress)
            End If
        End Sub
        
        Public Sub UpdateCustomerAddress(ByVal currentCustomerAddress As CustomerAddress)
            Me.ObjectContext.CustomerAddresses.AttachAsModified(currentCustomerAddress, Me.ChangeSet.GetOriginal(currentCustomerAddress))
        End Sub
        
        Public Sub DeleteCustomerAddress(ByVal customerAddress As CustomerAddress)
            If ((customerAddress.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(customerAddress, EntityState.Deleted)
            Else
                Me.ObjectContext.CustomerAddresses.Attach(customerAddress)
                Me.ObjectContext.CustomerAddresses.DeleteObject(customerAddress)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'DatabaseLogs' query.
        <Query(IsDefault:=true)>  _
        Public Function GetDatabaseLogs() As IQueryable(Of DatabaseLog)
            Return Me.ObjectContext.DatabaseLogs
        End Function
        
        Public Sub InsertDatabaseLog(ByVal databaseLog As DatabaseLog)
            If ((databaseLog.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(databaseLog, EntityState.Added)
            Else
                Me.ObjectContext.DatabaseLogs.AddObject(databaseLog)
            End If
        End Sub
        
        Public Sub UpdateDatabaseLog(ByVal currentDatabaseLog As DatabaseLog)
            Me.ObjectContext.DatabaseLogs.AttachAsModified(currentDatabaseLog, Me.ChangeSet.GetOriginal(currentDatabaseLog))
        End Sub
        
        Public Sub DeleteDatabaseLog(ByVal databaseLog As DatabaseLog)
            If ((databaseLog.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(databaseLog, EntityState.Deleted)
            Else
                Me.ObjectContext.DatabaseLogs.Attach(databaseLog)
                Me.ObjectContext.DatabaseLogs.DeleteObject(databaseLog)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Departments' query.
        <Query(IsDefault:=true)>  _
        Public Function GetDepartments() As IQueryable(Of Department)
            Return Me.ObjectContext.Departments
        End Function
        
        Public Sub InsertDepartment(ByVal department As Department)
            If ((department.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(department, EntityState.Added)
            Else
                Me.ObjectContext.Departments.AddObject(department)
            End If
        End Sub
        
        Public Sub UpdateDepartment(ByVal currentDepartment As Department)
            Me.ObjectContext.Departments.AttachAsModified(currentDepartment, Me.ChangeSet.GetOriginal(currentDepartment))
        End Sub
        
        Public Sub DeleteDepartment(ByVal department As Department)
            If ((department.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(department, EntityState.Deleted)
            Else
                Me.ObjectContext.Departments.Attach(department)
                Me.ObjectContext.Departments.DeleteObject(department)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Documents' query.
        <Query(IsDefault:=true)>  _
        Public Function GetDocuments() As IQueryable(Of Document)
            Return Me.ObjectContext.Documents
        End Function
        
        Public Sub InsertDocument(ByVal document As Document)
            If ((document.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(document, EntityState.Added)
            Else
                Me.ObjectContext.Documents.AddObject(document)
            End If
        End Sub
        
        Public Sub UpdateDocument(ByVal currentDocument As Document)
            Me.ObjectContext.Documents.AttachAsModified(currentDocument, Me.ChangeSet.GetOriginal(currentDocument))
        End Sub
        
        Public Sub DeleteDocument(ByVal document As Document)
            If ((document.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(document, EntityState.Deleted)
            Else
                Me.ObjectContext.Documents.Attach(document)
                Me.ObjectContext.Documents.DeleteObject(document)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Employees' query.
        <Query(IsDefault:=true)>  _
        Public Function GetEmployees() As IQueryable(Of Employee)
            Return Me.ObjectContext.Employees
        End Function
        
        Public Sub InsertEmployee(ByVal employee As Employee)
            If ((employee.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Added)
            Else
                Me.ObjectContext.Employees.AddObject(employee)
            End If
        End Sub
        
        Public Sub UpdateEmployee(ByVal currentEmployee As Employee)
            Me.ObjectContext.Employees.AttachAsModified(currentEmployee, Me.ChangeSet.GetOriginal(currentEmployee))
        End Sub
        
        Public Sub DeleteEmployee(ByVal employee As Employee)
            If ((employee.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Deleted)
            Else
                Me.ObjectContext.Employees.Attach(employee)
                Me.ObjectContext.Employees.DeleteObject(employee)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'EmployeeAddresses' query.
        <Query(IsDefault:=true)>  _
        Public Function GetEmployeeAddresses() As IQueryable(Of EmployeeAddress)
            Return Me.ObjectContext.EmployeeAddresses
        End Function
        
        Public Sub InsertEmployeeAddress(ByVal employeeAddress As EmployeeAddress)
            If ((employeeAddress.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeeAddress, EntityState.Added)
            Else
                Me.ObjectContext.EmployeeAddresses.AddObject(employeeAddress)
            End If
        End Sub
        
        Public Sub UpdateEmployeeAddress(ByVal currentEmployeeAddress As EmployeeAddress)
            Me.ObjectContext.EmployeeAddresses.AttachAsModified(currentEmployeeAddress, Me.ChangeSet.GetOriginal(currentEmployeeAddress))
        End Sub
        
        Public Sub DeleteEmployeeAddress(ByVal employeeAddress As EmployeeAddress)
            If ((employeeAddress.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeeAddress, EntityState.Deleted)
            Else
                Me.ObjectContext.EmployeeAddresses.Attach(employeeAddress)
                Me.ObjectContext.EmployeeAddresses.DeleteObject(employeeAddress)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'EmployeeDepartmentHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetEmployeeDepartmentHistories() As IQueryable(Of EmployeeDepartmentHistory)
            Return Me.ObjectContext.EmployeeDepartmentHistories
        End Function
        
        Public Sub InsertEmployeeDepartmentHistory(ByVal employeeDepartmentHistory As EmployeeDepartmentHistory)
            If ((employeeDepartmentHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeeDepartmentHistory, EntityState.Added)
            Else
                Me.ObjectContext.EmployeeDepartmentHistories.AddObject(employeeDepartmentHistory)
            End If
        End Sub
        
        Public Sub UpdateEmployeeDepartmentHistory(ByVal currentEmployeeDepartmentHistory As EmployeeDepartmentHistory)
            Me.ObjectContext.EmployeeDepartmentHistories.AttachAsModified(currentEmployeeDepartmentHistory, Me.ChangeSet.GetOriginal(currentEmployeeDepartmentHistory))
        End Sub
        
        Public Sub DeleteEmployeeDepartmentHistory(ByVal employeeDepartmentHistory As EmployeeDepartmentHistory)
            If ((employeeDepartmentHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeeDepartmentHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.EmployeeDepartmentHistories.Attach(employeeDepartmentHistory)
                Me.ObjectContext.EmployeeDepartmentHistories.DeleteObject(employeeDepartmentHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'EmployeePayHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetEmployeePayHistories() As IQueryable(Of EmployeePayHistory)
            Return Me.ObjectContext.EmployeePayHistories
        End Function
        
        Public Sub InsertEmployeePayHistory(ByVal employeePayHistory As EmployeePayHistory)
            If ((employeePayHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeePayHistory, EntityState.Added)
            Else
                Me.ObjectContext.EmployeePayHistories.AddObject(employeePayHistory)
            End If
        End Sub
        
        Public Sub UpdateEmployeePayHistory(ByVal currentEmployeePayHistory As EmployeePayHistory)
            Me.ObjectContext.EmployeePayHistories.AttachAsModified(currentEmployeePayHistory, Me.ChangeSet.GetOriginal(currentEmployeePayHistory))
        End Sub
        
        Public Sub DeleteEmployeePayHistory(ByVal employeePayHistory As EmployeePayHistory)
            If ((employeePayHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(employeePayHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.EmployeePayHistories.Attach(employeePayHistory)
                Me.ObjectContext.EmployeePayHistories.DeleteObject(employeePayHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ErrorLogs' query.
        <Query(IsDefault:=true)>  _
        Public Function GetErrorLogs() As IQueryable(Of ErrorLog)
            Return Me.ObjectContext.ErrorLogs
        End Function
        
        Public Sub InsertErrorLog(ByVal errorLog As ErrorLog)
            If ((errorLog.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(errorLog, EntityState.Added)
            Else
                Me.ObjectContext.ErrorLogs.AddObject(errorLog)
            End If
        End Sub
        
        Public Sub UpdateErrorLog(ByVal currentErrorLog As ErrorLog)
            Me.ObjectContext.ErrorLogs.AttachAsModified(currentErrorLog, Me.ChangeSet.GetOriginal(currentErrorLog))
        End Sub
        
        Public Sub DeleteErrorLog(ByVal errorLog As ErrorLog)
            If ((errorLog.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(errorLog, EntityState.Deleted)
            Else
                Me.ObjectContext.ErrorLogs.Attach(errorLog)
                Me.ObjectContext.ErrorLogs.DeleteObject(errorLog)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Illustrations' query.
        <Query(IsDefault:=true)>  _
        Public Function GetIllustrations() As IQueryable(Of Illustration)
            Return Me.ObjectContext.Illustrations
        End Function
        
        Public Sub InsertIllustration(ByVal illustration As Illustration)
            If ((illustration.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(illustration, EntityState.Added)
            Else
                Me.ObjectContext.Illustrations.AddObject(illustration)
            End If
        End Sub
        
        Public Sub UpdateIllustration(ByVal currentIllustration As Illustration)
            Me.ObjectContext.Illustrations.AttachAsModified(currentIllustration, Me.ChangeSet.GetOriginal(currentIllustration))
        End Sub
        
        Public Sub DeleteIllustration(ByVal illustration As Illustration)
            If ((illustration.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(illustration, EntityState.Deleted)
            Else
                Me.ObjectContext.Illustrations.Attach(illustration)
                Me.ObjectContext.Illustrations.DeleteObject(illustration)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Individuals' query.
        <Query(IsDefault:=true)>  _
        Public Function GetIndividuals() As IQueryable(Of Individual)
            Return Me.ObjectContext.Individuals
        End Function
        
        Public Sub InsertIndividual(ByVal individual As Individual)
            If ((individual.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(individual, EntityState.Added)
            Else
                Me.ObjectContext.Individuals.AddObject(individual)
            End If
        End Sub
        
        Public Sub UpdateIndividual(ByVal currentIndividual As Individual)
            Me.ObjectContext.Individuals.AttachAsModified(currentIndividual, Me.ChangeSet.GetOriginal(currentIndividual))
        End Sub
        
        Public Sub DeleteIndividual(ByVal individual As Individual)
            If ((individual.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(individual, EntityState.Deleted)
            Else
                Me.ObjectContext.Individuals.Attach(individual)
                Me.ObjectContext.Individuals.DeleteObject(individual)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'JobCandidates' query.
        <Query(IsDefault:=true)>  _
        Public Function GetJobCandidates() As IQueryable(Of JobCandidate)
            Return Me.ObjectContext.JobCandidates
        End Function
        
        Public Sub InsertJobCandidate(ByVal jobCandidate As JobCandidate)
            If ((jobCandidate.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(jobCandidate, EntityState.Added)
            Else
                Me.ObjectContext.JobCandidates.AddObject(jobCandidate)
            End If
        End Sub
        
        Public Sub UpdateJobCandidate(ByVal currentJobCandidate As JobCandidate)
            Me.ObjectContext.JobCandidates.AttachAsModified(currentJobCandidate, Me.ChangeSet.GetOriginal(currentJobCandidate))
        End Sub
        
        Public Sub DeleteJobCandidate(ByVal jobCandidate As JobCandidate)
            If ((jobCandidate.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(jobCandidate, EntityState.Deleted)
            Else
                Me.ObjectContext.JobCandidates.Attach(jobCandidate)
                Me.ObjectContext.JobCandidates.DeleteObject(jobCandidate)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Locations' query.
        <Query(IsDefault:=true)>  _
        Public Function GetLocations() As IQueryable(Of Location)
            Return Me.ObjectContext.Locations
        End Function
        
        Public Sub InsertLocation(ByVal location As Location)
            If ((location.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(location, EntityState.Added)
            Else
                Me.ObjectContext.Locations.AddObject(location)
            End If
        End Sub
        
        Public Sub UpdateLocation(ByVal currentLocation As Location)
            Me.ObjectContext.Locations.AttachAsModified(currentLocation, Me.ChangeSet.GetOriginal(currentLocation))
        End Sub
        
        Public Sub DeleteLocation(ByVal location As Location)
            If ((location.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(location, EntityState.Deleted)
            Else
                Me.ObjectContext.Locations.Attach(location)
                Me.ObjectContext.Locations.DeleteObject(location)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Products' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProducts() As IQueryable(Of Product)
            Return Me.ObjectContext.Products
        End Function
        
        Public Sub InsertProduct(ByVal product As Product)
            If ((product.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added)
            Else
                Me.ObjectContext.Products.AddObject(product)
            End If
        End Sub
        
        Public Sub UpdateProduct(ByVal currentProduct As Product)
            Me.ObjectContext.Products.AttachAsModified(currentProduct, Me.ChangeSet.GetOriginal(currentProduct))
        End Sub
        
        Public Sub DeleteProduct(ByVal product As Product)
            If ((product.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted)
            Else
                Me.ObjectContext.Products.Attach(product)
                Me.ObjectContext.Products.DeleteObject(product)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductCategories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductCategories() As IQueryable(Of ProductCategory)
            Return Me.ObjectContext.ProductCategories
        End Function
        
        Public Sub InsertProductCategory(ByVal productCategory As ProductCategory)
            If ((productCategory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productCategory, EntityState.Added)
            Else
                Me.ObjectContext.ProductCategories.AddObject(productCategory)
            End If
        End Sub
        
        Public Sub UpdateProductCategory(ByVal currentProductCategory As ProductCategory)
            Me.ObjectContext.ProductCategories.AttachAsModified(currentProductCategory, Me.ChangeSet.GetOriginal(currentProductCategory))
        End Sub
        
        Public Sub DeleteProductCategory(ByVal productCategory As ProductCategory)
            If ((productCategory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productCategory, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductCategories.Attach(productCategory)
                Me.ObjectContext.ProductCategories.DeleteObject(productCategory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductCostHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductCostHistories() As IQueryable(Of ProductCostHistory)
            Return Me.ObjectContext.ProductCostHistories
        End Function
        
        Public Sub InsertProductCostHistory(ByVal productCostHistory As ProductCostHistory)
            If ((productCostHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productCostHistory, EntityState.Added)
            Else
                Me.ObjectContext.ProductCostHistories.AddObject(productCostHistory)
            End If
        End Sub
        
        Public Sub UpdateProductCostHistory(ByVal currentProductCostHistory As ProductCostHistory)
            Me.ObjectContext.ProductCostHistories.AttachAsModified(currentProductCostHistory, Me.ChangeSet.GetOriginal(currentProductCostHistory))
        End Sub
        
        Public Sub DeleteProductCostHistory(ByVal productCostHistory As ProductCostHistory)
            If ((productCostHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productCostHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductCostHistories.Attach(productCostHistory)
                Me.ObjectContext.ProductCostHistories.DeleteObject(productCostHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductDescriptions' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductDescriptions() As IQueryable(Of ProductDescription)
            Return Me.ObjectContext.ProductDescriptions
        End Function
        
        Public Sub InsertProductDescription(ByVal productDescription As ProductDescription)
            If ((productDescription.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productDescription, EntityState.Added)
            Else
                Me.ObjectContext.ProductDescriptions.AddObject(productDescription)
            End If
        End Sub
        
        Public Sub UpdateProductDescription(ByVal currentProductDescription As ProductDescription)
            Me.ObjectContext.ProductDescriptions.AttachAsModified(currentProductDescription, Me.ChangeSet.GetOriginal(currentProductDescription))
        End Sub
        
        Public Sub DeleteProductDescription(ByVal productDescription As ProductDescription)
            If ((productDescription.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productDescription, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductDescriptions.Attach(productDescription)
                Me.ObjectContext.ProductDescriptions.DeleteObject(productDescription)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductDocuments' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductDocuments() As IQueryable(Of ProductDocument)
            Return Me.ObjectContext.ProductDocuments
        End Function
        
        Public Sub InsertProductDocument(ByVal productDocument As ProductDocument)
            If ((productDocument.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productDocument, EntityState.Added)
            Else
                Me.ObjectContext.ProductDocuments.AddObject(productDocument)
            End If
        End Sub
        
        Public Sub UpdateProductDocument(ByVal currentProductDocument As ProductDocument)
            Me.ObjectContext.ProductDocuments.AttachAsModified(currentProductDocument, Me.ChangeSet.GetOriginal(currentProductDocument))
        End Sub
        
        Public Sub DeleteProductDocument(ByVal productDocument As ProductDocument)
            If ((productDocument.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productDocument, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductDocuments.Attach(productDocument)
                Me.ObjectContext.ProductDocuments.DeleteObject(productDocument)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductInventories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductInventories() As IQueryable(Of ProductInventory)
            Return Me.ObjectContext.ProductInventories
        End Function
        
        Public Sub InsertProductInventory(ByVal productInventory As ProductInventory)
            If ((productInventory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productInventory, EntityState.Added)
            Else
                Me.ObjectContext.ProductInventories.AddObject(productInventory)
            End If
        End Sub
        
        Public Sub UpdateProductInventory(ByVal currentProductInventory As ProductInventory)
            Me.ObjectContext.ProductInventories.AttachAsModified(currentProductInventory, Me.ChangeSet.GetOriginal(currentProductInventory))
        End Sub
        
        Public Sub DeleteProductInventory(ByVal productInventory As ProductInventory)
            If ((productInventory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productInventory, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductInventories.Attach(productInventory)
                Me.ObjectContext.ProductInventories.DeleteObject(productInventory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductListPriceHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductListPriceHistories() As IQueryable(Of ProductListPriceHistory)
            Return Me.ObjectContext.ProductListPriceHistories
        End Function
        
        Public Sub InsertProductListPriceHistory(ByVal productListPriceHistory As ProductListPriceHistory)
            If ((productListPriceHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productListPriceHistory, EntityState.Added)
            Else
                Me.ObjectContext.ProductListPriceHistories.AddObject(productListPriceHistory)
            End If
        End Sub
        
        Public Sub UpdateProductListPriceHistory(ByVal currentProductListPriceHistory As ProductListPriceHistory)
            Me.ObjectContext.ProductListPriceHistories.AttachAsModified(currentProductListPriceHistory, Me.ChangeSet.GetOriginal(currentProductListPriceHistory))
        End Sub
        
        Public Sub DeleteProductListPriceHistory(ByVal productListPriceHistory As ProductListPriceHistory)
            If ((productListPriceHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productListPriceHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductListPriceHistories.Attach(productListPriceHistory)
                Me.ObjectContext.ProductListPriceHistories.DeleteObject(productListPriceHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductModels' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductModels() As IQueryable(Of ProductModel)
            Return Me.ObjectContext.ProductModels
        End Function
        
        Public Sub InsertProductModel(ByVal productModel As ProductModel)
            If ((productModel.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productModel, EntityState.Added)
            Else
                Me.ObjectContext.ProductModels.AddObject(productModel)
            End If
        End Sub
        
        Public Sub UpdateProductModel(ByVal currentProductModel As ProductModel)
            Me.ObjectContext.ProductModels.AttachAsModified(currentProductModel, Me.ChangeSet.GetOriginal(currentProductModel))
        End Sub
        
        Public Sub DeleteProductModel(ByVal productModel As ProductModel)
            If ((productModel.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productModel, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductModels.Attach(productModel)
                Me.ObjectContext.ProductModels.DeleteObject(productModel)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductModelIllustrations' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductModelIllustrations() As IQueryable(Of ProductModelIllustration)
            Return Me.ObjectContext.ProductModelIllustrations
        End Function
        
        Public Sub InsertProductModelIllustration(ByVal productModelIllustration As ProductModelIllustration)
            If ((productModelIllustration.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productModelIllustration, EntityState.Added)
            Else
                Me.ObjectContext.ProductModelIllustrations.AddObject(productModelIllustration)
            End If
        End Sub
        
        Public Sub UpdateProductModelIllustration(ByVal currentProductModelIllustration As ProductModelIllustration)
            Me.ObjectContext.ProductModelIllustrations.AttachAsModified(currentProductModelIllustration, Me.ChangeSet.GetOriginal(currentProductModelIllustration))
        End Sub
        
        Public Sub DeleteProductModelIllustration(ByVal productModelIllustration As ProductModelIllustration)
            If ((productModelIllustration.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productModelIllustration, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductModelIllustrations.Attach(productModelIllustration)
                Me.ObjectContext.ProductModelIllustrations.DeleteObject(productModelIllustration)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductModelProductDescriptionCultures' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductModelProductDescriptionCultures() As IQueryable(Of ProductModelProductDescriptionCulture)
            Return Me.ObjectContext.ProductModelProductDescriptionCultures
        End Function
        
        Public Sub InsertProductModelProductDescriptionCulture(ByVal productModelProductDescriptionCulture As ProductModelProductDescriptionCulture)
            If ((productModelProductDescriptionCulture.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productModelProductDescriptionCulture, EntityState.Added)
            Else
                Me.ObjectContext.ProductModelProductDescriptionCultures.AddObject(productModelProductDescriptionCulture)
            End If
        End Sub
        
        Public Sub UpdateProductModelProductDescriptionCulture(ByVal currentProductModelProductDescriptionCulture As ProductModelProductDescriptionCulture)
            Me.ObjectContext.ProductModelProductDescriptionCultures.AttachAsModified(currentProductModelProductDescriptionCulture, Me.ChangeSet.GetOriginal(currentProductModelProductDescriptionCulture))
        End Sub
        
        Public Sub DeleteProductModelProductDescriptionCulture(ByVal productModelProductDescriptionCulture As ProductModelProductDescriptionCulture)
            If ((productModelProductDescriptionCulture.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productModelProductDescriptionCulture, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductModelProductDescriptionCultures.Attach(productModelProductDescriptionCulture)
                Me.ObjectContext.ProductModelProductDescriptionCultures.DeleteObject(productModelProductDescriptionCulture)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductPhotoes' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductPhotoes() As IQueryable(Of ProductPhoto)
            Return Me.ObjectContext.ProductPhotoes
        End Function
        
        Public Sub InsertProductPhoto(ByVal productPhoto As ProductPhoto)
            If ((productPhoto.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productPhoto, EntityState.Added)
            Else
                Me.ObjectContext.ProductPhotoes.AddObject(productPhoto)
            End If
        End Sub
        
        Public Sub UpdateProductPhoto(ByVal currentProductPhoto As ProductPhoto)
            Me.ObjectContext.ProductPhotoes.AttachAsModified(currentProductPhoto, Me.ChangeSet.GetOriginal(currentProductPhoto))
        End Sub
        
        Public Sub DeleteProductPhoto(ByVal productPhoto As ProductPhoto)
            If ((productPhoto.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productPhoto, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductPhotoes.Attach(productPhoto)
                Me.ObjectContext.ProductPhotoes.DeleteObject(productPhoto)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductProductPhotoes' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductProductPhotoes() As IQueryable(Of ProductProductPhoto)
            Return Me.ObjectContext.ProductProductPhotoes
        End Function
        
        Public Sub InsertProductProductPhoto(ByVal productProductPhoto As ProductProductPhoto)
            If ((productProductPhoto.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productProductPhoto, EntityState.Added)
            Else
                Me.ObjectContext.ProductProductPhotoes.AddObject(productProductPhoto)
            End If
        End Sub
        
        Public Sub UpdateProductProductPhoto(ByVal currentProductProductPhoto As ProductProductPhoto)
            Me.ObjectContext.ProductProductPhotoes.AttachAsModified(currentProductProductPhoto, Me.ChangeSet.GetOriginal(currentProductProductPhoto))
        End Sub
        
        Public Sub DeleteProductProductPhoto(ByVal productProductPhoto As ProductProductPhoto)
            If ((productProductPhoto.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productProductPhoto, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductProductPhotoes.Attach(productProductPhoto)
                Me.ObjectContext.ProductProductPhotoes.DeleteObject(productProductPhoto)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductReviews' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductReviews() As IQueryable(Of ProductReview)
            Return Me.ObjectContext.ProductReviews
        End Function
        
        Public Sub InsertProductReview(ByVal productReview As ProductReview)
            If ((productReview.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productReview, EntityState.Added)
            Else
                Me.ObjectContext.ProductReviews.AddObject(productReview)
            End If
        End Sub
        
        Public Sub UpdateProductReview(ByVal currentProductReview As ProductReview)
            Me.ObjectContext.ProductReviews.AttachAsModified(currentProductReview, Me.ChangeSet.GetOriginal(currentProductReview))
        End Sub
        
        Public Sub DeleteProductReview(ByVal productReview As ProductReview)
            If ((productReview.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productReview, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductReviews.Attach(productReview)
                Me.ObjectContext.ProductReviews.DeleteObject(productReview)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductSubcategories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductSubcategories() As IQueryable(Of ProductSubcategory)
            Return Me.ObjectContext.ProductSubcategories
        End Function
        
        Public Sub InsertProductSubcategory(ByVal productSubcategory As ProductSubcategory)
            If ((productSubcategory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productSubcategory, EntityState.Added)
            Else
                Me.ObjectContext.ProductSubcategories.AddObject(productSubcategory)
            End If
        End Sub
        
        Public Sub UpdateProductSubcategory(ByVal currentProductSubcategory As ProductSubcategory)
            Me.ObjectContext.ProductSubcategories.AttachAsModified(currentProductSubcategory, Me.ChangeSet.GetOriginal(currentProductSubcategory))
        End Sub
        
        Public Sub DeleteProductSubcategory(ByVal productSubcategory As ProductSubcategory)
            If ((productSubcategory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productSubcategory, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductSubcategories.Attach(productSubcategory)
                Me.ObjectContext.ProductSubcategories.DeleteObject(productSubcategory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ProductVendors' query.
        <Query(IsDefault:=true)>  _
        Public Function GetProductVendors() As IQueryable(Of ProductVendor)
            Return Me.ObjectContext.ProductVendors
        End Function
        
        Public Sub InsertProductVendor(ByVal productVendor As ProductVendor)
            If ((productVendor.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productVendor, EntityState.Added)
            Else
                Me.ObjectContext.ProductVendors.AddObject(productVendor)
            End If
        End Sub
        
        Public Sub UpdateProductVendor(ByVal currentProductVendor As ProductVendor)
            Me.ObjectContext.ProductVendors.AttachAsModified(currentProductVendor, Me.ChangeSet.GetOriginal(currentProductVendor))
        End Sub
        
        Public Sub DeleteProductVendor(ByVal productVendor As ProductVendor)
            If ((productVendor.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(productVendor, EntityState.Deleted)
            Else
                Me.ObjectContext.ProductVendors.Attach(productVendor)
                Me.ObjectContext.ProductVendors.DeleteObject(productVendor)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'PurchaseOrders' query.
        <Query(IsDefault:=true)>  _
        Public Function GetPurchaseOrders() As IQueryable(Of PurchaseOrder)
            Return Me.ObjectContext.PurchaseOrders
        End Function
        
        Public Sub InsertPurchaseOrder(ByVal purchaseOrder As PurchaseOrder)
            If ((purchaseOrder.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrder, EntityState.Added)
            Else
                Me.ObjectContext.PurchaseOrders.AddObject(purchaseOrder)
            End If
        End Sub
        
        Public Sub UpdatePurchaseOrder(ByVal currentPurchaseOrder As PurchaseOrder)
            Me.ObjectContext.PurchaseOrders.AttachAsModified(currentPurchaseOrder, Me.ChangeSet.GetOriginal(currentPurchaseOrder))
        End Sub
        
        Public Sub DeletePurchaseOrder(ByVal purchaseOrder As PurchaseOrder)
            If ((purchaseOrder.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrder, EntityState.Deleted)
            Else
                Me.ObjectContext.PurchaseOrders.Attach(purchaseOrder)
                Me.ObjectContext.PurchaseOrders.DeleteObject(purchaseOrder)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'PurchaseOrderDetails' query.
        <Query(IsDefault:=true)>  _
        Public Function GetPurchaseOrderDetails() As IQueryable(Of PurchaseOrderDetail)
            Return Me.ObjectContext.PurchaseOrderDetails
        End Function
        
        Public Sub InsertPurchaseOrderDetail(ByVal purchaseOrderDetail As PurchaseOrderDetail)
            If ((purchaseOrderDetail.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrderDetail, EntityState.Added)
            Else
                Me.ObjectContext.PurchaseOrderDetails.AddObject(purchaseOrderDetail)
            End If
        End Sub
        
        Public Sub UpdatePurchaseOrderDetail(ByVal currentPurchaseOrderDetail As PurchaseOrderDetail)
            Me.ObjectContext.PurchaseOrderDetails.AttachAsModified(currentPurchaseOrderDetail, Me.ChangeSet.GetOriginal(currentPurchaseOrderDetail))
        End Sub
        
        Public Sub DeletePurchaseOrderDetail(ByVal purchaseOrderDetail As PurchaseOrderDetail)
            If ((purchaseOrderDetail.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrderDetail, EntityState.Deleted)
            Else
                Me.ObjectContext.PurchaseOrderDetails.Attach(purchaseOrderDetail)
                Me.ObjectContext.PurchaseOrderDetails.DeleteObject(purchaseOrderDetail)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesOrderDetails' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesOrderDetails() As IQueryable(Of SalesOrderDetail)
            Return Me.ObjectContext.SalesOrderDetails
        End Function
        
        Public Sub InsertSalesOrderDetail(ByVal salesOrderDetail As SalesOrderDetail)
            If ((salesOrderDetail.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderDetail, EntityState.Added)
            Else
                Me.ObjectContext.SalesOrderDetails.AddObject(salesOrderDetail)
            End If
        End Sub
        
        Public Sub UpdateSalesOrderDetail(ByVal currentSalesOrderDetail As SalesOrderDetail)
            Me.ObjectContext.SalesOrderDetails.AttachAsModified(currentSalesOrderDetail, Me.ChangeSet.GetOriginal(currentSalesOrderDetail))
        End Sub
        
        Public Sub DeleteSalesOrderDetail(ByVal salesOrderDetail As SalesOrderDetail)
            If ((salesOrderDetail.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderDetail, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesOrderDetails.Attach(salesOrderDetail)
                Me.ObjectContext.SalesOrderDetails.DeleteObject(salesOrderDetail)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesOrderHeaders' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesOrderHeaders() As IQueryable(Of SalesOrderHeader)
            Return Me.ObjectContext.SalesOrderHeaders
        End Function
        
        Public Sub InsertSalesOrderHeader(ByVal salesOrderHeader As SalesOrderHeader)
            If ((salesOrderHeader.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeader, EntityState.Added)
            Else
                Me.ObjectContext.SalesOrderHeaders.AddObject(salesOrderHeader)
            End If
        End Sub
        
        Public Sub UpdateSalesOrderHeader(ByVal currentSalesOrderHeader As SalesOrderHeader)
            Me.ObjectContext.SalesOrderHeaders.AttachAsModified(currentSalesOrderHeader, Me.ChangeSet.GetOriginal(currentSalesOrderHeader))
        End Sub
        
        Public Sub DeleteSalesOrderHeader(ByVal salesOrderHeader As SalesOrderHeader)
            If ((salesOrderHeader.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeader, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesOrderHeaders.Attach(salesOrderHeader)
                Me.ObjectContext.SalesOrderHeaders.DeleteObject(salesOrderHeader)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesOrderHeaderSalesReasons' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesOrderHeaderSalesReasons() As IQueryable(Of SalesOrderHeaderSalesReason)
            Return Me.ObjectContext.SalesOrderHeaderSalesReasons
        End Function
        
        Public Sub InsertSalesOrderHeaderSalesReason(ByVal salesOrderHeaderSalesReason As SalesOrderHeaderSalesReason)
            If ((salesOrderHeaderSalesReason.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeaderSalesReason, EntityState.Added)
            Else
                Me.ObjectContext.SalesOrderHeaderSalesReasons.AddObject(salesOrderHeaderSalesReason)
            End If
        End Sub
        
        Public Sub UpdateSalesOrderHeaderSalesReason(ByVal currentSalesOrderHeaderSalesReason As SalesOrderHeaderSalesReason)
            Me.ObjectContext.SalesOrderHeaderSalesReasons.AttachAsModified(currentSalesOrderHeaderSalesReason, Me.ChangeSet.GetOriginal(currentSalesOrderHeaderSalesReason))
        End Sub
        
        Public Sub DeleteSalesOrderHeaderSalesReason(ByVal salesOrderHeaderSalesReason As SalesOrderHeaderSalesReason)
            If ((salesOrderHeaderSalesReason.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeaderSalesReason, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesOrderHeaderSalesReasons.Attach(salesOrderHeaderSalesReason)
                Me.ObjectContext.SalesOrderHeaderSalesReasons.DeleteObject(salesOrderHeaderSalesReason)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesPersons' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesPersons() As IQueryable(Of SalesPerson)
            Return Me.ObjectContext.SalesPersons
        End Function
        
        Public Sub InsertSalesPerson(ByVal salesPerson As SalesPerson)
            If ((salesPerson.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesPerson, EntityState.Added)
            Else
                Me.ObjectContext.SalesPersons.AddObject(salesPerson)
            End If
        End Sub
        
        Public Sub UpdateSalesPerson(ByVal currentSalesPerson As SalesPerson)
            Me.ObjectContext.SalesPersons.AttachAsModified(currentSalesPerson, Me.ChangeSet.GetOriginal(currentSalesPerson))
        End Sub
        
        Public Sub DeleteSalesPerson(ByVal salesPerson As SalesPerson)
            If ((salesPerson.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesPerson, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesPersons.Attach(salesPerson)
                Me.ObjectContext.SalesPersons.DeleteObject(salesPerson)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesPersonQuotaHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesPersonQuotaHistories() As IQueryable(Of SalesPersonQuotaHistory)
            Return Me.ObjectContext.SalesPersonQuotaHistories
        End Function
        
        Public Sub InsertSalesPersonQuotaHistory(ByVal salesPersonQuotaHistory As SalesPersonQuotaHistory)
            If ((salesPersonQuotaHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesPersonQuotaHistory, EntityState.Added)
            Else
                Me.ObjectContext.SalesPersonQuotaHistories.AddObject(salesPersonQuotaHistory)
            End If
        End Sub
        
        Public Sub UpdateSalesPersonQuotaHistory(ByVal currentSalesPersonQuotaHistory As SalesPersonQuotaHistory)
            Me.ObjectContext.SalesPersonQuotaHistories.AttachAsModified(currentSalesPersonQuotaHistory, Me.ChangeSet.GetOriginal(currentSalesPersonQuotaHistory))
        End Sub
        
        Public Sub DeleteSalesPersonQuotaHistory(ByVal salesPersonQuotaHistory As SalesPersonQuotaHistory)
            If ((salesPersonQuotaHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesPersonQuotaHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesPersonQuotaHistories.Attach(salesPersonQuotaHistory)
                Me.ObjectContext.SalesPersonQuotaHistories.DeleteObject(salesPersonQuotaHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesReasons' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesReasons() As IQueryable(Of SalesReason)
            Return Me.ObjectContext.SalesReasons
        End Function
        
        Public Sub InsertSalesReason(ByVal salesReason As SalesReason)
            If ((salesReason.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesReason, EntityState.Added)
            Else
                Me.ObjectContext.SalesReasons.AddObject(salesReason)
            End If
        End Sub
        
        Public Sub UpdateSalesReason(ByVal currentSalesReason As SalesReason)
            Me.ObjectContext.SalesReasons.AttachAsModified(currentSalesReason, Me.ChangeSet.GetOriginal(currentSalesReason))
        End Sub
        
        Public Sub DeleteSalesReason(ByVal salesReason As SalesReason)
            If ((salesReason.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesReason, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesReasons.Attach(salesReason)
                Me.ObjectContext.SalesReasons.DeleteObject(salesReason)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesTaxRates' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesTaxRates() As IQueryable(Of SalesTaxRate)
            Return Me.ObjectContext.SalesTaxRates
        End Function
        
        Public Sub InsertSalesTaxRate(ByVal salesTaxRate As SalesTaxRate)
            If ((salesTaxRate.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesTaxRate, EntityState.Added)
            Else
                Me.ObjectContext.SalesTaxRates.AddObject(salesTaxRate)
            End If
        End Sub
        
        Public Sub UpdateSalesTaxRate(ByVal currentSalesTaxRate As SalesTaxRate)
            Me.ObjectContext.SalesTaxRates.AttachAsModified(currentSalesTaxRate, Me.ChangeSet.GetOriginal(currentSalesTaxRate))
        End Sub
        
        Public Sub DeleteSalesTaxRate(ByVal salesTaxRate As SalesTaxRate)
            If ((salesTaxRate.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesTaxRate, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesTaxRates.Attach(salesTaxRate)
                Me.ObjectContext.SalesTaxRates.DeleteObject(salesTaxRate)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesTerritories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesTerritories() As IQueryable(Of SalesTerritory)
            Return Me.ObjectContext.SalesTerritories
        End Function
        
        Public Sub InsertSalesTerritory(ByVal salesTerritory As SalesTerritory)
            If ((salesTerritory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritory, EntityState.Added)
            Else
                Me.ObjectContext.SalesTerritories.AddObject(salesTerritory)
            End If
        End Sub
        
        Public Sub UpdateSalesTerritory(ByVal currentSalesTerritory As SalesTerritory)
            Me.ObjectContext.SalesTerritories.AttachAsModified(currentSalesTerritory, Me.ChangeSet.GetOriginal(currentSalesTerritory))
        End Sub
        
        Public Sub DeleteSalesTerritory(ByVal salesTerritory As SalesTerritory)
            If ((salesTerritory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritory, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesTerritories.Attach(salesTerritory)
                Me.ObjectContext.SalesTerritories.DeleteObject(salesTerritory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SalesTerritoryHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSalesTerritoryHistories() As IQueryable(Of SalesTerritoryHistory)
            Return Me.ObjectContext.SalesTerritoryHistories
        End Function
        
        Public Sub InsertSalesTerritoryHistory(ByVal salesTerritoryHistory As SalesTerritoryHistory)
            If ((salesTerritoryHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritoryHistory, EntityState.Added)
            Else
                Me.ObjectContext.SalesTerritoryHistories.AddObject(salesTerritoryHistory)
            End If
        End Sub
        
        Public Sub UpdateSalesTerritoryHistory(ByVal currentSalesTerritoryHistory As SalesTerritoryHistory)
            Me.ObjectContext.SalesTerritoryHistories.AttachAsModified(currentSalesTerritoryHistory, Me.ChangeSet.GetOriginal(currentSalesTerritoryHistory))
        End Sub
        
        Public Sub DeleteSalesTerritoryHistory(ByVal salesTerritoryHistory As SalesTerritoryHistory)
            If ((salesTerritoryHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritoryHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.SalesTerritoryHistories.Attach(salesTerritoryHistory)
                Me.ObjectContext.SalesTerritoryHistories.DeleteObject(salesTerritoryHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ScrapReasons' query.
        <Query(IsDefault:=true)>  _
        Public Function GetScrapReasons() As IQueryable(Of ScrapReason)
            Return Me.ObjectContext.ScrapReasons
        End Function
        
        Public Sub InsertScrapReason(ByVal scrapReason As ScrapReason)
            If ((scrapReason.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(scrapReason, EntityState.Added)
            Else
                Me.ObjectContext.ScrapReasons.AddObject(scrapReason)
            End If
        End Sub
        
        Public Sub UpdateScrapReason(ByVal currentScrapReason As ScrapReason)
            Me.ObjectContext.ScrapReasons.AttachAsModified(currentScrapReason, Me.ChangeSet.GetOriginal(currentScrapReason))
        End Sub
        
        Public Sub DeleteScrapReason(ByVal scrapReason As ScrapReason)
            If ((scrapReason.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(scrapReason, EntityState.Deleted)
            Else
                Me.ObjectContext.ScrapReasons.Attach(scrapReason)
                Me.ObjectContext.ScrapReasons.DeleteObject(scrapReason)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Shifts' query.
        <Query(IsDefault:=true)>  _
        Public Function GetShifts() As IQueryable(Of Shift)
            Return Me.ObjectContext.Shifts
        End Function
        
        Public Sub InsertShift(ByVal shift As Shift)
            If ((shift.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shift, EntityState.Added)
            Else
                Me.ObjectContext.Shifts.AddObject(shift)
            End If
        End Sub
        
        Public Sub UpdateShift(ByVal currentShift As Shift)
            Me.ObjectContext.Shifts.AttachAsModified(currentShift, Me.ChangeSet.GetOriginal(currentShift))
        End Sub
        
        Public Sub DeleteShift(ByVal shift As Shift)
            If ((shift.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shift, EntityState.Deleted)
            Else
                Me.ObjectContext.Shifts.Attach(shift)
                Me.ObjectContext.Shifts.DeleteObject(shift)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ShipMethods' query.
        <Query(IsDefault:=true)>  _
        Public Function GetShipMethods() As IQueryable(Of ShipMethod)
            Return Me.ObjectContext.ShipMethods
        End Function
        
        Public Sub InsertShipMethod(ByVal shipMethod As ShipMethod)
            If ((shipMethod.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shipMethod, EntityState.Added)
            Else
                Me.ObjectContext.ShipMethods.AddObject(shipMethod)
            End If
        End Sub
        
        Public Sub UpdateShipMethod(ByVal currentShipMethod As ShipMethod)
            Me.ObjectContext.ShipMethods.AttachAsModified(currentShipMethod, Me.ChangeSet.GetOriginal(currentShipMethod))
        End Sub
        
        Public Sub DeleteShipMethod(ByVal shipMethod As ShipMethod)
            If ((shipMethod.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shipMethod, EntityState.Deleted)
            Else
                Me.ObjectContext.ShipMethods.Attach(shipMethod)
                Me.ObjectContext.ShipMethods.DeleteObject(shipMethod)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'ShoppingCartItems' query.
        <Query(IsDefault:=true)>  _
        Public Function GetShoppingCartItems() As IQueryable(Of ShoppingCartItem)
            Return Me.ObjectContext.ShoppingCartItems
        End Function
        
        Public Sub InsertShoppingCartItem(ByVal shoppingCartItem As ShoppingCartItem)
            If ((shoppingCartItem.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shoppingCartItem, EntityState.Added)
            Else
                Me.ObjectContext.ShoppingCartItems.AddObject(shoppingCartItem)
            End If
        End Sub
        
        Public Sub UpdateShoppingCartItem(ByVal currentShoppingCartItem As ShoppingCartItem)
            Me.ObjectContext.ShoppingCartItems.AttachAsModified(currentShoppingCartItem, Me.ChangeSet.GetOriginal(currentShoppingCartItem))
        End Sub
        
        Public Sub DeleteShoppingCartItem(ByVal shoppingCartItem As ShoppingCartItem)
            If ((shoppingCartItem.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(shoppingCartItem, EntityState.Deleted)
            Else
                Me.ObjectContext.ShoppingCartItems.Attach(shoppingCartItem)
                Me.ObjectContext.ShoppingCartItems.DeleteObject(shoppingCartItem)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SpecialOffers' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSpecialOffers() As IQueryable(Of SpecialOffer)
            Return Me.ObjectContext.SpecialOffers
        End Function
        
        Public Sub InsertSpecialOffer(ByVal specialOffer As SpecialOffer)
            If ((specialOffer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(specialOffer, EntityState.Added)
            Else
                Me.ObjectContext.SpecialOffers.AddObject(specialOffer)
            End If
        End Sub
        
        Public Sub UpdateSpecialOffer(ByVal currentSpecialOffer As SpecialOffer)
            Me.ObjectContext.SpecialOffers.AttachAsModified(currentSpecialOffer, Me.ChangeSet.GetOriginal(currentSpecialOffer))
        End Sub
        
        Public Sub DeleteSpecialOffer(ByVal specialOffer As SpecialOffer)
            If ((specialOffer.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(specialOffer, EntityState.Deleted)
            Else
                Me.ObjectContext.SpecialOffers.Attach(specialOffer)
                Me.ObjectContext.SpecialOffers.DeleteObject(specialOffer)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'SpecialOfferProducts' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSpecialOfferProducts() As IQueryable(Of SpecialOfferProduct)
            Return Me.ObjectContext.SpecialOfferProducts
        End Function
        
        Public Sub InsertSpecialOfferProduct(ByVal specialOfferProduct As SpecialOfferProduct)
            If ((specialOfferProduct.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(specialOfferProduct, EntityState.Added)
            Else
                Me.ObjectContext.SpecialOfferProducts.AddObject(specialOfferProduct)
            End If
        End Sub
        
        Public Sub UpdateSpecialOfferProduct(ByVal currentSpecialOfferProduct As SpecialOfferProduct)
            Me.ObjectContext.SpecialOfferProducts.AttachAsModified(currentSpecialOfferProduct, Me.ChangeSet.GetOriginal(currentSpecialOfferProduct))
        End Sub
        
        Public Sub DeleteSpecialOfferProduct(ByVal specialOfferProduct As SpecialOfferProduct)
            If ((specialOfferProduct.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(specialOfferProduct, EntityState.Deleted)
            Else
                Me.ObjectContext.SpecialOfferProducts.Attach(specialOfferProduct)
                Me.ObjectContext.SpecialOfferProducts.DeleteObject(specialOfferProduct)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'StateProvinces' query.
        <Query(IsDefault:=true)>  _
        Public Function GetStateProvinces() As IQueryable(Of StateProvince)
            Return Me.ObjectContext.StateProvinces
        End Function
        
        Public Sub InsertStateProvince(ByVal stateProvince As StateProvince)
            If ((stateProvince.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(stateProvince, EntityState.Added)
            Else
                Me.ObjectContext.StateProvinces.AddObject(stateProvince)
            End If
        End Sub
        
        Public Sub UpdateStateProvince(ByVal currentStateProvince As StateProvince)
            Me.ObjectContext.StateProvinces.AttachAsModified(currentStateProvince, Me.ChangeSet.GetOriginal(currentStateProvince))
        End Sub
        
        Public Sub DeleteStateProvince(ByVal stateProvince As StateProvince)
            If ((stateProvince.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(stateProvince, EntityState.Deleted)
            Else
                Me.ObjectContext.StateProvinces.Attach(stateProvince)
                Me.ObjectContext.StateProvinces.DeleteObject(stateProvince)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Stores' query.
        <Query(IsDefault:=true)>  _
        Public Function GetStores() As IQueryable(Of Store)
            Return Me.ObjectContext.Stores
        End Function
        
        Public Sub InsertStore(ByVal store As Store)
            If ((store.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(store, EntityState.Added)
            Else
                Me.ObjectContext.Stores.AddObject(store)
            End If
        End Sub
        
        Public Sub UpdateStore(ByVal currentStore As Store)
            Me.ObjectContext.Stores.AttachAsModified(currentStore, Me.ChangeSet.GetOriginal(currentStore))
        End Sub
        
        Public Sub DeleteStore(ByVal store As Store)
            If ((store.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(store, EntityState.Deleted)
            Else
                Me.ObjectContext.Stores.Attach(store)
                Me.ObjectContext.Stores.DeleteObject(store)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'StoreContacts' query.
        <Query(IsDefault:=true)>  _
        Public Function GetStoreContacts() As IQueryable(Of StoreContact)
            Return Me.ObjectContext.StoreContacts
        End Function
        
        Public Sub InsertStoreContact(ByVal storeContact As StoreContact)
            If ((storeContact.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(storeContact, EntityState.Added)
            Else
                Me.ObjectContext.StoreContacts.AddObject(storeContact)
            End If
        End Sub
        
        Public Sub UpdateStoreContact(ByVal currentStoreContact As StoreContact)
            Me.ObjectContext.StoreContacts.AttachAsModified(currentStoreContact, Me.ChangeSet.GetOriginal(currentStoreContact))
        End Sub
        
        Public Sub DeleteStoreContact(ByVal storeContact As StoreContact)
            If ((storeContact.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(storeContact, EntityState.Deleted)
            Else
                Me.ObjectContext.StoreContacts.Attach(storeContact)
                Me.ObjectContext.StoreContacts.DeleteObject(storeContact)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'sysdiagrams' query.
        <Query(IsDefault:=true)>  _
        Public Function GetSysdiagrams() As IQueryable(Of sysdiagram)
            Return Me.ObjectContext.sysdiagrams
        End Function
        
        Public Sub InsertSysdiagram(ByVal sysdiagram As sysdiagram)
            If ((sysdiagram.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(sysdiagram, EntityState.Added)
            Else
                Me.ObjectContext.sysdiagrams.AddObject(sysdiagram)
            End If
        End Sub
        
        Public Sub UpdateSysdiagram(ByVal currentsysdiagram As sysdiagram)
            Me.ObjectContext.sysdiagrams.AttachAsModified(currentsysdiagram, Me.ChangeSet.GetOriginal(currentsysdiagram))
        End Sub
        
        Public Sub DeleteSysdiagram(ByVal sysdiagram As sysdiagram)
            If ((sysdiagram.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(sysdiagram, EntityState.Deleted)
            Else
                Me.ObjectContext.sysdiagrams.Attach(sysdiagram)
                Me.ObjectContext.sysdiagrams.DeleteObject(sysdiagram)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'TransactionHistories' query.
        <Query(IsDefault:=true)>  _
        Public Function GetTransactionHistories() As IQueryable(Of TransactionHistory)
            Return Me.ObjectContext.TransactionHistories
        End Function
        
        Public Sub InsertTransactionHistory(ByVal transactionHistory As TransactionHistory)
            If ((transactionHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistory, EntityState.Added)
            Else
                Me.ObjectContext.TransactionHistories.AddObject(transactionHistory)
            End If
        End Sub
        
        Public Sub UpdateTransactionHistory(ByVal currentTransactionHistory As TransactionHistory)
            Me.ObjectContext.TransactionHistories.AttachAsModified(currentTransactionHistory, Me.ChangeSet.GetOriginal(currentTransactionHistory))
        End Sub
        
        Public Sub DeleteTransactionHistory(ByVal transactionHistory As TransactionHistory)
            If ((transactionHistory.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistory, EntityState.Deleted)
            Else
                Me.ObjectContext.TransactionHistories.Attach(transactionHistory)
                Me.ObjectContext.TransactionHistories.DeleteObject(transactionHistory)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'TransactionHistoryArchives' query.
        <Query(IsDefault:=true)>  _
        Public Function GetTransactionHistoryArchives() As IQueryable(Of TransactionHistoryArchive)
            Return Me.ObjectContext.TransactionHistoryArchives
        End Function
        
        Public Sub InsertTransactionHistoryArchive(ByVal transactionHistoryArchive As TransactionHistoryArchive)
            If ((transactionHistoryArchive.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistoryArchive, EntityState.Added)
            Else
                Me.ObjectContext.TransactionHistoryArchives.AddObject(transactionHistoryArchive)
            End If
        End Sub
        
        Public Sub UpdateTransactionHistoryArchive(ByVal currentTransactionHistoryArchive As TransactionHistoryArchive)
            Me.ObjectContext.TransactionHistoryArchives.AttachAsModified(currentTransactionHistoryArchive, Me.ChangeSet.GetOriginal(currentTransactionHistoryArchive))
        End Sub
        
        Public Sub DeleteTransactionHistoryArchive(ByVal transactionHistoryArchive As TransactionHistoryArchive)
            If ((transactionHistoryArchive.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistoryArchive, EntityState.Deleted)
            Else
                Me.ObjectContext.TransactionHistoryArchives.Attach(transactionHistoryArchive)
                Me.ObjectContext.TransactionHistoryArchives.DeleteObject(transactionHistoryArchive)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'UnitMeasures' query.
        <Query(IsDefault:=true)>  _
        Public Function GetUnitMeasures() As IQueryable(Of UnitMeasure)
            Return Me.ObjectContext.UnitMeasures
        End Function
        
        Public Sub InsertUnitMeasure(ByVal unitMeasure As UnitMeasure)
            If ((unitMeasure.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(unitMeasure, EntityState.Added)
            Else
                Me.ObjectContext.UnitMeasures.AddObject(unitMeasure)
            End If
        End Sub
        
        Public Sub UpdateUnitMeasure(ByVal currentUnitMeasure As UnitMeasure)
            Me.ObjectContext.UnitMeasures.AttachAsModified(currentUnitMeasure, Me.ChangeSet.GetOriginal(currentUnitMeasure))
        End Sub
        
        Public Sub DeleteUnitMeasure(ByVal unitMeasure As UnitMeasure)
            If ((unitMeasure.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(unitMeasure, EntityState.Deleted)
            Else
                Me.ObjectContext.UnitMeasures.Attach(unitMeasure)
                Me.ObjectContext.UnitMeasures.DeleteObject(unitMeasure)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'Vendors' query.
        <Query(IsDefault:=true)>  _
        Public Function GetVendors() As IQueryable(Of Vendor)
            Return Me.ObjectContext.Vendors
        End Function
        
        Public Sub InsertVendor(ByVal vendor As Vendor)
            If ((vendor.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(vendor, EntityState.Added)
            Else
                Me.ObjectContext.Vendors.AddObject(vendor)
            End If
        End Sub
        
        Public Sub UpdateVendor(ByVal currentVendor As Vendor)
            Me.ObjectContext.Vendors.AttachAsModified(currentVendor, Me.ChangeSet.GetOriginal(currentVendor))
        End Sub
        
        Public Sub DeleteVendor(ByVal vendor As Vendor)
            If ((vendor.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(vendor, EntityState.Deleted)
            Else
                Me.ObjectContext.Vendors.Attach(vendor)
                Me.ObjectContext.Vendors.DeleteObject(vendor)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'VendorAddresses' query.
        <Query(IsDefault:=true)>  _
        Public Function GetVendorAddresses() As IQueryable(Of VendorAddress)
            Return Me.ObjectContext.VendorAddresses
        End Function
        
        Public Sub InsertVendorAddress(ByVal vendorAddress As VendorAddress)
            If ((vendorAddress.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(vendorAddress, EntityState.Added)
            Else
                Me.ObjectContext.VendorAddresses.AddObject(vendorAddress)
            End If
        End Sub
        
        Public Sub UpdateVendorAddress(ByVal currentVendorAddress As VendorAddress)
            Me.ObjectContext.VendorAddresses.AttachAsModified(currentVendorAddress, Me.ChangeSet.GetOriginal(currentVendorAddress))
        End Sub
        
        Public Sub DeleteVendorAddress(ByVal vendorAddress As VendorAddress)
            If ((vendorAddress.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(vendorAddress, EntityState.Deleted)
            Else
                Me.ObjectContext.VendorAddresses.Attach(vendorAddress)
                Me.ObjectContext.VendorAddresses.DeleteObject(vendorAddress)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'VendorContacts' query.
        <Query(IsDefault:=true)>  _
        Public Function GetVendorContacts() As IQueryable(Of VendorContact)
            Return Me.ObjectContext.VendorContacts
        End Function
        
        Public Sub InsertVendorContact(ByVal vendorContact As VendorContact)
            If ((vendorContact.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(vendorContact, EntityState.Added)
            Else
                Me.ObjectContext.VendorContacts.AddObject(vendorContact)
            End If
        End Sub
        
        Public Sub UpdateVendorContact(ByVal currentVendorContact As VendorContact)
            Me.ObjectContext.VendorContacts.AttachAsModified(currentVendorContact, Me.ChangeSet.GetOriginal(currentVendorContact))
        End Sub
        
        Public Sub DeleteVendorContact(ByVal vendorContact As VendorContact)
            If ((vendorContact.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(vendorContact, EntityState.Deleted)
            Else
                Me.ObjectContext.VendorContacts.Attach(vendorContact)
                Me.ObjectContext.VendorContacts.DeleteObject(vendorContact)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'WorkOrders' query.
        <Query(IsDefault:=true)>  _
        Public Function GetWorkOrders() As IQueryable(Of WorkOrder)
            Return Me.ObjectContext.WorkOrders
        End Function
        
        Public Sub InsertWorkOrder(ByVal workOrder As WorkOrder)
            If ((workOrder.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(workOrder, EntityState.Added)
            Else
                Me.ObjectContext.WorkOrders.AddObject(workOrder)
            End If
        End Sub
        
        Public Sub UpdateWorkOrder(ByVal currentWorkOrder As WorkOrder)
            Me.ObjectContext.WorkOrders.AttachAsModified(currentWorkOrder, Me.ChangeSet.GetOriginal(currentWorkOrder))
        End Sub
        
        Public Sub DeleteWorkOrder(ByVal workOrder As WorkOrder)
            If ((workOrder.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(workOrder, EntityState.Deleted)
            Else
                Me.ObjectContext.WorkOrders.Attach(workOrder)
                Me.ObjectContext.WorkOrders.DeleteObject(workOrder)
            End If
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        'To support paging you will need to add ordering to the 'WorkOrderRoutings' query.
        <Query(IsDefault:=true)>  _
        Public Function GetWorkOrderRoutings() As IQueryable(Of WorkOrderRouting)
            Return Me.ObjectContext.WorkOrderRoutings
        End Function
        
        Public Sub InsertWorkOrderRouting(ByVal workOrderRouting As WorkOrderRouting)
            If ((workOrderRouting.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(workOrderRouting, EntityState.Added)
            Else
                Me.ObjectContext.WorkOrderRoutings.AddObject(workOrderRouting)
            End If
        End Sub
        
        Public Sub UpdateWorkOrderRouting(ByVal currentWorkOrderRouting As WorkOrderRouting)
            Me.ObjectContext.WorkOrderRoutings.AttachAsModified(currentWorkOrderRouting, Me.ChangeSet.GetOriginal(currentWorkOrderRouting))
        End Sub
        
        Public Sub DeleteWorkOrderRouting(ByVal workOrderRouting As WorkOrderRouting)
            If ((workOrderRouting.EntityState = EntityState.Detached)  _
                        = false) Then
                Me.ObjectContext.ObjectStateManager.ChangeObjectState(workOrderRouting, EntityState.Deleted)
            Else
                Me.ObjectContext.WorkOrderRoutings.Attach(workOrderRouting)
                Me.ObjectContext.WorkOrderRoutings.DeleteObject(workOrderRouting)
            End If
        End Sub
    End Class
End Namespace
