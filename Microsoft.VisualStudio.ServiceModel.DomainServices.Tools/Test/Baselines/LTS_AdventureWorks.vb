
Option Compare Binary
Option Infer On
Option Strict On
Option Explicit On

Imports DataTests.AdventureWorks.LTS
Imports Microsoft.ServiceModel.DomainServices.LinqToSql
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.ComponentModel.DataAnnotations
Imports System.Data.Linq
Imports System.Linq
Imports System.ServiceModel.DomainServices.Hosting
Imports System.ServiceModel.DomainServices.Server

Namespace BizLogic.Test
    
    'Implements application logic using the AdventureWorks context.
    ' TODO: Add your application logic to these methods or in additional methods.
    ' TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    ' Also consider adding roles to restrict access as appropriate.
    '<RequiresAuthentication> _
    <EnableClientAccess()>  _
    Public Class LTS_AdventureWorks
        Inherits LinqToSqlDomainService(Of AdventureWorks)
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetAddresses() As IQueryable(Of Address)
            Return Me.DataContext.Addresses
        End Function
        
        Public Sub InsertAddress(ByVal address As Address)
            Me.DataContext.Addresses.InsertOnSubmit(address)
        End Sub
        
        Public Sub UpdateAddress(ByVal currentAddress As Address)
            Me.DataContext.Addresses.Attach(currentAddress, Me.ChangeSet.GetOriginal(currentAddress))
        End Sub
        
        Public Sub DeleteAddress(ByVal address As Address)
            Me.DataContext.Addresses.Attach(address)
            Me.DataContext.Addresses.DeleteOnSubmit(address)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetAddressTypes() As IQueryable(Of AddressType)
            Return Me.DataContext.AddressTypes
        End Function
        
        Public Sub InsertAddressType(ByVal addressType As AddressType)
            Me.DataContext.AddressTypes.InsertOnSubmit(addressType)
        End Sub
        
        Public Sub UpdateAddressType(ByVal currentAddressType As AddressType)
            Me.DataContext.AddressTypes.Attach(currentAddressType, Me.ChangeSet.GetOriginal(currentAddressType))
        End Sub
        
        Public Sub DeleteAddressType(ByVal addressType As AddressType)
            Me.DataContext.AddressTypes.Attach(addressType)
            Me.DataContext.AddressTypes.DeleteOnSubmit(addressType)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetBillOfMaterials() As IQueryable(Of BillOfMaterial)
            Return Me.DataContext.BillOfMaterials
        End Function
        
        Public Sub InsertBillOfMaterial(ByVal billOfMaterial As BillOfMaterial)
            Me.DataContext.BillOfMaterials.InsertOnSubmit(billOfMaterial)
        End Sub
        
        Public Sub UpdateBillOfMaterial(ByVal currentBillOfMaterial As BillOfMaterial)
            Me.DataContext.BillOfMaterials.Attach(currentBillOfMaterial, Me.ChangeSet.GetOriginal(currentBillOfMaterial))
        End Sub
        
        Public Sub DeleteBillOfMaterial(ByVal billOfMaterial As BillOfMaterial)
            Me.DataContext.BillOfMaterials.Attach(billOfMaterial)
            Me.DataContext.BillOfMaterials.DeleteOnSubmit(billOfMaterial)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetContacts() As IQueryable(Of Contact)
            Return Me.DataContext.Contacts
        End Function
        
        Public Sub InsertContact(ByVal contact As Contact)
            Me.DataContext.Contacts.InsertOnSubmit(contact)
        End Sub
        
        Public Sub UpdateContact(ByVal currentContact As Contact)
            Me.DataContext.Contacts.Attach(currentContact, Me.ChangeSet.GetOriginal(currentContact))
        End Sub
        
        Public Sub DeleteContact(ByVal contact As Contact)
            Me.DataContext.Contacts.Attach(contact)
            Me.DataContext.Contacts.DeleteOnSubmit(contact)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetContactCreditCards() As IQueryable(Of ContactCreditCard)
            Return Me.DataContext.ContactCreditCards
        End Function
        
        Public Sub InsertContactCreditCard(ByVal contactCreditCard As ContactCreditCard)
            Me.DataContext.ContactCreditCards.InsertOnSubmit(contactCreditCard)
        End Sub
        
        Public Sub UpdateContactCreditCard(ByVal currentContactCreditCard As ContactCreditCard)
            Me.DataContext.ContactCreditCards.Attach(currentContactCreditCard, Me.ChangeSet.GetOriginal(currentContactCreditCard))
        End Sub
        
        Public Sub DeleteContactCreditCard(ByVal contactCreditCard As ContactCreditCard)
            Me.DataContext.ContactCreditCards.Attach(contactCreditCard)
            Me.DataContext.ContactCreditCards.DeleteOnSubmit(contactCreditCard)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetContactTypes() As IQueryable(Of ContactType)
            Return Me.DataContext.ContactTypes
        End Function
        
        Public Sub InsertContactType(ByVal contactType As ContactType)
            Me.DataContext.ContactTypes.InsertOnSubmit(contactType)
        End Sub
        
        Public Sub UpdateContactType(ByVal currentContactType As ContactType)
            Me.DataContext.ContactTypes.Attach(currentContactType, Me.ChangeSet.GetOriginal(currentContactType))
        End Sub
        
        Public Sub DeleteContactType(ByVal contactType As ContactType)
            Me.DataContext.ContactTypes.Attach(contactType)
            Me.DataContext.ContactTypes.DeleteOnSubmit(contactType)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCountryRegions() As IQueryable(Of CountryRegion)
            Return Me.DataContext.CountryRegions
        End Function
        
        Public Sub InsertCountryRegion(ByVal countryRegion As CountryRegion)
            Me.DataContext.CountryRegions.InsertOnSubmit(countryRegion)
        End Sub
        
        Public Sub UpdateCountryRegion(ByVal currentCountryRegion As CountryRegion)
            Me.DataContext.CountryRegions.Attach(currentCountryRegion, Me.ChangeSet.GetOriginal(currentCountryRegion))
        End Sub
        
        Public Sub DeleteCountryRegion(ByVal countryRegion As CountryRegion)
            Me.DataContext.CountryRegions.Attach(countryRegion)
            Me.DataContext.CountryRegions.DeleteOnSubmit(countryRegion)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCountryRegionCurrencies() As IQueryable(Of CountryRegionCurrency)
            Return Me.DataContext.CountryRegionCurrencies
        End Function
        
        Public Sub InsertCountryRegionCurrency(ByVal countryRegionCurrency As CountryRegionCurrency)
            Me.DataContext.CountryRegionCurrencies.InsertOnSubmit(countryRegionCurrency)
        End Sub
        
        Public Sub UpdateCountryRegionCurrency(ByVal currentCountryRegionCurrency As CountryRegionCurrency)
            Me.DataContext.CountryRegionCurrencies.Attach(currentCountryRegionCurrency, Me.ChangeSet.GetOriginal(currentCountryRegionCurrency))
        End Sub
        
        Public Sub DeleteCountryRegionCurrency(ByVal countryRegionCurrency As CountryRegionCurrency)
            Me.DataContext.CountryRegionCurrencies.Attach(countryRegionCurrency)
            Me.DataContext.CountryRegionCurrencies.DeleteOnSubmit(countryRegionCurrency)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCreditCards() As IQueryable(Of CreditCard)
            Return Me.DataContext.CreditCards
        End Function
        
        Public Sub InsertCreditCard(ByVal creditCard As CreditCard)
            Me.DataContext.CreditCards.InsertOnSubmit(creditCard)
        End Sub
        
        Public Sub UpdateCreditCard(ByVal currentCreditCard As CreditCard)
            Me.DataContext.CreditCards.Attach(currentCreditCard, Me.ChangeSet.GetOriginal(currentCreditCard))
        End Sub
        
        Public Sub DeleteCreditCard(ByVal creditCard As CreditCard)
            Me.DataContext.CreditCards.Attach(creditCard)
            Me.DataContext.CreditCards.DeleteOnSubmit(creditCard)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCultures() As IQueryable(Of Culture)
            Return Me.DataContext.Cultures
        End Function
        
        Public Sub InsertCulture(ByVal culture As Culture)
            Me.DataContext.Cultures.InsertOnSubmit(culture)
        End Sub
        
        Public Sub UpdateCulture(ByVal currentCulture As Culture)
            Me.DataContext.Cultures.Attach(currentCulture, Me.ChangeSet.GetOriginal(currentCulture))
        End Sub
        
        Public Sub DeleteCulture(ByVal culture As Culture)
            Me.DataContext.Cultures.Attach(culture)
            Me.DataContext.Cultures.DeleteOnSubmit(culture)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCurrencies() As IQueryable(Of Currency)
            Return Me.DataContext.Currencies
        End Function
        
        Public Sub InsertCurrency(ByVal currency As Currency)
            Me.DataContext.Currencies.InsertOnSubmit(currency)
        End Sub
        
        Public Sub UpdateCurrency(ByVal currentCurrency As Currency)
            Me.DataContext.Currencies.Attach(currentCurrency, Me.ChangeSet.GetOriginal(currentCurrency))
        End Sub
        
        Public Sub DeleteCurrency(ByVal currency As Currency)
            Me.DataContext.Currencies.Attach(currency)
            Me.DataContext.Currencies.DeleteOnSubmit(currency)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCurrencyRates() As IQueryable(Of CurrencyRate)
            Return Me.DataContext.CurrencyRates
        End Function
        
        Public Sub InsertCurrencyRate(ByVal currencyRate As CurrencyRate)
            Me.DataContext.CurrencyRates.InsertOnSubmit(currencyRate)
        End Sub
        
        Public Sub UpdateCurrencyRate(ByVal currentCurrencyRate As CurrencyRate)
            Me.DataContext.CurrencyRates.Attach(currentCurrencyRate, Me.ChangeSet.GetOriginal(currentCurrencyRate))
        End Sub
        
        Public Sub DeleteCurrencyRate(ByVal currencyRate As CurrencyRate)
            Me.DataContext.CurrencyRates.Attach(currencyRate)
            Me.DataContext.CurrencyRates.DeleteOnSubmit(currencyRate)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCustomers() As IQueryable(Of Customer)
            Return Me.DataContext.Customers
        End Function
        
        Public Sub InsertCustomer(ByVal customer As Customer)
            Me.DataContext.Customers.InsertOnSubmit(customer)
        End Sub
        
        Public Sub UpdateCustomer(ByVal currentCustomer As Customer)
            Me.DataContext.Customers.Attach(currentCustomer, Me.ChangeSet.GetOriginal(currentCustomer))
        End Sub
        
        Public Sub DeleteCustomer(ByVal customer As Customer)
            Me.DataContext.Customers.Attach(customer)
            Me.DataContext.Customers.DeleteOnSubmit(customer)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetCustomerAddresses() As IQueryable(Of CustomerAddress)
            Return Me.DataContext.CustomerAddresses
        End Function
        
        Public Sub InsertCustomerAddress(ByVal customerAddress As CustomerAddress)
            Me.DataContext.CustomerAddresses.InsertOnSubmit(customerAddress)
        End Sub
        
        Public Sub UpdateCustomerAddress(ByVal currentCustomerAddress As CustomerAddress)
            Me.DataContext.CustomerAddresses.Attach(currentCustomerAddress, Me.ChangeSet.GetOriginal(currentCustomerAddress))
        End Sub
        
        Public Sub DeleteCustomerAddress(ByVal customerAddress As CustomerAddress)
            Me.DataContext.CustomerAddresses.Attach(customerAddress)
            Me.DataContext.CustomerAddresses.DeleteOnSubmit(customerAddress)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetDepartments() As IQueryable(Of Department)
            Return Me.DataContext.Departments
        End Function
        
        Public Sub InsertDepartment(ByVal department As Department)
            Me.DataContext.Departments.InsertOnSubmit(department)
        End Sub
        
        Public Sub UpdateDepartment(ByVal currentDepartment As Department)
            Me.DataContext.Departments.Attach(currentDepartment, Me.ChangeSet.GetOriginal(currentDepartment))
        End Sub
        
        Public Sub DeleteDepartment(ByVal department As Department)
            Me.DataContext.Departments.Attach(department)
            Me.DataContext.Departments.DeleteOnSubmit(department)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetDocuments() As IQueryable(Of Document)
            Return Me.DataContext.Documents
        End Function
        
        Public Sub InsertDocument(ByVal document As Document)
            Me.DataContext.Documents.InsertOnSubmit(document)
        End Sub
        
        Public Sub UpdateDocument(ByVal currentDocument As Document)
            Me.DataContext.Documents.Attach(currentDocument, Me.ChangeSet.GetOriginal(currentDocument))
        End Sub
        
        Public Sub DeleteDocument(ByVal document As Document)
            Me.DataContext.Documents.Attach(document)
            Me.DataContext.Documents.DeleteOnSubmit(document)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEmployees() As IQueryable(Of Employee)
            Return Me.DataContext.Employees
        End Function
        
        Public Sub InsertEmployee(ByVal employee As Employee)
            Me.DataContext.Employees.InsertOnSubmit(employee)
        End Sub
        
        Public Sub UpdateEmployee(ByVal currentEmployee As Employee)
            Me.DataContext.Employees.Attach(currentEmployee, Me.ChangeSet.GetOriginal(currentEmployee))
        End Sub
        
        Public Sub DeleteEmployee(ByVal employee As Employee)
            Me.DataContext.Employees.Attach(employee)
            Me.DataContext.Employees.DeleteOnSubmit(employee)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEmployeeAddresses() As IQueryable(Of EmployeeAddress)
            Return Me.DataContext.EmployeeAddresses
        End Function
        
        Public Sub InsertEmployeeAddress(ByVal employeeAddress As EmployeeAddress)
            Me.DataContext.EmployeeAddresses.InsertOnSubmit(employeeAddress)
        End Sub
        
        Public Sub UpdateEmployeeAddress(ByVal currentEmployeeAddress As EmployeeAddress)
            Me.DataContext.EmployeeAddresses.Attach(currentEmployeeAddress, Me.ChangeSet.GetOriginal(currentEmployeeAddress))
        End Sub
        
        Public Sub DeleteEmployeeAddress(ByVal employeeAddress As EmployeeAddress)
            Me.DataContext.EmployeeAddresses.Attach(employeeAddress)
            Me.DataContext.EmployeeAddresses.DeleteOnSubmit(employeeAddress)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEmployeeDepartmentHistories() As IQueryable(Of EmployeeDepartmentHistory)
            Return Me.DataContext.EmployeeDepartmentHistories
        End Function
        
        Public Sub InsertEmployeeDepartmentHistory(ByVal employeeDepartmentHistory As EmployeeDepartmentHistory)
            Me.DataContext.EmployeeDepartmentHistories.InsertOnSubmit(employeeDepartmentHistory)
        End Sub
        
        Public Sub UpdateEmployeeDepartmentHistory(ByVal currentEmployeeDepartmentHistory As EmployeeDepartmentHistory)
            Me.DataContext.EmployeeDepartmentHistories.Attach(currentEmployeeDepartmentHistory, Me.ChangeSet.GetOriginal(currentEmployeeDepartmentHistory))
        End Sub
        
        Public Sub DeleteEmployeeDepartmentHistory(ByVal employeeDepartmentHistory As EmployeeDepartmentHistory)
            Me.DataContext.EmployeeDepartmentHistories.Attach(employeeDepartmentHistory)
            Me.DataContext.EmployeeDepartmentHistories.DeleteOnSubmit(employeeDepartmentHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetEmployeePayHistories() As IQueryable(Of EmployeePayHistory)
            Return Me.DataContext.EmployeePayHistories
        End Function
        
        Public Sub InsertEmployeePayHistory(ByVal employeePayHistory As EmployeePayHistory)
            Me.DataContext.EmployeePayHistories.InsertOnSubmit(employeePayHistory)
        End Sub
        
        Public Sub UpdateEmployeePayHistory(ByVal currentEmployeePayHistory As EmployeePayHistory)
            Me.DataContext.EmployeePayHistories.Attach(currentEmployeePayHistory, Me.ChangeSet.GetOriginal(currentEmployeePayHistory))
        End Sub
        
        Public Sub DeleteEmployeePayHistory(ByVal employeePayHistory As EmployeePayHistory)
            Me.DataContext.EmployeePayHistories.Attach(employeePayHistory)
            Me.DataContext.EmployeePayHistories.DeleteOnSubmit(employeePayHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetIllustrations() As IQueryable(Of Illustration)
            Return Me.DataContext.Illustrations
        End Function
        
        Public Sub InsertIllustration(ByVal illustration As Illustration)
            Me.DataContext.Illustrations.InsertOnSubmit(illustration)
        End Sub
        
        Public Sub UpdateIllustration(ByVal currentIllustration As Illustration)
            Me.DataContext.Illustrations.Attach(currentIllustration, Me.ChangeSet.GetOriginal(currentIllustration))
        End Sub
        
        Public Sub DeleteIllustration(ByVal illustration As Illustration)
            Me.DataContext.Illustrations.Attach(illustration)
            Me.DataContext.Illustrations.DeleteOnSubmit(illustration)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetIndividuals() As IQueryable(Of Individual)
            Return Me.DataContext.Individuals
        End Function
        
        Public Sub InsertIndividual(ByVal individual As Individual)
            Me.DataContext.Individuals.InsertOnSubmit(individual)
        End Sub
        
        Public Sub UpdateIndividual(ByVal currentIndividual As Individual)
            Me.DataContext.Individuals.Attach(currentIndividual, Me.ChangeSet.GetOriginal(currentIndividual))
        End Sub
        
        Public Sub DeleteIndividual(ByVal individual As Individual)
            Me.DataContext.Individuals.Attach(individual)
            Me.DataContext.Individuals.DeleteOnSubmit(individual)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetJobCandidates() As IQueryable(Of JobCandidate)
            Return Me.DataContext.JobCandidates
        End Function
        
        Public Sub InsertJobCandidate(ByVal jobCandidate As JobCandidate)
            Me.DataContext.JobCandidates.InsertOnSubmit(jobCandidate)
        End Sub
        
        Public Sub UpdateJobCandidate(ByVal currentJobCandidate As JobCandidate)
            Me.DataContext.JobCandidates.Attach(currentJobCandidate, Me.ChangeSet.GetOriginal(currentJobCandidate))
        End Sub
        
        Public Sub DeleteJobCandidate(ByVal jobCandidate As JobCandidate)
            Me.DataContext.JobCandidates.Attach(jobCandidate)
            Me.DataContext.JobCandidates.DeleteOnSubmit(jobCandidate)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetLocations() As IQueryable(Of Location)
            Return Me.DataContext.Locations
        End Function
        
        Public Sub InsertLocation(ByVal location As Location)
            Me.DataContext.Locations.InsertOnSubmit(location)
        End Sub
        
        Public Sub UpdateLocation(ByVal currentLocation As Location)
            Me.DataContext.Locations.Attach(currentLocation, Me.ChangeSet.GetOriginal(currentLocation))
        End Sub
        
        Public Sub DeleteLocation(ByVal location As Location)
            Me.DataContext.Locations.Attach(location)
            Me.DataContext.Locations.DeleteOnSubmit(location)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProducts() As IQueryable(Of Product)
            Return Me.DataContext.Products
        End Function
        
        Public Sub InsertProduct(ByVal product As Product)
            Me.DataContext.Products.InsertOnSubmit(product)
        End Sub
        
        Public Sub UpdateProduct(ByVal currentProduct As Product)
            Me.DataContext.Products.Attach(currentProduct, Me.ChangeSet.GetOriginal(currentProduct))
        End Sub
        
        Public Sub DeleteProduct(ByVal product As Product)
            Me.DataContext.Products.Attach(product)
            Me.DataContext.Products.DeleteOnSubmit(product)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductCategories() As IQueryable(Of ProductCategory)
            Return Me.DataContext.ProductCategories
        End Function
        
        Public Sub InsertProductCategory(ByVal productCategory As ProductCategory)
            Me.DataContext.ProductCategories.InsertOnSubmit(productCategory)
        End Sub
        
        Public Sub UpdateProductCategory(ByVal currentProductCategory As ProductCategory)
            Me.DataContext.ProductCategories.Attach(currentProductCategory, Me.ChangeSet.GetOriginal(currentProductCategory))
        End Sub
        
        Public Sub DeleteProductCategory(ByVal productCategory As ProductCategory)
            Me.DataContext.ProductCategories.Attach(productCategory)
            Me.DataContext.ProductCategories.DeleteOnSubmit(productCategory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductCostHistories() As IQueryable(Of ProductCostHistory)
            Return Me.DataContext.ProductCostHistories
        End Function
        
        Public Sub InsertProductCostHistory(ByVal productCostHistory As ProductCostHistory)
            Me.DataContext.ProductCostHistories.InsertOnSubmit(productCostHistory)
        End Sub
        
        Public Sub UpdateProductCostHistory(ByVal currentProductCostHistory As ProductCostHistory)
            Me.DataContext.ProductCostHistories.Attach(currentProductCostHistory, Me.ChangeSet.GetOriginal(currentProductCostHistory))
        End Sub
        
        Public Sub DeleteProductCostHistory(ByVal productCostHistory As ProductCostHistory)
            Me.DataContext.ProductCostHistories.Attach(productCostHistory)
            Me.DataContext.ProductCostHistories.DeleteOnSubmit(productCostHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductDescriptions() As IQueryable(Of ProductDescription)
            Return Me.DataContext.ProductDescriptions
        End Function
        
        Public Sub InsertProductDescription(ByVal productDescription As ProductDescription)
            Me.DataContext.ProductDescriptions.InsertOnSubmit(productDescription)
        End Sub
        
        Public Sub UpdateProductDescription(ByVal currentProductDescription As ProductDescription)
            Me.DataContext.ProductDescriptions.Attach(currentProductDescription, Me.ChangeSet.GetOriginal(currentProductDescription))
        End Sub
        
        Public Sub DeleteProductDescription(ByVal productDescription As ProductDescription)
            Me.DataContext.ProductDescriptions.Attach(productDescription)
            Me.DataContext.ProductDescriptions.DeleteOnSubmit(productDescription)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductDocuments() As IQueryable(Of ProductDocument)
            Return Me.DataContext.ProductDocuments
        End Function
        
        Public Sub InsertProductDocument(ByVal productDocument As ProductDocument)
            Me.DataContext.ProductDocuments.InsertOnSubmit(productDocument)
        End Sub
        
        Public Sub UpdateProductDocument(ByVal currentProductDocument As ProductDocument)
            Me.DataContext.ProductDocuments.Attach(currentProductDocument, Me.ChangeSet.GetOriginal(currentProductDocument))
        End Sub
        
        Public Sub DeleteProductDocument(ByVal productDocument As ProductDocument)
            Me.DataContext.ProductDocuments.Attach(productDocument)
            Me.DataContext.ProductDocuments.DeleteOnSubmit(productDocument)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductInventories() As IQueryable(Of ProductInventory)
            Return Me.DataContext.ProductInventories
        End Function
        
        Public Sub InsertProductInventory(ByVal productInventory As ProductInventory)
            Me.DataContext.ProductInventories.InsertOnSubmit(productInventory)
        End Sub
        
        Public Sub UpdateProductInventory(ByVal currentProductInventory As ProductInventory)
            Me.DataContext.ProductInventories.Attach(currentProductInventory, Me.ChangeSet.GetOriginal(currentProductInventory))
        End Sub
        
        Public Sub DeleteProductInventory(ByVal productInventory As ProductInventory)
            Me.DataContext.ProductInventories.Attach(productInventory)
            Me.DataContext.ProductInventories.DeleteOnSubmit(productInventory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductListPriceHistories() As IQueryable(Of ProductListPriceHistory)
            Return Me.DataContext.ProductListPriceHistories
        End Function
        
        Public Sub InsertProductListPriceHistory(ByVal productListPriceHistory As ProductListPriceHistory)
            Me.DataContext.ProductListPriceHistories.InsertOnSubmit(productListPriceHistory)
        End Sub
        
        Public Sub UpdateProductListPriceHistory(ByVal currentProductListPriceHistory As ProductListPriceHistory)
            Me.DataContext.ProductListPriceHistories.Attach(currentProductListPriceHistory, Me.ChangeSet.GetOriginal(currentProductListPriceHistory))
        End Sub
        
        Public Sub DeleteProductListPriceHistory(ByVal productListPriceHistory As ProductListPriceHistory)
            Me.DataContext.ProductListPriceHistories.Attach(productListPriceHistory)
            Me.DataContext.ProductListPriceHistories.DeleteOnSubmit(productListPriceHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductModels() As IQueryable(Of ProductModel)
            Return Me.DataContext.ProductModels
        End Function
        
        Public Sub InsertProductModel(ByVal productModel As ProductModel)
            Me.DataContext.ProductModels.InsertOnSubmit(productModel)
        End Sub
        
        Public Sub UpdateProductModel(ByVal currentProductModel As ProductModel)
            Me.DataContext.ProductModels.Attach(currentProductModel, Me.ChangeSet.GetOriginal(currentProductModel))
        End Sub
        
        Public Sub DeleteProductModel(ByVal productModel As ProductModel)
            Me.DataContext.ProductModels.Attach(productModel)
            Me.DataContext.ProductModels.DeleteOnSubmit(productModel)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductModelIllustrations() As IQueryable(Of ProductModelIllustration)
            Return Me.DataContext.ProductModelIllustrations
        End Function
        
        Public Sub InsertProductModelIllustration(ByVal productModelIllustration As ProductModelIllustration)
            Me.DataContext.ProductModelIllustrations.InsertOnSubmit(productModelIllustration)
        End Sub
        
        Public Sub UpdateProductModelIllustration(ByVal currentProductModelIllustration As ProductModelIllustration)
            Me.DataContext.ProductModelIllustrations.Attach(currentProductModelIllustration, Me.ChangeSet.GetOriginal(currentProductModelIllustration))
        End Sub
        
        Public Sub DeleteProductModelIllustration(ByVal productModelIllustration As ProductModelIllustration)
            Me.DataContext.ProductModelIllustrations.Attach(productModelIllustration)
            Me.DataContext.ProductModelIllustrations.DeleteOnSubmit(productModelIllustration)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductModelProductDescriptionCultures() As IQueryable(Of ProductModelProductDescriptionCulture)
            Return Me.DataContext.ProductModelProductDescriptionCultures
        End Function
        
        Public Sub InsertProductModelProductDescriptionCulture(ByVal productModelProductDescriptionCulture As ProductModelProductDescriptionCulture)
            Me.DataContext.ProductModelProductDescriptionCultures.InsertOnSubmit(productModelProductDescriptionCulture)
        End Sub
        
        Public Sub UpdateProductModelProductDescriptionCulture(ByVal currentProductModelProductDescriptionCulture As ProductModelProductDescriptionCulture)
            Me.DataContext.ProductModelProductDescriptionCultures.Attach(currentProductModelProductDescriptionCulture, Me.ChangeSet.GetOriginal(currentProductModelProductDescriptionCulture))
        End Sub
        
        Public Sub DeleteProductModelProductDescriptionCulture(ByVal productModelProductDescriptionCulture As ProductModelProductDescriptionCulture)
            Me.DataContext.ProductModelProductDescriptionCultures.Attach(productModelProductDescriptionCulture)
            Me.DataContext.ProductModelProductDescriptionCultures.DeleteOnSubmit(productModelProductDescriptionCulture)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductPhotos() As IQueryable(Of ProductPhoto)
            Return Me.DataContext.ProductPhotos
        End Function
        
        Public Sub InsertProductPhoto(ByVal productPhoto As ProductPhoto)
            Me.DataContext.ProductPhotos.InsertOnSubmit(productPhoto)
        End Sub
        
        Public Sub UpdateProductPhoto(ByVal currentProductPhoto As ProductPhoto)
            Me.DataContext.ProductPhotos.Attach(currentProductPhoto, Me.ChangeSet.GetOriginal(currentProductPhoto))
        End Sub
        
        Public Sub DeleteProductPhoto(ByVal productPhoto As ProductPhoto)
            Me.DataContext.ProductPhotos.Attach(productPhoto)
            Me.DataContext.ProductPhotos.DeleteOnSubmit(productPhoto)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductProductPhotos() As IQueryable(Of ProductProductPhoto)
            Return Me.DataContext.ProductProductPhotos
        End Function
        
        Public Sub InsertProductProductPhoto(ByVal productProductPhoto As ProductProductPhoto)
            Me.DataContext.ProductProductPhotos.InsertOnSubmit(productProductPhoto)
        End Sub
        
        Public Sub UpdateProductProductPhoto(ByVal currentProductProductPhoto As ProductProductPhoto)
            Me.DataContext.ProductProductPhotos.Attach(currentProductProductPhoto, Me.ChangeSet.GetOriginal(currentProductProductPhoto))
        End Sub
        
        Public Sub DeleteProductProductPhoto(ByVal productProductPhoto As ProductProductPhoto)
            Me.DataContext.ProductProductPhotos.Attach(productProductPhoto)
            Me.DataContext.ProductProductPhotos.DeleteOnSubmit(productProductPhoto)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductReviews() As IQueryable(Of ProductReview)
            Return Me.DataContext.ProductReviews
        End Function
        
        Public Sub InsertProductReview(ByVal productReview As ProductReview)
            Me.DataContext.ProductReviews.InsertOnSubmit(productReview)
        End Sub
        
        Public Sub UpdateProductReview(ByVal currentProductReview As ProductReview)
            Me.DataContext.ProductReviews.Attach(currentProductReview, Me.ChangeSet.GetOriginal(currentProductReview))
        End Sub
        
        Public Sub DeleteProductReview(ByVal productReview As ProductReview)
            Me.DataContext.ProductReviews.Attach(productReview)
            Me.DataContext.ProductReviews.DeleteOnSubmit(productReview)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductSubcategories() As IQueryable(Of ProductSubcategory)
            Return Me.DataContext.ProductSubcategories
        End Function
        
        Public Sub InsertProductSubcategory(ByVal productSubcategory As ProductSubcategory)
            Me.DataContext.ProductSubcategories.InsertOnSubmit(productSubcategory)
        End Sub
        
        Public Sub UpdateProductSubcategory(ByVal currentProductSubcategory As ProductSubcategory)
            Me.DataContext.ProductSubcategories.Attach(currentProductSubcategory, Me.ChangeSet.GetOriginal(currentProductSubcategory))
        End Sub
        
        Public Sub DeleteProductSubcategory(ByVal productSubcategory As ProductSubcategory)
            Me.DataContext.ProductSubcategories.Attach(productSubcategory)
            Me.DataContext.ProductSubcategories.DeleteOnSubmit(productSubcategory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetProductVendors() As IQueryable(Of ProductVendor)
            Return Me.DataContext.ProductVendors
        End Function
        
        Public Sub InsertProductVendor(ByVal productVendor As ProductVendor)
            Me.DataContext.ProductVendors.InsertOnSubmit(productVendor)
        End Sub
        
        Public Sub UpdateProductVendor(ByVal currentProductVendor As ProductVendor)
            Me.DataContext.ProductVendors.Attach(currentProductVendor, Me.ChangeSet.GetOriginal(currentProductVendor))
        End Sub
        
        Public Sub DeleteProductVendor(ByVal productVendor As ProductVendor)
            Me.DataContext.ProductVendors.Attach(productVendor)
            Me.DataContext.ProductVendors.DeleteOnSubmit(productVendor)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetPurchaseOrders() As IQueryable(Of PurchaseOrder)
            Return Me.DataContext.PurchaseOrders
        End Function
        
        Public Sub InsertPurchaseOrder(ByVal purchaseOrder As PurchaseOrder)
            Me.DataContext.PurchaseOrders.InsertOnSubmit(purchaseOrder)
        End Sub
        
        Public Sub UpdatePurchaseOrder(ByVal currentPurchaseOrder As PurchaseOrder)
            Me.DataContext.PurchaseOrders.Attach(currentPurchaseOrder, Me.ChangeSet.GetOriginal(currentPurchaseOrder))
        End Sub
        
        Public Sub DeletePurchaseOrder(ByVal purchaseOrder As PurchaseOrder)
            Me.DataContext.PurchaseOrders.Attach(purchaseOrder)
            Me.DataContext.PurchaseOrders.DeleteOnSubmit(purchaseOrder)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetPurchaseOrderDetails() As IQueryable(Of PurchaseOrderDetail)
            Return Me.DataContext.PurchaseOrderDetails
        End Function
        
        Public Sub InsertPurchaseOrderDetail(ByVal purchaseOrderDetail As PurchaseOrderDetail)
            Me.DataContext.PurchaseOrderDetails.InsertOnSubmit(purchaseOrderDetail)
        End Sub
        
        Public Sub UpdatePurchaseOrderDetail(ByVal currentPurchaseOrderDetail As PurchaseOrderDetail)
            Me.DataContext.PurchaseOrderDetails.Attach(currentPurchaseOrderDetail, Me.ChangeSet.GetOriginal(currentPurchaseOrderDetail))
        End Sub
        
        Public Sub DeletePurchaseOrderDetail(ByVal purchaseOrderDetail As PurchaseOrderDetail)
            Me.DataContext.PurchaseOrderDetails.Attach(purchaseOrderDetail)
            Me.DataContext.PurchaseOrderDetails.DeleteOnSubmit(purchaseOrderDetail)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesOrderDetails() As IQueryable(Of SalesOrderDetail)
            Return Me.DataContext.SalesOrderDetails
        End Function
        
        Public Sub InsertSalesOrderDetail(ByVal salesOrderDetail As SalesOrderDetail)
            Me.DataContext.SalesOrderDetails.InsertOnSubmit(salesOrderDetail)
        End Sub
        
        Public Sub UpdateSalesOrderDetail(ByVal currentSalesOrderDetail As SalesOrderDetail)
            Me.DataContext.SalesOrderDetails.Attach(currentSalesOrderDetail, Me.ChangeSet.GetOriginal(currentSalesOrderDetail))
        End Sub
        
        Public Sub DeleteSalesOrderDetail(ByVal salesOrderDetail As SalesOrderDetail)
            Me.DataContext.SalesOrderDetails.Attach(salesOrderDetail)
            Me.DataContext.SalesOrderDetails.DeleteOnSubmit(salesOrderDetail)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesOrderHeaders() As IQueryable(Of SalesOrderHeader)
            Return Me.DataContext.SalesOrderHeaders
        End Function
        
        Public Sub InsertSalesOrderHeader(ByVal salesOrderHeader As SalesOrderHeader)
            Me.DataContext.SalesOrderHeaders.InsertOnSubmit(salesOrderHeader)
        End Sub
        
        Public Sub UpdateSalesOrderHeader(ByVal currentSalesOrderHeader As SalesOrderHeader)
            Me.DataContext.SalesOrderHeaders.Attach(currentSalesOrderHeader, Me.ChangeSet.GetOriginal(currentSalesOrderHeader))
        End Sub
        
        Public Sub DeleteSalesOrderHeader(ByVal salesOrderHeader As SalesOrderHeader)
            Me.DataContext.SalesOrderHeaders.Attach(salesOrderHeader)
            Me.DataContext.SalesOrderHeaders.DeleteOnSubmit(salesOrderHeader)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesOrderHeaderSalesReasons() As IQueryable(Of SalesOrderHeaderSalesReason)
            Return Me.DataContext.SalesOrderHeaderSalesReasons
        End Function
        
        Public Sub InsertSalesOrderHeaderSalesReason(ByVal salesOrderHeaderSalesReason As SalesOrderHeaderSalesReason)
            Me.DataContext.SalesOrderHeaderSalesReasons.InsertOnSubmit(salesOrderHeaderSalesReason)
        End Sub
        
        Public Sub UpdateSalesOrderHeaderSalesReason(ByVal currentSalesOrderHeaderSalesReason As SalesOrderHeaderSalesReason)
            Me.DataContext.SalesOrderHeaderSalesReasons.Attach(currentSalesOrderHeaderSalesReason, Me.ChangeSet.GetOriginal(currentSalesOrderHeaderSalesReason))
        End Sub
        
        Public Sub DeleteSalesOrderHeaderSalesReason(ByVal salesOrderHeaderSalesReason As SalesOrderHeaderSalesReason)
            Me.DataContext.SalesOrderHeaderSalesReasons.Attach(salesOrderHeaderSalesReason)
            Me.DataContext.SalesOrderHeaderSalesReasons.DeleteOnSubmit(salesOrderHeaderSalesReason)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesPersons() As IQueryable(Of SalesPerson)
            Return Me.DataContext.SalesPersons
        End Function
        
        Public Sub InsertSalesPerson(ByVal salesPerson As SalesPerson)
            Me.DataContext.SalesPersons.InsertOnSubmit(salesPerson)
        End Sub
        
        Public Sub UpdateSalesPerson(ByVal currentSalesPerson As SalesPerson)
            Me.DataContext.SalesPersons.Attach(currentSalesPerson, Me.ChangeSet.GetOriginal(currentSalesPerson))
        End Sub
        
        Public Sub DeleteSalesPerson(ByVal salesPerson As SalesPerson)
            Me.DataContext.SalesPersons.Attach(salesPerson)
            Me.DataContext.SalesPersons.DeleteOnSubmit(salesPerson)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesPersonQuotaHistories() As IQueryable(Of SalesPersonQuotaHistory)
            Return Me.DataContext.SalesPersonQuotaHistories
        End Function
        
        Public Sub InsertSalesPersonQuotaHistory(ByVal salesPersonQuotaHistory As SalesPersonQuotaHistory)
            Me.DataContext.SalesPersonQuotaHistories.InsertOnSubmit(salesPersonQuotaHistory)
        End Sub
        
        Public Sub UpdateSalesPersonQuotaHistory(ByVal currentSalesPersonQuotaHistory As SalesPersonQuotaHistory)
            Me.DataContext.SalesPersonQuotaHistories.Attach(currentSalesPersonQuotaHistory, Me.ChangeSet.GetOriginal(currentSalesPersonQuotaHistory))
        End Sub
        
        Public Sub DeleteSalesPersonQuotaHistory(ByVal salesPersonQuotaHistory As SalesPersonQuotaHistory)
            Me.DataContext.SalesPersonQuotaHistories.Attach(salesPersonQuotaHistory)
            Me.DataContext.SalesPersonQuotaHistories.DeleteOnSubmit(salesPersonQuotaHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesReasons() As IQueryable(Of SalesReason)
            Return Me.DataContext.SalesReasons
        End Function
        
        Public Sub InsertSalesReason(ByVal salesReason As SalesReason)
            Me.DataContext.SalesReasons.InsertOnSubmit(salesReason)
        End Sub
        
        Public Sub UpdateSalesReason(ByVal currentSalesReason As SalesReason)
            Me.DataContext.SalesReasons.Attach(currentSalesReason, Me.ChangeSet.GetOriginal(currentSalesReason))
        End Sub
        
        Public Sub DeleteSalesReason(ByVal salesReason As SalesReason)
            Me.DataContext.SalesReasons.Attach(salesReason)
            Me.DataContext.SalesReasons.DeleteOnSubmit(salesReason)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesTaxRates() As IQueryable(Of SalesTaxRate)
            Return Me.DataContext.SalesTaxRates
        End Function
        
        Public Sub InsertSalesTaxRate(ByVal salesTaxRate As SalesTaxRate)
            Me.DataContext.SalesTaxRates.InsertOnSubmit(salesTaxRate)
        End Sub
        
        Public Sub UpdateSalesTaxRate(ByVal currentSalesTaxRate As SalesTaxRate)
            Me.DataContext.SalesTaxRates.Attach(currentSalesTaxRate, Me.ChangeSet.GetOriginal(currentSalesTaxRate))
        End Sub
        
        Public Sub DeleteSalesTaxRate(ByVal salesTaxRate As SalesTaxRate)
            Me.DataContext.SalesTaxRates.Attach(salesTaxRate)
            Me.DataContext.SalesTaxRates.DeleteOnSubmit(salesTaxRate)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesTerritories() As IQueryable(Of SalesTerritory)
            Return Me.DataContext.SalesTerritories
        End Function
        
        Public Sub InsertSalesTerritory(ByVal salesTerritory As SalesTerritory)
            Me.DataContext.SalesTerritories.InsertOnSubmit(salesTerritory)
        End Sub
        
        Public Sub UpdateSalesTerritory(ByVal currentSalesTerritory As SalesTerritory)
            Me.DataContext.SalesTerritories.Attach(currentSalesTerritory, Me.ChangeSet.GetOriginal(currentSalesTerritory))
        End Sub
        
        Public Sub DeleteSalesTerritory(ByVal salesTerritory As SalesTerritory)
            Me.DataContext.SalesTerritories.Attach(salesTerritory)
            Me.DataContext.SalesTerritories.DeleteOnSubmit(salesTerritory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSalesTerritoryHistories() As IQueryable(Of SalesTerritoryHistory)
            Return Me.DataContext.SalesTerritoryHistories
        End Function
        
        Public Sub InsertSalesTerritoryHistory(ByVal salesTerritoryHistory As SalesTerritoryHistory)
            Me.DataContext.SalesTerritoryHistories.InsertOnSubmit(salesTerritoryHistory)
        End Sub
        
        Public Sub UpdateSalesTerritoryHistory(ByVal currentSalesTerritoryHistory As SalesTerritoryHistory)
            Me.DataContext.SalesTerritoryHistories.Attach(currentSalesTerritoryHistory, Me.ChangeSet.GetOriginal(currentSalesTerritoryHistory))
        End Sub
        
        Public Sub DeleteSalesTerritoryHistory(ByVal salesTerritoryHistory As SalesTerritoryHistory)
            Me.DataContext.SalesTerritoryHistories.Attach(salesTerritoryHistory)
            Me.DataContext.SalesTerritoryHistories.DeleteOnSubmit(salesTerritoryHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetScrapReasons() As IQueryable(Of ScrapReason)
            Return Me.DataContext.ScrapReasons
        End Function
        
        Public Sub InsertScrapReason(ByVal scrapReason As ScrapReason)
            Me.DataContext.ScrapReasons.InsertOnSubmit(scrapReason)
        End Sub
        
        Public Sub UpdateScrapReason(ByVal currentScrapReason As ScrapReason)
            Me.DataContext.ScrapReasons.Attach(currentScrapReason, Me.ChangeSet.GetOriginal(currentScrapReason))
        End Sub
        
        Public Sub DeleteScrapReason(ByVal scrapReason As ScrapReason)
            Me.DataContext.ScrapReasons.Attach(scrapReason)
            Me.DataContext.ScrapReasons.DeleteOnSubmit(scrapReason)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetShifts() As IQueryable(Of Shift)
            Return Me.DataContext.Shifts
        End Function
        
        Public Sub InsertShift(ByVal shift As Shift)
            Me.DataContext.Shifts.InsertOnSubmit(shift)
        End Sub
        
        Public Sub UpdateShift(ByVal currentShift As Shift)
            Me.DataContext.Shifts.Attach(currentShift, Me.ChangeSet.GetOriginal(currentShift))
        End Sub
        
        Public Sub DeleteShift(ByVal shift As Shift)
            Me.DataContext.Shifts.Attach(shift)
            Me.DataContext.Shifts.DeleteOnSubmit(shift)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetShipMethods() As IQueryable(Of ShipMethod)
            Return Me.DataContext.ShipMethods
        End Function
        
        Public Sub InsertShipMethod(ByVal shipMethod As ShipMethod)
            Me.DataContext.ShipMethods.InsertOnSubmit(shipMethod)
        End Sub
        
        Public Sub UpdateShipMethod(ByVal currentShipMethod As ShipMethod)
            Me.DataContext.ShipMethods.Attach(currentShipMethod, Me.ChangeSet.GetOriginal(currentShipMethod))
        End Sub
        
        Public Sub DeleteShipMethod(ByVal shipMethod As ShipMethod)
            Me.DataContext.ShipMethods.Attach(shipMethod)
            Me.DataContext.ShipMethods.DeleteOnSubmit(shipMethod)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetShoppingCartItems() As IQueryable(Of ShoppingCartItem)
            Return Me.DataContext.ShoppingCartItems
        End Function
        
        Public Sub InsertShoppingCartItem(ByVal shoppingCartItem As ShoppingCartItem)
            Me.DataContext.ShoppingCartItems.InsertOnSubmit(shoppingCartItem)
        End Sub
        
        Public Sub UpdateShoppingCartItem(ByVal currentShoppingCartItem As ShoppingCartItem)
            Me.DataContext.ShoppingCartItems.Attach(currentShoppingCartItem, Me.ChangeSet.GetOriginal(currentShoppingCartItem))
        End Sub
        
        Public Sub DeleteShoppingCartItem(ByVal shoppingCartItem As ShoppingCartItem)
            Me.DataContext.ShoppingCartItems.Attach(shoppingCartItem)
            Me.DataContext.ShoppingCartItems.DeleteOnSubmit(shoppingCartItem)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSpecialOffers() As IQueryable(Of SpecialOffer)
            Return Me.DataContext.SpecialOffers
        End Function
        
        Public Sub InsertSpecialOffer(ByVal specialOffer As SpecialOffer)
            Me.DataContext.SpecialOffers.InsertOnSubmit(specialOffer)
        End Sub
        
        Public Sub UpdateSpecialOffer(ByVal currentSpecialOffer As SpecialOffer)
            Me.DataContext.SpecialOffers.Attach(currentSpecialOffer, Me.ChangeSet.GetOriginal(currentSpecialOffer))
        End Sub
        
        Public Sub DeleteSpecialOffer(ByVal specialOffer As SpecialOffer)
            Me.DataContext.SpecialOffers.Attach(specialOffer)
            Me.DataContext.SpecialOffers.DeleteOnSubmit(specialOffer)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetSpecialOfferProducts() As IQueryable(Of SpecialOfferProduct)
            Return Me.DataContext.SpecialOfferProducts
        End Function
        
        Public Sub InsertSpecialOfferProduct(ByVal specialOfferProduct As SpecialOfferProduct)
            Me.DataContext.SpecialOfferProducts.InsertOnSubmit(specialOfferProduct)
        End Sub
        
        Public Sub UpdateSpecialOfferProduct(ByVal currentSpecialOfferProduct As SpecialOfferProduct)
            Me.DataContext.SpecialOfferProducts.Attach(currentSpecialOfferProduct, Me.ChangeSet.GetOriginal(currentSpecialOfferProduct))
        End Sub
        
        Public Sub DeleteSpecialOfferProduct(ByVal specialOfferProduct As SpecialOfferProduct)
            Me.DataContext.SpecialOfferProducts.Attach(specialOfferProduct)
            Me.DataContext.SpecialOfferProducts.DeleteOnSubmit(specialOfferProduct)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetStateProvinces() As IQueryable(Of StateProvince)
            Return Me.DataContext.StateProvinces
        End Function
        
        Public Sub InsertStateProvince(ByVal stateProvince As StateProvince)
            Me.DataContext.StateProvinces.InsertOnSubmit(stateProvince)
        End Sub
        
        Public Sub UpdateStateProvince(ByVal currentStateProvince As StateProvince)
            Me.DataContext.StateProvinces.Attach(currentStateProvince, Me.ChangeSet.GetOriginal(currentStateProvince))
        End Sub
        
        Public Sub DeleteStateProvince(ByVal stateProvince As StateProvince)
            Me.DataContext.StateProvinces.Attach(stateProvince)
            Me.DataContext.StateProvinces.DeleteOnSubmit(stateProvince)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetStores() As IQueryable(Of Store)
            Return Me.DataContext.Stores
        End Function
        
        Public Sub InsertStore(ByVal store As Store)
            Me.DataContext.Stores.InsertOnSubmit(store)
        End Sub
        
        Public Sub UpdateStore(ByVal currentStore As Store)
            Me.DataContext.Stores.Attach(currentStore, Me.ChangeSet.GetOriginal(currentStore))
        End Sub
        
        Public Sub DeleteStore(ByVal store As Store)
            Me.DataContext.Stores.Attach(store)
            Me.DataContext.Stores.DeleteOnSubmit(store)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetStoreContacts() As IQueryable(Of StoreContact)
            Return Me.DataContext.StoreContacts
        End Function
        
        Public Sub InsertStoreContact(ByVal storeContact As StoreContact)
            Me.DataContext.StoreContacts.InsertOnSubmit(storeContact)
        End Sub
        
        Public Sub UpdateStoreContact(ByVal currentStoreContact As StoreContact)
            Me.DataContext.StoreContacts.Attach(currentStoreContact, Me.ChangeSet.GetOriginal(currentStoreContact))
        End Sub
        
        Public Sub DeleteStoreContact(ByVal storeContact As StoreContact)
            Me.DataContext.StoreContacts.Attach(storeContact)
            Me.DataContext.StoreContacts.DeleteOnSubmit(storeContact)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetTransactionHistories() As IQueryable(Of TransactionHistory)
            Return Me.DataContext.TransactionHistories
        End Function
        
        Public Sub InsertTransactionHistory(ByVal transactionHistory As TransactionHistory)
            Me.DataContext.TransactionHistories.InsertOnSubmit(transactionHistory)
        End Sub
        
        Public Sub UpdateTransactionHistory(ByVal currentTransactionHistory As TransactionHistory)
            Me.DataContext.TransactionHistories.Attach(currentTransactionHistory, Me.ChangeSet.GetOriginal(currentTransactionHistory))
        End Sub
        
        Public Sub DeleteTransactionHistory(ByVal transactionHistory As TransactionHistory)
            Me.DataContext.TransactionHistories.Attach(transactionHistory)
            Me.DataContext.TransactionHistories.DeleteOnSubmit(transactionHistory)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetTransactionHistoryArchives() As IQueryable(Of TransactionHistoryArchive)
            Return Me.DataContext.TransactionHistoryArchives
        End Function
        
        Public Sub InsertTransactionHistoryArchive(ByVal transactionHistoryArchive As TransactionHistoryArchive)
            Me.DataContext.TransactionHistoryArchives.InsertOnSubmit(transactionHistoryArchive)
        End Sub
        
        Public Sub UpdateTransactionHistoryArchive(ByVal currentTransactionHistoryArchive As TransactionHistoryArchive)
            Me.DataContext.TransactionHistoryArchives.Attach(currentTransactionHistoryArchive, Me.ChangeSet.GetOriginal(currentTransactionHistoryArchive))
        End Sub
        
        Public Sub DeleteTransactionHistoryArchive(ByVal transactionHistoryArchive As TransactionHistoryArchive)
            Me.DataContext.TransactionHistoryArchives.Attach(transactionHistoryArchive)
            Me.DataContext.TransactionHistoryArchives.DeleteOnSubmit(transactionHistoryArchive)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetUnitMeasures() As IQueryable(Of UnitMeasure)
            Return Me.DataContext.UnitMeasures
        End Function
        
        Public Sub InsertUnitMeasure(ByVal unitMeasure As UnitMeasure)
            Me.DataContext.UnitMeasures.InsertOnSubmit(unitMeasure)
        End Sub
        
        Public Sub UpdateUnitMeasure(ByVal currentUnitMeasure As UnitMeasure)
            Me.DataContext.UnitMeasures.Attach(currentUnitMeasure, Me.ChangeSet.GetOriginal(currentUnitMeasure))
        End Sub
        
        Public Sub DeleteUnitMeasure(ByVal unitMeasure As UnitMeasure)
            Me.DataContext.UnitMeasures.Attach(unitMeasure)
            Me.DataContext.UnitMeasures.DeleteOnSubmit(unitMeasure)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetVendors() As IQueryable(Of Vendor)
            Return Me.DataContext.Vendors
        End Function
        
        Public Sub InsertVendor(ByVal vendor As Vendor)
            Me.DataContext.Vendors.InsertOnSubmit(vendor)
        End Sub
        
        Public Sub UpdateVendor(ByVal currentVendor As Vendor)
            Me.DataContext.Vendors.Attach(currentVendor, Me.ChangeSet.GetOriginal(currentVendor))
        End Sub
        
        Public Sub DeleteVendor(ByVal vendor As Vendor)
            Me.DataContext.Vendors.Attach(vendor)
            Me.DataContext.Vendors.DeleteOnSubmit(vendor)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetVendorAddresses() As IQueryable(Of VendorAddress)
            Return Me.DataContext.VendorAddresses
        End Function
        
        Public Sub InsertVendorAddress(ByVal vendorAddress As VendorAddress)
            Me.DataContext.VendorAddresses.InsertOnSubmit(vendorAddress)
        End Sub
        
        Public Sub UpdateVendorAddress(ByVal currentVendorAddress As VendorAddress)
            Me.DataContext.VendorAddresses.Attach(currentVendorAddress, Me.ChangeSet.GetOriginal(currentVendorAddress))
        End Sub
        
        Public Sub DeleteVendorAddress(ByVal vendorAddress As VendorAddress)
            Me.DataContext.VendorAddresses.Attach(vendorAddress)
            Me.DataContext.VendorAddresses.DeleteOnSubmit(vendorAddress)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetVendorContacts() As IQueryable(Of VendorContact)
            Return Me.DataContext.VendorContacts
        End Function
        
        Public Sub InsertVendorContact(ByVal vendorContact As VendorContact)
            Me.DataContext.VendorContacts.InsertOnSubmit(vendorContact)
        End Sub
        
        Public Sub UpdateVendorContact(ByVal currentVendorContact As VendorContact)
            Me.DataContext.VendorContacts.Attach(currentVendorContact, Me.ChangeSet.GetOriginal(currentVendorContact))
        End Sub
        
        Public Sub DeleteVendorContact(ByVal vendorContact As VendorContact)
            Me.DataContext.VendorContacts.Attach(vendorContact)
            Me.DataContext.VendorContacts.DeleteOnSubmit(vendorContact)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetWorkOrders() As IQueryable(Of WorkOrder)
            Return Me.DataContext.WorkOrders
        End Function
        
        Public Sub InsertWorkOrder(ByVal workOrder As WorkOrder)
            Me.DataContext.WorkOrders.InsertOnSubmit(workOrder)
        End Sub
        
        Public Sub UpdateWorkOrder(ByVal currentWorkOrder As WorkOrder)
            Me.DataContext.WorkOrders.Attach(currentWorkOrder, Me.ChangeSet.GetOriginal(currentWorkOrder))
        End Sub
        
        Public Sub DeleteWorkOrder(ByVal workOrder As WorkOrder)
            Me.DataContext.WorkOrders.Attach(workOrder)
            Me.DataContext.WorkOrders.DeleteOnSubmit(workOrder)
        End Sub
        
        'TODO:
        ' Consider constraining the results of your query method.  If you need additional input you can
        ' add parameters to this method or create additional query methods with different names.
        Public Function GetWorkOrderRoutings() As IQueryable(Of WorkOrderRouting)
            Return Me.DataContext.WorkOrderRoutings
        End Function
        
        Public Sub InsertWorkOrderRouting(ByVal workOrderRouting As WorkOrderRouting)
            Me.DataContext.WorkOrderRoutings.InsertOnSubmit(workOrderRouting)
        End Sub
        
        Public Sub UpdateWorkOrderRouting(ByVal currentWorkOrderRouting As WorkOrderRouting)
            Me.DataContext.WorkOrderRoutings.Attach(currentWorkOrderRouting, Me.ChangeSet.GetOriginal(currentWorkOrderRouting))
        End Sub
        
        Public Sub DeleteWorkOrderRouting(ByVal workOrderRouting As WorkOrderRouting)
            Me.DataContext.WorkOrderRoutings.Attach(workOrderRouting)
            Me.DataContext.WorkOrderRoutings.DeleteOnSubmit(workOrderRouting)
        End Sub
    End Class
End Namespace
