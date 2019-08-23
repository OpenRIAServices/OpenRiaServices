
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Linq;
    using AdventureWorksModel;
    using OpenRiaServices.DomainServices.EntityFramework;
    using OpenRiaServices.DomainServices.Hosting;
    using OpenRiaServices.DomainServices.Server;
    
    
    // Implements application logic using the AdventureWorksEntities context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class EF_AdventureWorks : LinqToEntitiesDomainService<AdventureWorksEntities>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Addresses' query.
        public IQueryable<Address> GetAddresses()
        {
            return this.ObjectContext.Addresses;
        }
        
        public void InsertAddress(Address address)
        {
            if ((address.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(address, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Addresses.AddObject(address);
            }
        }
        
        public void UpdateAddress(Address currentAddress)
        {
            this.ObjectContext.Addresses.AttachAsModified(currentAddress, this.ChangeSet.GetOriginal(currentAddress));
        }
        
        public void DeleteAddress(Address address)
        {
            if ((address.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(address, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Addresses.Attach(address);
                this.ObjectContext.Addresses.DeleteObject(address);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'AddressTypes' query.
        public IQueryable<AddressType> GetAddressTypes()
        {
            return this.ObjectContext.AddressTypes;
        }
        
        public void InsertAddressType(AddressType addressType)
        {
            if ((addressType.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(addressType, EntityState.Added);
            }
            else
            {
                this.ObjectContext.AddressTypes.AddObject(addressType);
            }
        }
        
        public void UpdateAddressType(AddressType currentAddressType)
        {
            this.ObjectContext.AddressTypes.AttachAsModified(currentAddressType, this.ChangeSet.GetOriginal(currentAddressType));
        }
        
        public void DeleteAddressType(AddressType addressType)
        {
            if ((addressType.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(addressType, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.AddressTypes.Attach(addressType);
                this.ObjectContext.AddressTypes.DeleteObject(addressType);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'AWBuildVersions' query.
        public IQueryable<AWBuildVersion> GetAWBuildVersions()
        {
            return this.ObjectContext.AWBuildVersions;
        }
        
        public void InsertAWBuildVersion(AWBuildVersion aWBuildVersion)
        {
            if ((aWBuildVersion.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(aWBuildVersion, EntityState.Added);
            }
            else
            {
                this.ObjectContext.AWBuildVersions.AddObject(aWBuildVersion);
            }
        }
        
        public void UpdateAWBuildVersion(AWBuildVersion currentAWBuildVersion)
        {
            this.ObjectContext.AWBuildVersions.AttachAsModified(currentAWBuildVersion, this.ChangeSet.GetOriginal(currentAWBuildVersion));
        }
        
        public void DeleteAWBuildVersion(AWBuildVersion aWBuildVersion)
        {
            if ((aWBuildVersion.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(aWBuildVersion, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.AWBuildVersions.Attach(aWBuildVersion);
                this.ObjectContext.AWBuildVersions.DeleteObject(aWBuildVersion);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'BillOfMaterials' query.
        public IQueryable<BillOfMaterial> GetBillOfMaterials()
        {
            return this.ObjectContext.BillOfMaterials;
        }
        
        public void InsertBillOfMaterial(BillOfMaterial billOfMaterial)
        {
            if ((billOfMaterial.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(billOfMaterial, EntityState.Added);
            }
            else
            {
                this.ObjectContext.BillOfMaterials.AddObject(billOfMaterial);
            }
        }
        
        public void UpdateBillOfMaterial(BillOfMaterial currentBillOfMaterial)
        {
            this.ObjectContext.BillOfMaterials.AttachAsModified(currentBillOfMaterial, this.ChangeSet.GetOriginal(currentBillOfMaterial));
        }
        
        public void DeleteBillOfMaterial(BillOfMaterial billOfMaterial)
        {
            if ((billOfMaterial.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(billOfMaterial, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.BillOfMaterials.Attach(billOfMaterial);
                this.ObjectContext.BillOfMaterials.DeleteObject(billOfMaterial);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Contacts' query.
        public IQueryable<Contact> GetContacts()
        {
            return this.ObjectContext.Contacts;
        }
        
        public void InsertContact(Contact contact)
        {
            if ((contact.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(contact, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Contacts.AddObject(contact);
            }
        }
        
        public void UpdateContact(Contact currentContact)
        {
            this.ObjectContext.Contacts.AttachAsModified(currentContact, this.ChangeSet.GetOriginal(currentContact));
        }
        
        public void DeleteContact(Contact contact)
        {
            if ((contact.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(contact, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Contacts.Attach(contact);
                this.ObjectContext.Contacts.DeleteObject(contact);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ContactCreditCards' query.
        public IQueryable<ContactCreditCard> GetContactCreditCards()
        {
            return this.ObjectContext.ContactCreditCards;
        }
        
        public void InsertContactCreditCard(ContactCreditCard contactCreditCard)
        {
            if ((contactCreditCard.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(contactCreditCard, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ContactCreditCards.AddObject(contactCreditCard);
            }
        }
        
        public void UpdateContactCreditCard(ContactCreditCard currentContactCreditCard)
        {
            this.ObjectContext.ContactCreditCards.AttachAsModified(currentContactCreditCard, this.ChangeSet.GetOriginal(currentContactCreditCard));
        }
        
        public void DeleteContactCreditCard(ContactCreditCard contactCreditCard)
        {
            if ((contactCreditCard.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(contactCreditCard, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ContactCreditCards.Attach(contactCreditCard);
                this.ObjectContext.ContactCreditCards.DeleteObject(contactCreditCard);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ContactTypes' query.
        public IQueryable<ContactType> GetContactTypes()
        {
            return this.ObjectContext.ContactTypes;
        }
        
        public void InsertContactType(ContactType contactType)
        {
            if ((contactType.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(contactType, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ContactTypes.AddObject(contactType);
            }
        }
        
        public void UpdateContactType(ContactType currentContactType)
        {
            this.ObjectContext.ContactTypes.AttachAsModified(currentContactType, this.ChangeSet.GetOriginal(currentContactType));
        }
        
        public void DeleteContactType(ContactType contactType)
        {
            if ((contactType.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(contactType, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ContactTypes.Attach(contactType);
                this.ObjectContext.ContactTypes.DeleteObject(contactType);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CountryRegions' query.
        public IQueryable<CountryRegion> GetCountryRegions()
        {
            return this.ObjectContext.CountryRegions;
        }
        
        public void InsertCountryRegion(CountryRegion countryRegion)
        {
            if ((countryRegion.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegion, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CountryRegions.AddObject(countryRegion);
            }
        }
        
        public void UpdateCountryRegion(CountryRegion currentCountryRegion)
        {
            this.ObjectContext.CountryRegions.AttachAsModified(currentCountryRegion, this.ChangeSet.GetOriginal(currentCountryRegion));
        }
        
        public void DeleteCountryRegion(CountryRegion countryRegion)
        {
            if ((countryRegion.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegion, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CountryRegions.Attach(countryRegion);
                this.ObjectContext.CountryRegions.DeleteObject(countryRegion);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CountryRegionCurrencies' query.
        public IQueryable<CountryRegionCurrency> GetCountryRegionCurrencies()
        {
            return this.ObjectContext.CountryRegionCurrencies;
        }
        
        public void InsertCountryRegionCurrency(CountryRegionCurrency countryRegionCurrency)
        {
            if ((countryRegionCurrency.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegionCurrency, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CountryRegionCurrencies.AddObject(countryRegionCurrency);
            }
        }
        
        public void UpdateCountryRegionCurrency(CountryRegionCurrency currentCountryRegionCurrency)
        {
            this.ObjectContext.CountryRegionCurrencies.AttachAsModified(currentCountryRegionCurrency, this.ChangeSet.GetOriginal(currentCountryRegionCurrency));
        }
        
        public void DeleteCountryRegionCurrency(CountryRegionCurrency countryRegionCurrency)
        {
            if ((countryRegionCurrency.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(countryRegionCurrency, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CountryRegionCurrencies.Attach(countryRegionCurrency);
                this.ObjectContext.CountryRegionCurrencies.DeleteObject(countryRegionCurrency);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CreditCards' query.
        public IQueryable<CreditCard> GetCreditCards()
        {
            return this.ObjectContext.CreditCards;
        }
        
        public void InsertCreditCard(CreditCard creditCard)
        {
            if ((creditCard.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(creditCard, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CreditCards.AddObject(creditCard);
            }
        }
        
        public void UpdateCreditCard(CreditCard currentCreditCard)
        {
            this.ObjectContext.CreditCards.AttachAsModified(currentCreditCard, this.ChangeSet.GetOriginal(currentCreditCard));
        }
        
        public void DeleteCreditCard(CreditCard creditCard)
        {
            if ((creditCard.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(creditCard, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CreditCards.Attach(creditCard);
                this.ObjectContext.CreditCards.DeleteObject(creditCard);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Cultures' query.
        public IQueryable<Culture> GetCultures()
        {
            return this.ObjectContext.Cultures;
        }
        
        public void InsertCulture(Culture culture)
        {
            if ((culture.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(culture, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Cultures.AddObject(culture);
            }
        }
        
        public void UpdateCulture(Culture currentCulture)
        {
            this.ObjectContext.Cultures.AttachAsModified(currentCulture, this.ChangeSet.GetOriginal(currentCulture));
        }
        
        public void DeleteCulture(Culture culture)
        {
            if ((culture.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(culture, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Cultures.Attach(culture);
                this.ObjectContext.Cultures.DeleteObject(culture);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Currencies' query.
        public IQueryable<Currency> GetCurrencies()
        {
            return this.ObjectContext.Currencies;
        }
        
        public void InsertCurrency(Currency currency)
        {
            if ((currency.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(currency, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Currencies.AddObject(currency);
            }
        }
        
        public void UpdateCurrency(Currency currentCurrency)
        {
            this.ObjectContext.Currencies.AttachAsModified(currentCurrency, this.ChangeSet.GetOriginal(currentCurrency));
        }
        
        public void DeleteCurrency(Currency currency)
        {
            if ((currency.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(currency, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Currencies.Attach(currency);
                this.ObjectContext.Currencies.DeleteObject(currency);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CurrencyRates' query.
        public IQueryable<CurrencyRate> GetCurrencyRates()
        {
            return this.ObjectContext.CurrencyRates;
        }
        
        public void InsertCurrencyRate(CurrencyRate currencyRate)
        {
            if ((currencyRate.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(currencyRate, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CurrencyRates.AddObject(currencyRate);
            }
        }
        
        public void UpdateCurrencyRate(CurrencyRate currentCurrencyRate)
        {
            this.ObjectContext.CurrencyRates.AttachAsModified(currentCurrencyRate, this.ChangeSet.GetOriginal(currentCurrencyRate));
        }
        
        public void DeleteCurrencyRate(CurrencyRate currencyRate)
        {
            if ((currencyRate.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(currencyRate, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CurrencyRates.Attach(currencyRate);
                this.ObjectContext.CurrencyRates.DeleteObject(currencyRate);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Customers' query.
        public IQueryable<Customer> GetCustomers()
        {
            return this.ObjectContext.Customers;
        }
        
        public void InsertCustomer(Customer customer)
        {
            if ((customer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Customers.AddObject(customer);
            }
        }
        
        public void UpdateCustomer(Customer currentCustomer)
        {
            this.ObjectContext.Customers.AttachAsModified(currentCustomer, this.ChangeSet.GetOriginal(currentCustomer));
        }
        
        public void DeleteCustomer(Customer customer)
        {
            if ((customer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customer, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Customers.Attach(customer);
                this.ObjectContext.Customers.DeleteObject(customer);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'CustomerAddresses' query.
        public IQueryable<CustomerAddress> GetCustomerAddresses()
        {
            return this.ObjectContext.CustomerAddresses;
        }
        
        public void InsertCustomerAddress(CustomerAddress customerAddress)
        {
            if ((customerAddress.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customerAddress, EntityState.Added);
            }
            else
            {
                this.ObjectContext.CustomerAddresses.AddObject(customerAddress);
            }
        }
        
        public void UpdateCustomerAddress(CustomerAddress currentCustomerAddress)
        {
            this.ObjectContext.CustomerAddresses.AttachAsModified(currentCustomerAddress, this.ChangeSet.GetOriginal(currentCustomerAddress));
        }
        
        public void DeleteCustomerAddress(CustomerAddress customerAddress)
        {
            if ((customerAddress.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(customerAddress, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.CustomerAddresses.Attach(customerAddress);
                this.ObjectContext.CustomerAddresses.DeleteObject(customerAddress);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'DatabaseLogs' query.
        public IQueryable<DatabaseLog> GetDatabaseLogs()
        {
            return this.ObjectContext.DatabaseLogs;
        }
        
        public void InsertDatabaseLog(DatabaseLog databaseLog)
        {
            if ((databaseLog.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(databaseLog, EntityState.Added);
            }
            else
            {
                this.ObjectContext.DatabaseLogs.AddObject(databaseLog);
            }
        }
        
        public void UpdateDatabaseLog(DatabaseLog currentDatabaseLog)
        {
            this.ObjectContext.DatabaseLogs.AttachAsModified(currentDatabaseLog, this.ChangeSet.GetOriginal(currentDatabaseLog));
        }
        
        public void DeleteDatabaseLog(DatabaseLog databaseLog)
        {
            if ((databaseLog.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(databaseLog, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.DatabaseLogs.Attach(databaseLog);
                this.ObjectContext.DatabaseLogs.DeleteObject(databaseLog);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Departments' query.
        public IQueryable<Department> GetDepartments()
        {
            return this.ObjectContext.Departments;
        }
        
        public void InsertDepartment(Department department)
        {
            if ((department.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(department, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Departments.AddObject(department);
            }
        }
        
        public void UpdateDepartment(Department currentDepartment)
        {
            this.ObjectContext.Departments.AttachAsModified(currentDepartment, this.ChangeSet.GetOriginal(currentDepartment));
        }
        
        public void DeleteDepartment(Department department)
        {
            if ((department.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(department, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Departments.Attach(department);
                this.ObjectContext.Departments.DeleteObject(department);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Documents' query.
        public IQueryable<Document> GetDocuments()
        {
            return this.ObjectContext.Documents;
        }
        
        public void InsertDocument(Document document)
        {
            if ((document.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(document, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Documents.AddObject(document);
            }
        }
        
        public void UpdateDocument(Document currentDocument)
        {
            this.ObjectContext.Documents.AttachAsModified(currentDocument, this.ChangeSet.GetOriginal(currentDocument));
        }
        
        public void DeleteDocument(Document document)
        {
            if ((document.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(document, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Documents.Attach(document);
                this.ObjectContext.Documents.DeleteObject(document);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Employees' query.
        public IQueryable<Employee> GetEmployees()
        {
            return this.ObjectContext.Employees;
        }
        
        public void InsertEmployee(Employee employee)
        {
            if ((employee.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Employees.AddObject(employee);
            }
        }
        
        public void UpdateEmployee(Employee currentEmployee)
        {
            this.ObjectContext.Employees.AttachAsModified(currentEmployee, this.ChangeSet.GetOriginal(currentEmployee));
        }
        
        public void DeleteEmployee(Employee employee)
        {
            if ((employee.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employee, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Employees.Attach(employee);
                this.ObjectContext.Employees.DeleteObject(employee);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'EmployeeAddresses' query.
        public IQueryable<EmployeeAddress> GetEmployeeAddresses()
        {
            return this.ObjectContext.EmployeeAddresses;
        }
        
        public void InsertEmployeeAddress(EmployeeAddress employeeAddress)
        {
            if ((employeeAddress.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeeAddress, EntityState.Added);
            }
            else
            {
                this.ObjectContext.EmployeeAddresses.AddObject(employeeAddress);
            }
        }
        
        public void UpdateEmployeeAddress(EmployeeAddress currentEmployeeAddress)
        {
            this.ObjectContext.EmployeeAddresses.AttachAsModified(currentEmployeeAddress, this.ChangeSet.GetOriginal(currentEmployeeAddress));
        }
        
        public void DeleteEmployeeAddress(EmployeeAddress employeeAddress)
        {
            if ((employeeAddress.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeeAddress, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.EmployeeAddresses.Attach(employeeAddress);
                this.ObjectContext.EmployeeAddresses.DeleteObject(employeeAddress);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'EmployeeDepartmentHistories' query.
        public IQueryable<EmployeeDepartmentHistory> GetEmployeeDepartmentHistories()
        {
            return this.ObjectContext.EmployeeDepartmentHistories;
        }
        
        public void InsertEmployeeDepartmentHistory(EmployeeDepartmentHistory employeeDepartmentHistory)
        {
            if ((employeeDepartmentHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeeDepartmentHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.EmployeeDepartmentHistories.AddObject(employeeDepartmentHistory);
            }
        }
        
        public void UpdateEmployeeDepartmentHistory(EmployeeDepartmentHistory currentEmployeeDepartmentHistory)
        {
            this.ObjectContext.EmployeeDepartmentHistories.AttachAsModified(currentEmployeeDepartmentHistory, this.ChangeSet.GetOriginal(currentEmployeeDepartmentHistory));
        }
        
        public void DeleteEmployeeDepartmentHistory(EmployeeDepartmentHistory employeeDepartmentHistory)
        {
            if ((employeeDepartmentHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeeDepartmentHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.EmployeeDepartmentHistories.Attach(employeeDepartmentHistory);
                this.ObjectContext.EmployeeDepartmentHistories.DeleteObject(employeeDepartmentHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'EmployeePayHistories' query.
        public IQueryable<EmployeePayHistory> GetEmployeePayHistories()
        {
            return this.ObjectContext.EmployeePayHistories;
        }
        
        public void InsertEmployeePayHistory(EmployeePayHistory employeePayHistory)
        {
            if ((employeePayHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeePayHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.EmployeePayHistories.AddObject(employeePayHistory);
            }
        }
        
        public void UpdateEmployeePayHistory(EmployeePayHistory currentEmployeePayHistory)
        {
            this.ObjectContext.EmployeePayHistories.AttachAsModified(currentEmployeePayHistory, this.ChangeSet.GetOriginal(currentEmployeePayHistory));
        }
        
        public void DeleteEmployeePayHistory(EmployeePayHistory employeePayHistory)
        {
            if ((employeePayHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(employeePayHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.EmployeePayHistories.Attach(employeePayHistory);
                this.ObjectContext.EmployeePayHistories.DeleteObject(employeePayHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ErrorLogs' query.
        public IQueryable<ErrorLog> GetErrorLogs()
        {
            return this.ObjectContext.ErrorLogs;
        }
        
        public void InsertErrorLog(ErrorLog errorLog)
        {
            if ((errorLog.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(errorLog, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ErrorLogs.AddObject(errorLog);
            }
        }
        
        public void UpdateErrorLog(ErrorLog currentErrorLog)
        {
            this.ObjectContext.ErrorLogs.AttachAsModified(currentErrorLog, this.ChangeSet.GetOriginal(currentErrorLog));
        }
        
        public void DeleteErrorLog(ErrorLog errorLog)
        {
            if ((errorLog.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(errorLog, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ErrorLogs.Attach(errorLog);
                this.ObjectContext.ErrorLogs.DeleteObject(errorLog);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Illustrations' query.
        public IQueryable<Illustration> GetIllustrations()
        {
            return this.ObjectContext.Illustrations;
        }
        
        public void InsertIllustration(Illustration illustration)
        {
            if ((illustration.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(illustration, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Illustrations.AddObject(illustration);
            }
        }
        
        public void UpdateIllustration(Illustration currentIllustration)
        {
            this.ObjectContext.Illustrations.AttachAsModified(currentIllustration, this.ChangeSet.GetOriginal(currentIllustration));
        }
        
        public void DeleteIllustration(Illustration illustration)
        {
            if ((illustration.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(illustration, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Illustrations.Attach(illustration);
                this.ObjectContext.Illustrations.DeleteObject(illustration);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Individuals' query.
        public IQueryable<Individual> GetIndividuals()
        {
            return this.ObjectContext.Individuals;
        }
        
        public void InsertIndividual(Individual individual)
        {
            if ((individual.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(individual, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Individuals.AddObject(individual);
            }
        }
        
        public void UpdateIndividual(Individual currentIndividual)
        {
            this.ObjectContext.Individuals.AttachAsModified(currentIndividual, this.ChangeSet.GetOriginal(currentIndividual));
        }
        
        public void DeleteIndividual(Individual individual)
        {
            if ((individual.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(individual, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Individuals.Attach(individual);
                this.ObjectContext.Individuals.DeleteObject(individual);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'JobCandidates' query.
        public IQueryable<JobCandidate> GetJobCandidates()
        {
            return this.ObjectContext.JobCandidates;
        }
        
        public void InsertJobCandidate(JobCandidate jobCandidate)
        {
            if ((jobCandidate.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(jobCandidate, EntityState.Added);
            }
            else
            {
                this.ObjectContext.JobCandidates.AddObject(jobCandidate);
            }
        }
        
        public void UpdateJobCandidate(JobCandidate currentJobCandidate)
        {
            this.ObjectContext.JobCandidates.AttachAsModified(currentJobCandidate, this.ChangeSet.GetOriginal(currentJobCandidate));
        }
        
        public void DeleteJobCandidate(JobCandidate jobCandidate)
        {
            if ((jobCandidate.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(jobCandidate, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.JobCandidates.Attach(jobCandidate);
                this.ObjectContext.JobCandidates.DeleteObject(jobCandidate);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Locations' query.
        public IQueryable<Location> GetLocations()
        {
            return this.ObjectContext.Locations;
        }
        
        public void InsertLocation(Location location)
        {
            if ((location.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(location, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Locations.AddObject(location);
            }
        }
        
        public void UpdateLocation(Location currentLocation)
        {
            this.ObjectContext.Locations.AttachAsModified(currentLocation, this.ChangeSet.GetOriginal(currentLocation));
        }
        
        public void DeleteLocation(Location location)
        {
            if ((location.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(location, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Locations.Attach(location);
                this.ObjectContext.Locations.DeleteObject(location);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Products' query.
        public IQueryable<Product> GetProducts()
        {
            return this.ObjectContext.Products;
        }
        
        public void InsertProduct(Product product)
        {
            if ((product.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Products.AddObject(product);
            }
        }
        
        public void UpdateProduct(Product currentProduct)
        {
            this.ObjectContext.Products.AttachAsModified(currentProduct, this.ChangeSet.GetOriginal(currentProduct));
        }
        
        public void DeleteProduct(Product product)
        {
            if ((product.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(product, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Products.Attach(product);
                this.ObjectContext.Products.DeleteObject(product);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductCategories' query.
        public IQueryable<ProductCategory> GetProductCategories()
        {
            return this.ObjectContext.ProductCategories;
        }
        
        public void InsertProductCategory(ProductCategory productCategory)
        {
            if ((productCategory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productCategory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductCategories.AddObject(productCategory);
            }
        }
        
        public void UpdateProductCategory(ProductCategory currentProductCategory)
        {
            this.ObjectContext.ProductCategories.AttachAsModified(currentProductCategory, this.ChangeSet.GetOriginal(currentProductCategory));
        }
        
        public void DeleteProductCategory(ProductCategory productCategory)
        {
            if ((productCategory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productCategory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductCategories.Attach(productCategory);
                this.ObjectContext.ProductCategories.DeleteObject(productCategory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductCostHistories' query.
        public IQueryable<ProductCostHistory> GetProductCostHistories()
        {
            return this.ObjectContext.ProductCostHistories;
        }
        
        public void InsertProductCostHistory(ProductCostHistory productCostHistory)
        {
            if ((productCostHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productCostHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductCostHistories.AddObject(productCostHistory);
            }
        }
        
        public void UpdateProductCostHistory(ProductCostHistory currentProductCostHistory)
        {
            this.ObjectContext.ProductCostHistories.AttachAsModified(currentProductCostHistory, this.ChangeSet.GetOriginal(currentProductCostHistory));
        }
        
        public void DeleteProductCostHistory(ProductCostHistory productCostHistory)
        {
            if ((productCostHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productCostHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductCostHistories.Attach(productCostHistory);
                this.ObjectContext.ProductCostHistories.DeleteObject(productCostHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductDescriptions' query.
        public IQueryable<ProductDescription> GetProductDescriptions()
        {
            return this.ObjectContext.ProductDescriptions;
        }
        
        public void InsertProductDescription(ProductDescription productDescription)
        {
            if ((productDescription.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productDescription, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductDescriptions.AddObject(productDescription);
            }
        }
        
        public void UpdateProductDescription(ProductDescription currentProductDescription)
        {
            this.ObjectContext.ProductDescriptions.AttachAsModified(currentProductDescription, this.ChangeSet.GetOriginal(currentProductDescription));
        }
        
        public void DeleteProductDescription(ProductDescription productDescription)
        {
            if ((productDescription.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productDescription, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductDescriptions.Attach(productDescription);
                this.ObjectContext.ProductDescriptions.DeleteObject(productDescription);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductDocuments' query.
        public IQueryable<ProductDocument> GetProductDocuments()
        {
            return this.ObjectContext.ProductDocuments;
        }
        
        public void InsertProductDocument(ProductDocument productDocument)
        {
            if ((productDocument.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productDocument, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductDocuments.AddObject(productDocument);
            }
        }
        
        public void UpdateProductDocument(ProductDocument currentProductDocument)
        {
            this.ObjectContext.ProductDocuments.AttachAsModified(currentProductDocument, this.ChangeSet.GetOriginal(currentProductDocument));
        }
        
        public void DeleteProductDocument(ProductDocument productDocument)
        {
            if ((productDocument.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productDocument, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductDocuments.Attach(productDocument);
                this.ObjectContext.ProductDocuments.DeleteObject(productDocument);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductInventories' query.
        public IQueryable<ProductInventory> GetProductInventories()
        {
            return this.ObjectContext.ProductInventories;
        }
        
        public void InsertProductInventory(ProductInventory productInventory)
        {
            if ((productInventory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productInventory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductInventories.AddObject(productInventory);
            }
        }
        
        public void UpdateProductInventory(ProductInventory currentProductInventory)
        {
            this.ObjectContext.ProductInventories.AttachAsModified(currentProductInventory, this.ChangeSet.GetOriginal(currentProductInventory));
        }
        
        public void DeleteProductInventory(ProductInventory productInventory)
        {
            if ((productInventory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productInventory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductInventories.Attach(productInventory);
                this.ObjectContext.ProductInventories.DeleteObject(productInventory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductListPriceHistories' query.
        public IQueryable<ProductListPriceHistory> GetProductListPriceHistories()
        {
            return this.ObjectContext.ProductListPriceHistories;
        }
        
        public void InsertProductListPriceHistory(ProductListPriceHistory productListPriceHistory)
        {
            if ((productListPriceHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productListPriceHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductListPriceHistories.AddObject(productListPriceHistory);
            }
        }
        
        public void UpdateProductListPriceHistory(ProductListPriceHistory currentProductListPriceHistory)
        {
            this.ObjectContext.ProductListPriceHistories.AttachAsModified(currentProductListPriceHistory, this.ChangeSet.GetOriginal(currentProductListPriceHistory));
        }
        
        public void DeleteProductListPriceHistory(ProductListPriceHistory productListPriceHistory)
        {
            if ((productListPriceHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productListPriceHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductListPriceHistories.Attach(productListPriceHistory);
                this.ObjectContext.ProductListPriceHistories.DeleteObject(productListPriceHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductModels' query.
        public IQueryable<ProductModel> GetProductModels()
        {
            return this.ObjectContext.ProductModels;
        }
        
        public void InsertProductModel(ProductModel productModel)
        {
            if ((productModel.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productModel, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductModels.AddObject(productModel);
            }
        }
        
        public void UpdateProductModel(ProductModel currentProductModel)
        {
            this.ObjectContext.ProductModels.AttachAsModified(currentProductModel, this.ChangeSet.GetOriginal(currentProductModel));
        }
        
        public void DeleteProductModel(ProductModel productModel)
        {
            if ((productModel.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productModel, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductModels.Attach(productModel);
                this.ObjectContext.ProductModels.DeleteObject(productModel);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductModelIllustrations' query.
        public IQueryable<ProductModelIllustration> GetProductModelIllustrations()
        {
            return this.ObjectContext.ProductModelIllustrations;
        }
        
        public void InsertProductModelIllustration(ProductModelIllustration productModelIllustration)
        {
            if ((productModelIllustration.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productModelIllustration, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductModelIllustrations.AddObject(productModelIllustration);
            }
        }
        
        public void UpdateProductModelIllustration(ProductModelIllustration currentProductModelIllustration)
        {
            this.ObjectContext.ProductModelIllustrations.AttachAsModified(currentProductModelIllustration, this.ChangeSet.GetOriginal(currentProductModelIllustration));
        }
        
        public void DeleteProductModelIllustration(ProductModelIllustration productModelIllustration)
        {
            if ((productModelIllustration.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productModelIllustration, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductModelIllustrations.Attach(productModelIllustration);
                this.ObjectContext.ProductModelIllustrations.DeleteObject(productModelIllustration);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductModelProductDescriptionCultures' query.
        public IQueryable<ProductModelProductDescriptionCulture> GetProductModelProductDescriptionCultures()
        {
            return this.ObjectContext.ProductModelProductDescriptionCultures;
        }
        
        public void InsertProductModelProductDescriptionCulture(ProductModelProductDescriptionCulture productModelProductDescriptionCulture)
        {
            if ((productModelProductDescriptionCulture.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productModelProductDescriptionCulture, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductModelProductDescriptionCultures.AddObject(productModelProductDescriptionCulture);
            }
        }
        
        public void UpdateProductModelProductDescriptionCulture(ProductModelProductDescriptionCulture currentProductModelProductDescriptionCulture)
        {
            this.ObjectContext.ProductModelProductDescriptionCultures.AttachAsModified(currentProductModelProductDescriptionCulture, this.ChangeSet.GetOriginal(currentProductModelProductDescriptionCulture));
        }
        
        public void DeleteProductModelProductDescriptionCulture(ProductModelProductDescriptionCulture productModelProductDescriptionCulture)
        {
            if ((productModelProductDescriptionCulture.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productModelProductDescriptionCulture, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductModelProductDescriptionCultures.Attach(productModelProductDescriptionCulture);
                this.ObjectContext.ProductModelProductDescriptionCultures.DeleteObject(productModelProductDescriptionCulture);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductPhotoes' query.
        public IQueryable<ProductPhoto> GetProductPhotoes()
        {
            return this.ObjectContext.ProductPhotoes;
        }
        
        public void InsertProductPhoto(ProductPhoto productPhoto)
        {
            if ((productPhoto.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productPhoto, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductPhotoes.AddObject(productPhoto);
            }
        }
        
        public void UpdateProductPhoto(ProductPhoto currentProductPhoto)
        {
            this.ObjectContext.ProductPhotoes.AttachAsModified(currentProductPhoto, this.ChangeSet.GetOriginal(currentProductPhoto));
        }
        
        public void DeleteProductPhoto(ProductPhoto productPhoto)
        {
            if ((productPhoto.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productPhoto, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductPhotoes.Attach(productPhoto);
                this.ObjectContext.ProductPhotoes.DeleteObject(productPhoto);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductProductPhotoes' query.
        public IQueryable<ProductProductPhoto> GetProductProductPhotoes()
        {
            return this.ObjectContext.ProductProductPhotoes;
        }
        
        public void InsertProductProductPhoto(ProductProductPhoto productProductPhoto)
        {
            if ((productProductPhoto.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productProductPhoto, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductProductPhotoes.AddObject(productProductPhoto);
            }
        }
        
        public void UpdateProductProductPhoto(ProductProductPhoto currentProductProductPhoto)
        {
            this.ObjectContext.ProductProductPhotoes.AttachAsModified(currentProductProductPhoto, this.ChangeSet.GetOriginal(currentProductProductPhoto));
        }
        
        public void DeleteProductProductPhoto(ProductProductPhoto productProductPhoto)
        {
            if ((productProductPhoto.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productProductPhoto, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductProductPhotoes.Attach(productProductPhoto);
                this.ObjectContext.ProductProductPhotoes.DeleteObject(productProductPhoto);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductReviews' query.
        public IQueryable<ProductReview> GetProductReviews()
        {
            return this.ObjectContext.ProductReviews;
        }
        
        public void InsertProductReview(ProductReview productReview)
        {
            if ((productReview.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productReview, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductReviews.AddObject(productReview);
            }
        }
        
        public void UpdateProductReview(ProductReview currentProductReview)
        {
            this.ObjectContext.ProductReviews.AttachAsModified(currentProductReview, this.ChangeSet.GetOriginal(currentProductReview));
        }
        
        public void DeleteProductReview(ProductReview productReview)
        {
            if ((productReview.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productReview, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductReviews.Attach(productReview);
                this.ObjectContext.ProductReviews.DeleteObject(productReview);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductSubcategories' query.
        public IQueryable<ProductSubcategory> GetProductSubcategories()
        {
            return this.ObjectContext.ProductSubcategories;
        }
        
        public void InsertProductSubcategory(ProductSubcategory productSubcategory)
        {
            if ((productSubcategory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productSubcategory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductSubcategories.AddObject(productSubcategory);
            }
        }
        
        public void UpdateProductSubcategory(ProductSubcategory currentProductSubcategory)
        {
            this.ObjectContext.ProductSubcategories.AttachAsModified(currentProductSubcategory, this.ChangeSet.GetOriginal(currentProductSubcategory));
        }
        
        public void DeleteProductSubcategory(ProductSubcategory productSubcategory)
        {
            if ((productSubcategory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productSubcategory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductSubcategories.Attach(productSubcategory);
                this.ObjectContext.ProductSubcategories.DeleteObject(productSubcategory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ProductVendors' query.
        public IQueryable<ProductVendor> GetProductVendors()
        {
            return this.ObjectContext.ProductVendors;
        }
        
        public void InsertProductVendor(ProductVendor productVendor)
        {
            if ((productVendor.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productVendor, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ProductVendors.AddObject(productVendor);
            }
        }
        
        public void UpdateProductVendor(ProductVendor currentProductVendor)
        {
            this.ObjectContext.ProductVendors.AttachAsModified(currentProductVendor, this.ChangeSet.GetOriginal(currentProductVendor));
        }
        
        public void DeleteProductVendor(ProductVendor productVendor)
        {
            if ((productVendor.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(productVendor, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ProductVendors.Attach(productVendor);
                this.ObjectContext.ProductVendors.DeleteObject(productVendor);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'PurchaseOrders' query.
        public IQueryable<PurchaseOrder> GetPurchaseOrders()
        {
            return this.ObjectContext.PurchaseOrders;
        }
        
        public void InsertPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            if ((purchaseOrder.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrder, EntityState.Added);
            }
            else
            {
                this.ObjectContext.PurchaseOrders.AddObject(purchaseOrder);
            }
        }
        
        public void UpdatePurchaseOrder(PurchaseOrder currentPurchaseOrder)
        {
            this.ObjectContext.PurchaseOrders.AttachAsModified(currentPurchaseOrder, this.ChangeSet.GetOriginal(currentPurchaseOrder));
        }
        
        public void DeletePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            if ((purchaseOrder.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrder, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.PurchaseOrders.Attach(purchaseOrder);
                this.ObjectContext.PurchaseOrders.DeleteObject(purchaseOrder);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'PurchaseOrderDetails' query.
        public IQueryable<PurchaseOrderDetail> GetPurchaseOrderDetails()
        {
            return this.ObjectContext.PurchaseOrderDetails;
        }
        
        public void InsertPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            if ((purchaseOrderDetail.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrderDetail, EntityState.Added);
            }
            else
            {
                this.ObjectContext.PurchaseOrderDetails.AddObject(purchaseOrderDetail);
            }
        }
        
        public void UpdatePurchaseOrderDetail(PurchaseOrderDetail currentPurchaseOrderDetail)
        {
            this.ObjectContext.PurchaseOrderDetails.AttachAsModified(currentPurchaseOrderDetail, this.ChangeSet.GetOriginal(currentPurchaseOrderDetail));
        }
        
        public void DeletePurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            if ((purchaseOrderDetail.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(purchaseOrderDetail, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.PurchaseOrderDetails.Attach(purchaseOrderDetail);
                this.ObjectContext.PurchaseOrderDetails.DeleteObject(purchaseOrderDetail);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesOrderDetails' query.
        public IQueryable<SalesOrderDetail> GetSalesOrderDetails()
        {
            return this.ObjectContext.SalesOrderDetails;
        }
        
        public void InsertSalesOrderDetail(SalesOrderDetail salesOrderDetail)
        {
            if ((salesOrderDetail.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderDetail, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesOrderDetails.AddObject(salesOrderDetail);
            }
        }
        
        public void UpdateSalesOrderDetail(SalesOrderDetail currentSalesOrderDetail)
        {
            this.ObjectContext.SalesOrderDetails.AttachAsModified(currentSalesOrderDetail, this.ChangeSet.GetOriginal(currentSalesOrderDetail));
        }
        
        public void DeleteSalesOrderDetail(SalesOrderDetail salesOrderDetail)
        {
            if ((salesOrderDetail.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderDetail, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesOrderDetails.Attach(salesOrderDetail);
                this.ObjectContext.SalesOrderDetails.DeleteObject(salesOrderDetail);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesOrderHeaders' query.
        public IQueryable<SalesOrderHeader> GetSalesOrderHeaders()
        {
            return this.ObjectContext.SalesOrderHeaders;
        }
        
        public void InsertSalesOrderHeader(SalesOrderHeader salesOrderHeader)
        {
            if ((salesOrderHeader.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeader, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesOrderHeaders.AddObject(salesOrderHeader);
            }
        }
        
        public void UpdateSalesOrderHeader(SalesOrderHeader currentSalesOrderHeader)
        {
            this.ObjectContext.SalesOrderHeaders.AttachAsModified(currentSalesOrderHeader, this.ChangeSet.GetOriginal(currentSalesOrderHeader));
        }
        
        public void DeleteSalesOrderHeader(SalesOrderHeader salesOrderHeader)
        {
            if ((salesOrderHeader.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeader, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesOrderHeaders.Attach(salesOrderHeader);
                this.ObjectContext.SalesOrderHeaders.DeleteObject(salesOrderHeader);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesOrderHeaderSalesReasons' query.
        public IQueryable<SalesOrderHeaderSalesReason> GetSalesOrderHeaderSalesReasons()
        {
            return this.ObjectContext.SalesOrderHeaderSalesReasons;
        }
        
        public void InsertSalesOrderHeaderSalesReason(SalesOrderHeaderSalesReason salesOrderHeaderSalesReason)
        {
            if ((salesOrderHeaderSalesReason.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeaderSalesReason, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesOrderHeaderSalesReasons.AddObject(salesOrderHeaderSalesReason);
            }
        }
        
        public void UpdateSalesOrderHeaderSalesReason(SalesOrderHeaderSalesReason currentSalesOrderHeaderSalesReason)
        {
            this.ObjectContext.SalesOrderHeaderSalesReasons.AttachAsModified(currentSalesOrderHeaderSalesReason, this.ChangeSet.GetOriginal(currentSalesOrderHeaderSalesReason));
        }
        
        public void DeleteSalesOrderHeaderSalesReason(SalesOrderHeaderSalesReason salesOrderHeaderSalesReason)
        {
            if ((salesOrderHeaderSalesReason.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesOrderHeaderSalesReason, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesOrderHeaderSalesReasons.Attach(salesOrderHeaderSalesReason);
                this.ObjectContext.SalesOrderHeaderSalesReasons.DeleteObject(salesOrderHeaderSalesReason);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesPersons' query.
        public IQueryable<SalesPerson> GetSalesPersons()
        {
            return this.ObjectContext.SalesPersons;
        }
        
        public void InsertSalesPerson(SalesPerson salesPerson)
        {
            if ((salesPerson.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesPerson, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesPersons.AddObject(salesPerson);
            }
        }
        
        public void UpdateSalesPerson(SalesPerson currentSalesPerson)
        {
            this.ObjectContext.SalesPersons.AttachAsModified(currentSalesPerson, this.ChangeSet.GetOriginal(currentSalesPerson));
        }
        
        public void DeleteSalesPerson(SalesPerson salesPerson)
        {
            if ((salesPerson.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesPerson, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesPersons.Attach(salesPerson);
                this.ObjectContext.SalesPersons.DeleteObject(salesPerson);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesPersonQuotaHistories' query.
        public IQueryable<SalesPersonQuotaHistory> GetSalesPersonQuotaHistories()
        {
            return this.ObjectContext.SalesPersonQuotaHistories;
        }
        
        public void InsertSalesPersonQuotaHistory(SalesPersonQuotaHistory salesPersonQuotaHistory)
        {
            if ((salesPersonQuotaHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesPersonQuotaHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesPersonQuotaHistories.AddObject(salesPersonQuotaHistory);
            }
        }
        
        public void UpdateSalesPersonQuotaHistory(SalesPersonQuotaHistory currentSalesPersonQuotaHistory)
        {
            this.ObjectContext.SalesPersonQuotaHistories.AttachAsModified(currentSalesPersonQuotaHistory, this.ChangeSet.GetOriginal(currentSalesPersonQuotaHistory));
        }
        
        public void DeleteSalesPersonQuotaHistory(SalesPersonQuotaHistory salesPersonQuotaHistory)
        {
            if ((salesPersonQuotaHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesPersonQuotaHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesPersonQuotaHistories.Attach(salesPersonQuotaHistory);
                this.ObjectContext.SalesPersonQuotaHistories.DeleteObject(salesPersonQuotaHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesReasons' query.
        public IQueryable<SalesReason> GetSalesReasons()
        {
            return this.ObjectContext.SalesReasons;
        }
        
        public void InsertSalesReason(SalesReason salesReason)
        {
            if ((salesReason.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesReason, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesReasons.AddObject(salesReason);
            }
        }
        
        public void UpdateSalesReason(SalesReason currentSalesReason)
        {
            this.ObjectContext.SalesReasons.AttachAsModified(currentSalesReason, this.ChangeSet.GetOriginal(currentSalesReason));
        }
        
        public void DeleteSalesReason(SalesReason salesReason)
        {
            if ((salesReason.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesReason, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesReasons.Attach(salesReason);
                this.ObjectContext.SalesReasons.DeleteObject(salesReason);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesTaxRates' query.
        public IQueryable<SalesTaxRate> GetSalesTaxRates()
        {
            return this.ObjectContext.SalesTaxRates;
        }
        
        public void InsertSalesTaxRate(SalesTaxRate salesTaxRate)
        {
            if ((salesTaxRate.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesTaxRate, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesTaxRates.AddObject(salesTaxRate);
            }
        }
        
        public void UpdateSalesTaxRate(SalesTaxRate currentSalesTaxRate)
        {
            this.ObjectContext.SalesTaxRates.AttachAsModified(currentSalesTaxRate, this.ChangeSet.GetOriginal(currentSalesTaxRate));
        }
        
        public void DeleteSalesTaxRate(SalesTaxRate salesTaxRate)
        {
            if ((salesTaxRate.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesTaxRate, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesTaxRates.Attach(salesTaxRate);
                this.ObjectContext.SalesTaxRates.DeleteObject(salesTaxRate);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesTerritories' query.
        public IQueryable<SalesTerritory> GetSalesTerritories()
        {
            return this.ObjectContext.SalesTerritories;
        }
        
        public void InsertSalesTerritory(SalesTerritory salesTerritory)
        {
            if ((salesTerritory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesTerritories.AddObject(salesTerritory);
            }
        }
        
        public void UpdateSalesTerritory(SalesTerritory currentSalesTerritory)
        {
            this.ObjectContext.SalesTerritories.AttachAsModified(currentSalesTerritory, this.ChangeSet.GetOriginal(currentSalesTerritory));
        }
        
        public void DeleteSalesTerritory(SalesTerritory salesTerritory)
        {
            if ((salesTerritory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesTerritories.Attach(salesTerritory);
                this.ObjectContext.SalesTerritories.DeleteObject(salesTerritory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SalesTerritoryHistories' query.
        public IQueryable<SalesTerritoryHistory> GetSalesTerritoryHistories()
        {
            return this.ObjectContext.SalesTerritoryHistories;
        }
        
        public void InsertSalesTerritoryHistory(SalesTerritoryHistory salesTerritoryHistory)
        {
            if ((salesTerritoryHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritoryHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SalesTerritoryHistories.AddObject(salesTerritoryHistory);
            }
        }
        
        public void UpdateSalesTerritoryHistory(SalesTerritoryHistory currentSalesTerritoryHistory)
        {
            this.ObjectContext.SalesTerritoryHistories.AttachAsModified(currentSalesTerritoryHistory, this.ChangeSet.GetOriginal(currentSalesTerritoryHistory));
        }
        
        public void DeleteSalesTerritoryHistory(SalesTerritoryHistory salesTerritoryHistory)
        {
            if ((salesTerritoryHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(salesTerritoryHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SalesTerritoryHistories.Attach(salesTerritoryHistory);
                this.ObjectContext.SalesTerritoryHistories.DeleteObject(salesTerritoryHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ScrapReasons' query.
        public IQueryable<ScrapReason> GetScrapReasons()
        {
            return this.ObjectContext.ScrapReasons;
        }
        
        public void InsertScrapReason(ScrapReason scrapReason)
        {
            if ((scrapReason.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(scrapReason, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ScrapReasons.AddObject(scrapReason);
            }
        }
        
        public void UpdateScrapReason(ScrapReason currentScrapReason)
        {
            this.ObjectContext.ScrapReasons.AttachAsModified(currentScrapReason, this.ChangeSet.GetOriginal(currentScrapReason));
        }
        
        public void DeleteScrapReason(ScrapReason scrapReason)
        {
            if ((scrapReason.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(scrapReason, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ScrapReasons.Attach(scrapReason);
                this.ObjectContext.ScrapReasons.DeleteObject(scrapReason);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Shifts' query.
        public IQueryable<Shift> GetShifts()
        {
            return this.ObjectContext.Shifts;
        }
        
        public void InsertShift(Shift shift)
        {
            if ((shift.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shift, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Shifts.AddObject(shift);
            }
        }
        
        public void UpdateShift(Shift currentShift)
        {
            this.ObjectContext.Shifts.AttachAsModified(currentShift, this.ChangeSet.GetOriginal(currentShift));
        }
        
        public void DeleteShift(Shift shift)
        {
            if ((shift.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shift, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Shifts.Attach(shift);
                this.ObjectContext.Shifts.DeleteObject(shift);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ShipMethods' query.
        public IQueryable<ShipMethod> GetShipMethods()
        {
            return this.ObjectContext.ShipMethods;
        }
        
        public void InsertShipMethod(ShipMethod shipMethod)
        {
            if ((shipMethod.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shipMethod, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ShipMethods.AddObject(shipMethod);
            }
        }
        
        public void UpdateShipMethod(ShipMethod currentShipMethod)
        {
            this.ObjectContext.ShipMethods.AttachAsModified(currentShipMethod, this.ChangeSet.GetOriginal(currentShipMethod));
        }
        
        public void DeleteShipMethod(ShipMethod shipMethod)
        {
            if ((shipMethod.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shipMethod, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ShipMethods.Attach(shipMethod);
                this.ObjectContext.ShipMethods.DeleteObject(shipMethod);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'ShoppingCartItems' query.
        public IQueryable<ShoppingCartItem> GetShoppingCartItems()
        {
            return this.ObjectContext.ShoppingCartItems;
        }
        
        public void InsertShoppingCartItem(ShoppingCartItem shoppingCartItem)
        {
            if ((shoppingCartItem.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shoppingCartItem, EntityState.Added);
            }
            else
            {
                this.ObjectContext.ShoppingCartItems.AddObject(shoppingCartItem);
            }
        }
        
        public void UpdateShoppingCartItem(ShoppingCartItem currentShoppingCartItem)
        {
            this.ObjectContext.ShoppingCartItems.AttachAsModified(currentShoppingCartItem, this.ChangeSet.GetOriginal(currentShoppingCartItem));
        }
        
        public void DeleteShoppingCartItem(ShoppingCartItem shoppingCartItem)
        {
            if ((shoppingCartItem.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(shoppingCartItem, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.ShoppingCartItems.Attach(shoppingCartItem);
                this.ObjectContext.ShoppingCartItems.DeleteObject(shoppingCartItem);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SpecialOffers' query.
        public IQueryable<SpecialOffer> GetSpecialOffers()
        {
            return this.ObjectContext.SpecialOffers;
        }
        
        public void InsertSpecialOffer(SpecialOffer specialOffer)
        {
            if ((specialOffer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(specialOffer, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SpecialOffers.AddObject(specialOffer);
            }
        }
        
        public void UpdateSpecialOffer(SpecialOffer currentSpecialOffer)
        {
            this.ObjectContext.SpecialOffers.AttachAsModified(currentSpecialOffer, this.ChangeSet.GetOriginal(currentSpecialOffer));
        }
        
        public void DeleteSpecialOffer(SpecialOffer specialOffer)
        {
            if ((specialOffer.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(specialOffer, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SpecialOffers.Attach(specialOffer);
                this.ObjectContext.SpecialOffers.DeleteObject(specialOffer);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'SpecialOfferProducts' query.
        public IQueryable<SpecialOfferProduct> GetSpecialOfferProducts()
        {
            return this.ObjectContext.SpecialOfferProducts;
        }
        
        public void InsertSpecialOfferProduct(SpecialOfferProduct specialOfferProduct)
        {
            if ((specialOfferProduct.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(specialOfferProduct, EntityState.Added);
            }
            else
            {
                this.ObjectContext.SpecialOfferProducts.AddObject(specialOfferProduct);
            }
        }
        
        public void UpdateSpecialOfferProduct(SpecialOfferProduct currentSpecialOfferProduct)
        {
            this.ObjectContext.SpecialOfferProducts.AttachAsModified(currentSpecialOfferProduct, this.ChangeSet.GetOriginal(currentSpecialOfferProduct));
        }
        
        public void DeleteSpecialOfferProduct(SpecialOfferProduct specialOfferProduct)
        {
            if ((specialOfferProduct.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(specialOfferProduct, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.SpecialOfferProducts.Attach(specialOfferProduct);
                this.ObjectContext.SpecialOfferProducts.DeleteObject(specialOfferProduct);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'StateProvinces' query.
        public IQueryable<StateProvince> GetStateProvinces()
        {
            return this.ObjectContext.StateProvinces;
        }
        
        public void InsertStateProvince(StateProvince stateProvince)
        {
            if ((stateProvince.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(stateProvince, EntityState.Added);
            }
            else
            {
                this.ObjectContext.StateProvinces.AddObject(stateProvince);
            }
        }
        
        public void UpdateStateProvince(StateProvince currentStateProvince)
        {
            this.ObjectContext.StateProvinces.AttachAsModified(currentStateProvince, this.ChangeSet.GetOriginal(currentStateProvince));
        }
        
        public void DeleteStateProvince(StateProvince stateProvince)
        {
            if ((stateProvince.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(stateProvince, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.StateProvinces.Attach(stateProvince);
                this.ObjectContext.StateProvinces.DeleteObject(stateProvince);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Stores' query.
        public IQueryable<Store> GetStores()
        {
            return this.ObjectContext.Stores;
        }
        
        public void InsertStore(Store store)
        {
            if ((store.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(store, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Stores.AddObject(store);
            }
        }
        
        public void UpdateStore(Store currentStore)
        {
            this.ObjectContext.Stores.AttachAsModified(currentStore, this.ChangeSet.GetOriginal(currentStore));
        }
        
        public void DeleteStore(Store store)
        {
            if ((store.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(store, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Stores.Attach(store);
                this.ObjectContext.Stores.DeleteObject(store);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'StoreContacts' query.
        public IQueryable<StoreContact> GetStoreContacts()
        {
            return this.ObjectContext.StoreContacts;
        }
        
        public void InsertStoreContact(StoreContact storeContact)
        {
            if ((storeContact.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(storeContact, EntityState.Added);
            }
            else
            {
                this.ObjectContext.StoreContacts.AddObject(storeContact);
            }
        }
        
        public void UpdateStoreContact(StoreContact currentStoreContact)
        {
            this.ObjectContext.StoreContacts.AttachAsModified(currentStoreContact, this.ChangeSet.GetOriginal(currentStoreContact));
        }
        
        public void DeleteStoreContact(StoreContact storeContact)
        {
            if ((storeContact.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(storeContact, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.StoreContacts.Attach(storeContact);
                this.ObjectContext.StoreContacts.DeleteObject(storeContact);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'sysdiagrams' query.
        public IQueryable<sysdiagram> GetSysdiagrams()
        {
            return this.ObjectContext.sysdiagrams;
        }
        
        public void InsertSysdiagram(sysdiagram sysdiagram)
        {
            if ((sysdiagram.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(sysdiagram, EntityState.Added);
            }
            else
            {
                this.ObjectContext.sysdiagrams.AddObject(sysdiagram);
            }
        }
        
        public void UpdateSysdiagram(sysdiagram currentsysdiagram)
        {
            this.ObjectContext.sysdiagrams.AttachAsModified(currentsysdiagram, this.ChangeSet.GetOriginal(currentsysdiagram));
        }
        
        public void DeleteSysdiagram(sysdiagram sysdiagram)
        {
            if ((sysdiagram.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(sysdiagram, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.sysdiagrams.Attach(sysdiagram);
                this.ObjectContext.sysdiagrams.DeleteObject(sysdiagram);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'TransactionHistories' query.
        public IQueryable<TransactionHistory> GetTransactionHistories()
        {
            return this.ObjectContext.TransactionHistories;
        }
        
        public void InsertTransactionHistory(TransactionHistory transactionHistory)
        {
            if ((transactionHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistory, EntityState.Added);
            }
            else
            {
                this.ObjectContext.TransactionHistories.AddObject(transactionHistory);
            }
        }
        
        public void UpdateTransactionHistory(TransactionHistory currentTransactionHistory)
        {
            this.ObjectContext.TransactionHistories.AttachAsModified(currentTransactionHistory, this.ChangeSet.GetOriginal(currentTransactionHistory));
        }
        
        public void DeleteTransactionHistory(TransactionHistory transactionHistory)
        {
            if ((transactionHistory.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistory, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.TransactionHistories.Attach(transactionHistory);
                this.ObjectContext.TransactionHistories.DeleteObject(transactionHistory);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'TransactionHistoryArchives' query.
        public IQueryable<TransactionHistoryArchive> GetTransactionHistoryArchives()
        {
            return this.ObjectContext.TransactionHistoryArchives;
        }
        
        public void InsertTransactionHistoryArchive(TransactionHistoryArchive transactionHistoryArchive)
        {
            if ((transactionHistoryArchive.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistoryArchive, EntityState.Added);
            }
            else
            {
                this.ObjectContext.TransactionHistoryArchives.AddObject(transactionHistoryArchive);
            }
        }
        
        public void UpdateTransactionHistoryArchive(TransactionHistoryArchive currentTransactionHistoryArchive)
        {
            this.ObjectContext.TransactionHistoryArchives.AttachAsModified(currentTransactionHistoryArchive, this.ChangeSet.GetOriginal(currentTransactionHistoryArchive));
        }
        
        public void DeleteTransactionHistoryArchive(TransactionHistoryArchive transactionHistoryArchive)
        {
            if ((transactionHistoryArchive.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(transactionHistoryArchive, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.TransactionHistoryArchives.Attach(transactionHistoryArchive);
                this.ObjectContext.TransactionHistoryArchives.DeleteObject(transactionHistoryArchive);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'UnitMeasures' query.
        public IQueryable<UnitMeasure> GetUnitMeasures()
        {
            return this.ObjectContext.UnitMeasures;
        }
        
        public void InsertUnitMeasure(UnitMeasure unitMeasure)
        {
            if ((unitMeasure.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(unitMeasure, EntityState.Added);
            }
            else
            {
                this.ObjectContext.UnitMeasures.AddObject(unitMeasure);
            }
        }
        
        public void UpdateUnitMeasure(UnitMeasure currentUnitMeasure)
        {
            this.ObjectContext.UnitMeasures.AttachAsModified(currentUnitMeasure, this.ChangeSet.GetOriginal(currentUnitMeasure));
        }
        
        public void DeleteUnitMeasure(UnitMeasure unitMeasure)
        {
            if ((unitMeasure.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(unitMeasure, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.UnitMeasures.Attach(unitMeasure);
                this.ObjectContext.UnitMeasures.DeleteObject(unitMeasure);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'Vendors' query.
        public IQueryable<Vendor> GetVendors()
        {
            return this.ObjectContext.Vendors;
        }
        
        public void InsertVendor(Vendor vendor)
        {
            if ((vendor.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(vendor, EntityState.Added);
            }
            else
            {
                this.ObjectContext.Vendors.AddObject(vendor);
            }
        }
        
        public void UpdateVendor(Vendor currentVendor)
        {
            this.ObjectContext.Vendors.AttachAsModified(currentVendor, this.ChangeSet.GetOriginal(currentVendor));
        }
        
        public void DeleteVendor(Vendor vendor)
        {
            if ((vendor.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(vendor, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.Vendors.Attach(vendor);
                this.ObjectContext.Vendors.DeleteObject(vendor);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'VendorAddresses' query.
        public IQueryable<VendorAddress> GetVendorAddresses()
        {
            return this.ObjectContext.VendorAddresses;
        }
        
        public void InsertVendorAddress(VendorAddress vendorAddress)
        {
            if ((vendorAddress.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(vendorAddress, EntityState.Added);
            }
            else
            {
                this.ObjectContext.VendorAddresses.AddObject(vendorAddress);
            }
        }
        
        public void UpdateVendorAddress(VendorAddress currentVendorAddress)
        {
            this.ObjectContext.VendorAddresses.AttachAsModified(currentVendorAddress, this.ChangeSet.GetOriginal(currentVendorAddress));
        }
        
        public void DeleteVendorAddress(VendorAddress vendorAddress)
        {
            if ((vendorAddress.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(vendorAddress, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.VendorAddresses.Attach(vendorAddress);
                this.ObjectContext.VendorAddresses.DeleteObject(vendorAddress);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'VendorContacts' query.
        public IQueryable<VendorContact> GetVendorContacts()
        {
            return this.ObjectContext.VendorContacts;
        }
        
        public void InsertVendorContact(VendorContact vendorContact)
        {
            if ((vendorContact.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(vendorContact, EntityState.Added);
            }
            else
            {
                this.ObjectContext.VendorContacts.AddObject(vendorContact);
            }
        }
        
        public void UpdateVendorContact(VendorContact currentVendorContact)
        {
            this.ObjectContext.VendorContacts.AttachAsModified(currentVendorContact, this.ChangeSet.GetOriginal(currentVendorContact));
        }
        
        public void DeleteVendorContact(VendorContact vendorContact)
        {
            if ((vendorContact.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(vendorContact, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.VendorContacts.Attach(vendorContact);
                this.ObjectContext.VendorContacts.DeleteObject(vendorContact);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'WorkOrders' query.
        public IQueryable<WorkOrder> GetWorkOrders()
        {
            return this.ObjectContext.WorkOrders;
        }
        
        public void InsertWorkOrder(WorkOrder workOrder)
        {
            if ((workOrder.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(workOrder, EntityState.Added);
            }
            else
            {
                this.ObjectContext.WorkOrders.AddObject(workOrder);
            }
        }
        
        public void UpdateWorkOrder(WorkOrder currentWorkOrder)
        {
            this.ObjectContext.WorkOrders.AttachAsModified(currentWorkOrder, this.ChangeSet.GetOriginal(currentWorkOrder));
        }
        
        public void DeleteWorkOrder(WorkOrder workOrder)
        {
            if ((workOrder.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(workOrder, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.WorkOrders.Attach(workOrder);
                this.ObjectContext.WorkOrders.DeleteObject(workOrder);
            }
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        // To support paging you will need to add ordering to the 'WorkOrderRoutings' query.
        public IQueryable<WorkOrderRouting> GetWorkOrderRoutings()
        {
            return this.ObjectContext.WorkOrderRoutings;
        }
        
        public void InsertWorkOrderRouting(WorkOrderRouting workOrderRouting)
        {
            if ((workOrderRouting.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(workOrderRouting, EntityState.Added);
            }
            else
            {
                this.ObjectContext.WorkOrderRoutings.AddObject(workOrderRouting);
            }
        }
        
        public void UpdateWorkOrderRouting(WorkOrderRouting currentWorkOrderRouting)
        {
            this.ObjectContext.WorkOrderRoutings.AttachAsModified(currentWorkOrderRouting, this.ChangeSet.GetOriginal(currentWorkOrderRouting));
        }
        
        public void DeleteWorkOrderRouting(WorkOrderRouting workOrderRouting)
        {
            if ((workOrderRouting.EntityState != EntityState.Detached))
            {
                this.ObjectContext.ObjectStateManager.ChangeObjectState(workOrderRouting, EntityState.Deleted);
            }
            else
            {
                this.ObjectContext.WorkOrderRoutings.Attach(workOrderRouting);
                this.ObjectContext.WorkOrderRoutings.DeleteObject(workOrderRouting);
            }
        }
    }
}
