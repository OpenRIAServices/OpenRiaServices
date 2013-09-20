
namespace BizLogic.Test
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Linq;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using DataTests.AdventureWorks.LTS;
    using Microsoft.ServiceModel.DomainServices.LinqToSql;
    
    
    // Implements application logic using the AdventureWorks context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class LTS_AdventureWorks : LinqToSqlDomainService<AdventureWorks>
    {
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Address> GetAddresses()
        {
            return this.DataContext.Addresses;
        }
        
        public void InsertAddress(Address address)
        {
            this.DataContext.Addresses.InsertOnSubmit(address);
        }
        
        public void UpdateAddress(Address currentAddress)
        {
            this.DataContext.Addresses.Attach(currentAddress, this.ChangeSet.GetOriginal(currentAddress));
        }
        
        public void DeleteAddress(Address address)
        {
            this.DataContext.Addresses.Attach(address);
            this.DataContext.Addresses.DeleteOnSubmit(address);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<AddressType> GetAddressTypes()
        {
            return this.DataContext.AddressTypes;
        }
        
        public void InsertAddressType(AddressType addressType)
        {
            this.DataContext.AddressTypes.InsertOnSubmit(addressType);
        }
        
        public void UpdateAddressType(AddressType currentAddressType)
        {
            this.DataContext.AddressTypes.Attach(currentAddressType, this.ChangeSet.GetOriginal(currentAddressType));
        }
        
        public void DeleteAddressType(AddressType addressType)
        {
            this.DataContext.AddressTypes.Attach(addressType);
            this.DataContext.AddressTypes.DeleteOnSubmit(addressType);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<BillOfMaterial> GetBillOfMaterials()
        {
            return this.DataContext.BillOfMaterials;
        }
        
        public void InsertBillOfMaterial(BillOfMaterial billOfMaterial)
        {
            this.DataContext.BillOfMaterials.InsertOnSubmit(billOfMaterial);
        }
        
        public void UpdateBillOfMaterial(BillOfMaterial currentBillOfMaterial)
        {
            this.DataContext.BillOfMaterials.Attach(currentBillOfMaterial, this.ChangeSet.GetOriginal(currentBillOfMaterial));
        }
        
        public void DeleteBillOfMaterial(BillOfMaterial billOfMaterial)
        {
            this.DataContext.BillOfMaterials.Attach(billOfMaterial);
            this.DataContext.BillOfMaterials.DeleteOnSubmit(billOfMaterial);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Contact> GetContacts()
        {
            return this.DataContext.Contacts;
        }
        
        public void InsertContact(Contact contact)
        {
            this.DataContext.Contacts.InsertOnSubmit(contact);
        }
        
        public void UpdateContact(Contact currentContact)
        {
            this.DataContext.Contacts.Attach(currentContact, this.ChangeSet.GetOriginal(currentContact));
        }
        
        public void DeleteContact(Contact contact)
        {
            this.DataContext.Contacts.Attach(contact);
            this.DataContext.Contacts.DeleteOnSubmit(contact);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ContactCreditCard> GetContactCreditCards()
        {
            return this.DataContext.ContactCreditCards;
        }
        
        public void InsertContactCreditCard(ContactCreditCard contactCreditCard)
        {
            this.DataContext.ContactCreditCards.InsertOnSubmit(contactCreditCard);
        }
        
        public void UpdateContactCreditCard(ContactCreditCard currentContactCreditCard)
        {
            this.DataContext.ContactCreditCards.Attach(currentContactCreditCard, this.ChangeSet.GetOriginal(currentContactCreditCard));
        }
        
        public void DeleteContactCreditCard(ContactCreditCard contactCreditCard)
        {
            this.DataContext.ContactCreditCards.Attach(contactCreditCard);
            this.DataContext.ContactCreditCards.DeleteOnSubmit(contactCreditCard);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ContactType> GetContactTypes()
        {
            return this.DataContext.ContactTypes;
        }
        
        public void InsertContactType(ContactType contactType)
        {
            this.DataContext.ContactTypes.InsertOnSubmit(contactType);
        }
        
        public void UpdateContactType(ContactType currentContactType)
        {
            this.DataContext.ContactTypes.Attach(currentContactType, this.ChangeSet.GetOriginal(currentContactType));
        }
        
        public void DeleteContactType(ContactType contactType)
        {
            this.DataContext.ContactTypes.Attach(contactType);
            this.DataContext.ContactTypes.DeleteOnSubmit(contactType);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<CountryRegion> GetCountryRegions()
        {
            return this.DataContext.CountryRegions;
        }
        
        public void InsertCountryRegion(CountryRegion countryRegion)
        {
            this.DataContext.CountryRegions.InsertOnSubmit(countryRegion);
        }
        
        public void UpdateCountryRegion(CountryRegion currentCountryRegion)
        {
            this.DataContext.CountryRegions.Attach(currentCountryRegion, this.ChangeSet.GetOriginal(currentCountryRegion));
        }
        
        public void DeleteCountryRegion(CountryRegion countryRegion)
        {
            this.DataContext.CountryRegions.Attach(countryRegion);
            this.DataContext.CountryRegions.DeleteOnSubmit(countryRegion);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<CountryRegionCurrency> GetCountryRegionCurrencies()
        {
            return this.DataContext.CountryRegionCurrencies;
        }
        
        public void InsertCountryRegionCurrency(CountryRegionCurrency countryRegionCurrency)
        {
            this.DataContext.CountryRegionCurrencies.InsertOnSubmit(countryRegionCurrency);
        }
        
        public void UpdateCountryRegionCurrency(CountryRegionCurrency currentCountryRegionCurrency)
        {
            this.DataContext.CountryRegionCurrencies.Attach(currentCountryRegionCurrency, this.ChangeSet.GetOriginal(currentCountryRegionCurrency));
        }
        
        public void DeleteCountryRegionCurrency(CountryRegionCurrency countryRegionCurrency)
        {
            this.DataContext.CountryRegionCurrencies.Attach(countryRegionCurrency);
            this.DataContext.CountryRegionCurrencies.DeleteOnSubmit(countryRegionCurrency);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<CreditCard> GetCreditCards()
        {
            return this.DataContext.CreditCards;
        }
        
        public void InsertCreditCard(CreditCard creditCard)
        {
            this.DataContext.CreditCards.InsertOnSubmit(creditCard);
        }
        
        public void UpdateCreditCard(CreditCard currentCreditCard)
        {
            this.DataContext.CreditCards.Attach(currentCreditCard, this.ChangeSet.GetOriginal(currentCreditCard));
        }
        
        public void DeleteCreditCard(CreditCard creditCard)
        {
            this.DataContext.CreditCards.Attach(creditCard);
            this.DataContext.CreditCards.DeleteOnSubmit(creditCard);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Culture> GetCultures()
        {
            return this.DataContext.Cultures;
        }
        
        public void InsertCulture(Culture culture)
        {
            this.DataContext.Cultures.InsertOnSubmit(culture);
        }
        
        public void UpdateCulture(Culture currentCulture)
        {
            this.DataContext.Cultures.Attach(currentCulture, this.ChangeSet.GetOriginal(currentCulture));
        }
        
        public void DeleteCulture(Culture culture)
        {
            this.DataContext.Cultures.Attach(culture);
            this.DataContext.Cultures.DeleteOnSubmit(culture);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Currency> GetCurrencies()
        {
            return this.DataContext.Currencies;
        }
        
        public void InsertCurrency(Currency currency)
        {
            this.DataContext.Currencies.InsertOnSubmit(currency);
        }
        
        public void UpdateCurrency(Currency currentCurrency)
        {
            this.DataContext.Currencies.Attach(currentCurrency, this.ChangeSet.GetOriginal(currentCurrency));
        }
        
        public void DeleteCurrency(Currency currency)
        {
            this.DataContext.Currencies.Attach(currency);
            this.DataContext.Currencies.DeleteOnSubmit(currency);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<CurrencyRate> GetCurrencyRates()
        {
            return this.DataContext.CurrencyRates;
        }
        
        public void InsertCurrencyRate(CurrencyRate currencyRate)
        {
            this.DataContext.CurrencyRates.InsertOnSubmit(currencyRate);
        }
        
        public void UpdateCurrencyRate(CurrencyRate currentCurrencyRate)
        {
            this.DataContext.CurrencyRates.Attach(currentCurrencyRate, this.ChangeSet.GetOriginal(currentCurrencyRate));
        }
        
        public void DeleteCurrencyRate(CurrencyRate currencyRate)
        {
            this.DataContext.CurrencyRates.Attach(currencyRate);
            this.DataContext.CurrencyRates.DeleteOnSubmit(currencyRate);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Customer> GetCustomers()
        {
            return this.DataContext.Customers;
        }
        
        public void InsertCustomer(Customer customer)
        {
            this.DataContext.Customers.InsertOnSubmit(customer);
        }
        
        public void UpdateCustomer(Customer currentCustomer)
        {
            this.DataContext.Customers.Attach(currentCustomer, this.ChangeSet.GetOriginal(currentCustomer));
        }
        
        public void DeleteCustomer(Customer customer)
        {
            this.DataContext.Customers.Attach(customer);
            this.DataContext.Customers.DeleteOnSubmit(customer);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<CustomerAddress> GetCustomerAddresses()
        {
            return this.DataContext.CustomerAddresses;
        }
        
        public void InsertCustomerAddress(CustomerAddress customerAddress)
        {
            this.DataContext.CustomerAddresses.InsertOnSubmit(customerAddress);
        }
        
        public void UpdateCustomerAddress(CustomerAddress currentCustomerAddress)
        {
            this.DataContext.CustomerAddresses.Attach(currentCustomerAddress, this.ChangeSet.GetOriginal(currentCustomerAddress));
        }
        
        public void DeleteCustomerAddress(CustomerAddress customerAddress)
        {
            this.DataContext.CustomerAddresses.Attach(customerAddress);
            this.DataContext.CustomerAddresses.DeleteOnSubmit(customerAddress);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Department> GetDepartments()
        {
            return this.DataContext.Departments;
        }
        
        public void InsertDepartment(Department department)
        {
            this.DataContext.Departments.InsertOnSubmit(department);
        }
        
        public void UpdateDepartment(Department currentDepartment)
        {
            this.DataContext.Departments.Attach(currentDepartment, this.ChangeSet.GetOriginal(currentDepartment));
        }
        
        public void DeleteDepartment(Department department)
        {
            this.DataContext.Departments.Attach(department);
            this.DataContext.Departments.DeleteOnSubmit(department);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Document> GetDocuments()
        {
            return this.DataContext.Documents;
        }
        
        public void InsertDocument(Document document)
        {
            this.DataContext.Documents.InsertOnSubmit(document);
        }
        
        public void UpdateDocument(Document currentDocument)
        {
            this.DataContext.Documents.Attach(currentDocument, this.ChangeSet.GetOriginal(currentDocument));
        }
        
        public void DeleteDocument(Document document)
        {
            this.DataContext.Documents.Attach(document);
            this.DataContext.Documents.DeleteOnSubmit(document);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Employee> GetEmployees()
        {
            return this.DataContext.Employees;
        }
        
        public void InsertEmployee(Employee employee)
        {
            this.DataContext.Employees.InsertOnSubmit(employee);
        }
        
        public void UpdateEmployee(Employee currentEmployee)
        {
            this.DataContext.Employees.Attach(currentEmployee, this.ChangeSet.GetOriginal(currentEmployee));
        }
        
        public void DeleteEmployee(Employee employee)
        {
            this.DataContext.Employees.Attach(employee);
            this.DataContext.Employees.DeleteOnSubmit(employee);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<EmployeeAddress> GetEmployeeAddresses()
        {
            return this.DataContext.EmployeeAddresses;
        }
        
        public void InsertEmployeeAddress(EmployeeAddress employeeAddress)
        {
            this.DataContext.EmployeeAddresses.InsertOnSubmit(employeeAddress);
        }
        
        public void UpdateEmployeeAddress(EmployeeAddress currentEmployeeAddress)
        {
            this.DataContext.EmployeeAddresses.Attach(currentEmployeeAddress, this.ChangeSet.GetOriginal(currentEmployeeAddress));
        }
        
        public void DeleteEmployeeAddress(EmployeeAddress employeeAddress)
        {
            this.DataContext.EmployeeAddresses.Attach(employeeAddress);
            this.DataContext.EmployeeAddresses.DeleteOnSubmit(employeeAddress);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<EmployeeDepartmentHistory> GetEmployeeDepartmentHistories()
        {
            return this.DataContext.EmployeeDepartmentHistories;
        }
        
        public void InsertEmployeeDepartmentHistory(EmployeeDepartmentHistory employeeDepartmentHistory)
        {
            this.DataContext.EmployeeDepartmentHistories.InsertOnSubmit(employeeDepartmentHistory);
        }
        
        public void UpdateEmployeeDepartmentHistory(EmployeeDepartmentHistory currentEmployeeDepartmentHistory)
        {
            this.DataContext.EmployeeDepartmentHistories.Attach(currentEmployeeDepartmentHistory, this.ChangeSet.GetOriginal(currentEmployeeDepartmentHistory));
        }
        
        public void DeleteEmployeeDepartmentHistory(EmployeeDepartmentHistory employeeDepartmentHistory)
        {
            this.DataContext.EmployeeDepartmentHistories.Attach(employeeDepartmentHistory);
            this.DataContext.EmployeeDepartmentHistories.DeleteOnSubmit(employeeDepartmentHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<EmployeePayHistory> GetEmployeePayHistories()
        {
            return this.DataContext.EmployeePayHistories;
        }
        
        public void InsertEmployeePayHistory(EmployeePayHistory employeePayHistory)
        {
            this.DataContext.EmployeePayHistories.InsertOnSubmit(employeePayHistory);
        }
        
        public void UpdateEmployeePayHistory(EmployeePayHistory currentEmployeePayHistory)
        {
            this.DataContext.EmployeePayHistories.Attach(currentEmployeePayHistory, this.ChangeSet.GetOriginal(currentEmployeePayHistory));
        }
        
        public void DeleteEmployeePayHistory(EmployeePayHistory employeePayHistory)
        {
            this.DataContext.EmployeePayHistories.Attach(employeePayHistory);
            this.DataContext.EmployeePayHistories.DeleteOnSubmit(employeePayHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Illustration> GetIllustrations()
        {
            return this.DataContext.Illustrations;
        }
        
        public void InsertIllustration(Illustration illustration)
        {
            this.DataContext.Illustrations.InsertOnSubmit(illustration);
        }
        
        public void UpdateIllustration(Illustration currentIllustration)
        {
            this.DataContext.Illustrations.Attach(currentIllustration, this.ChangeSet.GetOriginal(currentIllustration));
        }
        
        public void DeleteIllustration(Illustration illustration)
        {
            this.DataContext.Illustrations.Attach(illustration);
            this.DataContext.Illustrations.DeleteOnSubmit(illustration);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Individual> GetIndividuals()
        {
            return this.DataContext.Individuals;
        }
        
        public void InsertIndividual(Individual individual)
        {
            this.DataContext.Individuals.InsertOnSubmit(individual);
        }
        
        public void UpdateIndividual(Individual currentIndividual)
        {
            this.DataContext.Individuals.Attach(currentIndividual, this.ChangeSet.GetOriginal(currentIndividual));
        }
        
        public void DeleteIndividual(Individual individual)
        {
            this.DataContext.Individuals.Attach(individual);
            this.DataContext.Individuals.DeleteOnSubmit(individual);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<JobCandidate> GetJobCandidates()
        {
            return this.DataContext.JobCandidates;
        }
        
        public void InsertJobCandidate(JobCandidate jobCandidate)
        {
            this.DataContext.JobCandidates.InsertOnSubmit(jobCandidate);
        }
        
        public void UpdateJobCandidate(JobCandidate currentJobCandidate)
        {
            this.DataContext.JobCandidates.Attach(currentJobCandidate, this.ChangeSet.GetOriginal(currentJobCandidate));
        }
        
        public void DeleteJobCandidate(JobCandidate jobCandidate)
        {
            this.DataContext.JobCandidates.Attach(jobCandidate);
            this.DataContext.JobCandidates.DeleteOnSubmit(jobCandidate);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Location> GetLocations()
        {
            return this.DataContext.Locations;
        }
        
        public void InsertLocation(Location location)
        {
            this.DataContext.Locations.InsertOnSubmit(location);
        }
        
        public void UpdateLocation(Location currentLocation)
        {
            this.DataContext.Locations.Attach(currentLocation, this.ChangeSet.GetOriginal(currentLocation));
        }
        
        public void DeleteLocation(Location location)
        {
            this.DataContext.Locations.Attach(location);
            this.DataContext.Locations.DeleteOnSubmit(location);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Product> GetProducts()
        {
            return this.DataContext.Products;
        }
        
        public void InsertProduct(Product product)
        {
            this.DataContext.Products.InsertOnSubmit(product);
        }
        
        public void UpdateProduct(Product currentProduct)
        {
            this.DataContext.Products.Attach(currentProduct, this.ChangeSet.GetOriginal(currentProduct));
        }
        
        public void DeleteProduct(Product product)
        {
            this.DataContext.Products.Attach(product);
            this.DataContext.Products.DeleteOnSubmit(product);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductCategory> GetProductCategories()
        {
            return this.DataContext.ProductCategories;
        }
        
        public void InsertProductCategory(ProductCategory productCategory)
        {
            this.DataContext.ProductCategories.InsertOnSubmit(productCategory);
        }
        
        public void UpdateProductCategory(ProductCategory currentProductCategory)
        {
            this.DataContext.ProductCategories.Attach(currentProductCategory, this.ChangeSet.GetOriginal(currentProductCategory));
        }
        
        public void DeleteProductCategory(ProductCategory productCategory)
        {
            this.DataContext.ProductCategories.Attach(productCategory);
            this.DataContext.ProductCategories.DeleteOnSubmit(productCategory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductCostHistory> GetProductCostHistories()
        {
            return this.DataContext.ProductCostHistories;
        }
        
        public void InsertProductCostHistory(ProductCostHistory productCostHistory)
        {
            this.DataContext.ProductCostHistories.InsertOnSubmit(productCostHistory);
        }
        
        public void UpdateProductCostHistory(ProductCostHistory currentProductCostHistory)
        {
            this.DataContext.ProductCostHistories.Attach(currentProductCostHistory, this.ChangeSet.GetOriginal(currentProductCostHistory));
        }
        
        public void DeleteProductCostHistory(ProductCostHistory productCostHistory)
        {
            this.DataContext.ProductCostHistories.Attach(productCostHistory);
            this.DataContext.ProductCostHistories.DeleteOnSubmit(productCostHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductDescription> GetProductDescriptions()
        {
            return this.DataContext.ProductDescriptions;
        }
        
        public void InsertProductDescription(ProductDescription productDescription)
        {
            this.DataContext.ProductDescriptions.InsertOnSubmit(productDescription);
        }
        
        public void UpdateProductDescription(ProductDescription currentProductDescription)
        {
            this.DataContext.ProductDescriptions.Attach(currentProductDescription, this.ChangeSet.GetOriginal(currentProductDescription));
        }
        
        public void DeleteProductDescription(ProductDescription productDescription)
        {
            this.DataContext.ProductDescriptions.Attach(productDescription);
            this.DataContext.ProductDescriptions.DeleteOnSubmit(productDescription);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductDocument> GetProductDocuments()
        {
            return this.DataContext.ProductDocuments;
        }
        
        public void InsertProductDocument(ProductDocument productDocument)
        {
            this.DataContext.ProductDocuments.InsertOnSubmit(productDocument);
        }
        
        public void UpdateProductDocument(ProductDocument currentProductDocument)
        {
            this.DataContext.ProductDocuments.Attach(currentProductDocument, this.ChangeSet.GetOriginal(currentProductDocument));
        }
        
        public void DeleteProductDocument(ProductDocument productDocument)
        {
            this.DataContext.ProductDocuments.Attach(productDocument);
            this.DataContext.ProductDocuments.DeleteOnSubmit(productDocument);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductInventory> GetProductInventories()
        {
            return this.DataContext.ProductInventories;
        }
        
        public void InsertProductInventory(ProductInventory productInventory)
        {
            this.DataContext.ProductInventories.InsertOnSubmit(productInventory);
        }
        
        public void UpdateProductInventory(ProductInventory currentProductInventory)
        {
            this.DataContext.ProductInventories.Attach(currentProductInventory, this.ChangeSet.GetOriginal(currentProductInventory));
        }
        
        public void DeleteProductInventory(ProductInventory productInventory)
        {
            this.DataContext.ProductInventories.Attach(productInventory);
            this.DataContext.ProductInventories.DeleteOnSubmit(productInventory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductListPriceHistory> GetProductListPriceHistories()
        {
            return this.DataContext.ProductListPriceHistories;
        }
        
        public void InsertProductListPriceHistory(ProductListPriceHistory productListPriceHistory)
        {
            this.DataContext.ProductListPriceHistories.InsertOnSubmit(productListPriceHistory);
        }
        
        public void UpdateProductListPriceHistory(ProductListPriceHistory currentProductListPriceHistory)
        {
            this.DataContext.ProductListPriceHistories.Attach(currentProductListPriceHistory, this.ChangeSet.GetOriginal(currentProductListPriceHistory));
        }
        
        public void DeleteProductListPriceHistory(ProductListPriceHistory productListPriceHistory)
        {
            this.DataContext.ProductListPriceHistories.Attach(productListPriceHistory);
            this.DataContext.ProductListPriceHistories.DeleteOnSubmit(productListPriceHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductModel> GetProductModels()
        {
            return this.DataContext.ProductModels;
        }
        
        public void InsertProductModel(ProductModel productModel)
        {
            this.DataContext.ProductModels.InsertOnSubmit(productModel);
        }
        
        public void UpdateProductModel(ProductModel currentProductModel)
        {
            this.DataContext.ProductModels.Attach(currentProductModel, this.ChangeSet.GetOriginal(currentProductModel));
        }
        
        public void DeleteProductModel(ProductModel productModel)
        {
            this.DataContext.ProductModels.Attach(productModel);
            this.DataContext.ProductModels.DeleteOnSubmit(productModel);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductModelIllustration> GetProductModelIllustrations()
        {
            return this.DataContext.ProductModelIllustrations;
        }
        
        public void InsertProductModelIllustration(ProductModelIllustration productModelIllustration)
        {
            this.DataContext.ProductModelIllustrations.InsertOnSubmit(productModelIllustration);
        }
        
        public void UpdateProductModelIllustration(ProductModelIllustration currentProductModelIllustration)
        {
            this.DataContext.ProductModelIllustrations.Attach(currentProductModelIllustration, this.ChangeSet.GetOriginal(currentProductModelIllustration));
        }
        
        public void DeleteProductModelIllustration(ProductModelIllustration productModelIllustration)
        {
            this.DataContext.ProductModelIllustrations.Attach(productModelIllustration);
            this.DataContext.ProductModelIllustrations.DeleteOnSubmit(productModelIllustration);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductModelProductDescriptionCulture> GetProductModelProductDescriptionCultures()
        {
            return this.DataContext.ProductModelProductDescriptionCultures;
        }
        
        public void InsertProductModelProductDescriptionCulture(ProductModelProductDescriptionCulture productModelProductDescriptionCulture)
        {
            this.DataContext.ProductModelProductDescriptionCultures.InsertOnSubmit(productModelProductDescriptionCulture);
        }
        
        public void UpdateProductModelProductDescriptionCulture(ProductModelProductDescriptionCulture currentProductModelProductDescriptionCulture)
        {
            this.DataContext.ProductModelProductDescriptionCultures.Attach(currentProductModelProductDescriptionCulture, this.ChangeSet.GetOriginal(currentProductModelProductDescriptionCulture));
        }
        
        public void DeleteProductModelProductDescriptionCulture(ProductModelProductDescriptionCulture productModelProductDescriptionCulture)
        {
            this.DataContext.ProductModelProductDescriptionCultures.Attach(productModelProductDescriptionCulture);
            this.DataContext.ProductModelProductDescriptionCultures.DeleteOnSubmit(productModelProductDescriptionCulture);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductPhoto> GetProductPhotos()
        {
            return this.DataContext.ProductPhotos;
        }
        
        public void InsertProductPhoto(ProductPhoto productPhoto)
        {
            this.DataContext.ProductPhotos.InsertOnSubmit(productPhoto);
        }
        
        public void UpdateProductPhoto(ProductPhoto currentProductPhoto)
        {
            this.DataContext.ProductPhotos.Attach(currentProductPhoto, this.ChangeSet.GetOriginal(currentProductPhoto));
        }
        
        public void DeleteProductPhoto(ProductPhoto productPhoto)
        {
            this.DataContext.ProductPhotos.Attach(productPhoto);
            this.DataContext.ProductPhotos.DeleteOnSubmit(productPhoto);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductProductPhoto> GetProductProductPhotos()
        {
            return this.DataContext.ProductProductPhotos;
        }
        
        public void InsertProductProductPhoto(ProductProductPhoto productProductPhoto)
        {
            this.DataContext.ProductProductPhotos.InsertOnSubmit(productProductPhoto);
        }
        
        public void UpdateProductProductPhoto(ProductProductPhoto currentProductProductPhoto)
        {
            this.DataContext.ProductProductPhotos.Attach(currentProductProductPhoto, this.ChangeSet.GetOriginal(currentProductProductPhoto));
        }
        
        public void DeleteProductProductPhoto(ProductProductPhoto productProductPhoto)
        {
            this.DataContext.ProductProductPhotos.Attach(productProductPhoto);
            this.DataContext.ProductProductPhotos.DeleteOnSubmit(productProductPhoto);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductReview> GetProductReviews()
        {
            return this.DataContext.ProductReviews;
        }
        
        public void InsertProductReview(ProductReview productReview)
        {
            this.DataContext.ProductReviews.InsertOnSubmit(productReview);
        }
        
        public void UpdateProductReview(ProductReview currentProductReview)
        {
            this.DataContext.ProductReviews.Attach(currentProductReview, this.ChangeSet.GetOriginal(currentProductReview));
        }
        
        public void DeleteProductReview(ProductReview productReview)
        {
            this.DataContext.ProductReviews.Attach(productReview);
            this.DataContext.ProductReviews.DeleteOnSubmit(productReview);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductSubcategory> GetProductSubcategories()
        {
            return this.DataContext.ProductSubcategories;
        }
        
        public void InsertProductSubcategory(ProductSubcategory productSubcategory)
        {
            this.DataContext.ProductSubcategories.InsertOnSubmit(productSubcategory);
        }
        
        public void UpdateProductSubcategory(ProductSubcategory currentProductSubcategory)
        {
            this.DataContext.ProductSubcategories.Attach(currentProductSubcategory, this.ChangeSet.GetOriginal(currentProductSubcategory));
        }
        
        public void DeleteProductSubcategory(ProductSubcategory productSubcategory)
        {
            this.DataContext.ProductSubcategories.Attach(productSubcategory);
            this.DataContext.ProductSubcategories.DeleteOnSubmit(productSubcategory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ProductVendor> GetProductVendors()
        {
            return this.DataContext.ProductVendors;
        }
        
        public void InsertProductVendor(ProductVendor productVendor)
        {
            this.DataContext.ProductVendors.InsertOnSubmit(productVendor);
        }
        
        public void UpdateProductVendor(ProductVendor currentProductVendor)
        {
            this.DataContext.ProductVendors.Attach(currentProductVendor, this.ChangeSet.GetOriginal(currentProductVendor));
        }
        
        public void DeleteProductVendor(ProductVendor productVendor)
        {
            this.DataContext.ProductVendors.Attach(productVendor);
            this.DataContext.ProductVendors.DeleteOnSubmit(productVendor);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<PurchaseOrder> GetPurchaseOrders()
        {
            return this.DataContext.PurchaseOrders;
        }
        
        public void InsertPurchaseOrder(PurchaseOrder purchaseOrder)
        {
            this.DataContext.PurchaseOrders.InsertOnSubmit(purchaseOrder);
        }
        
        public void UpdatePurchaseOrder(PurchaseOrder currentPurchaseOrder)
        {
            this.DataContext.PurchaseOrders.Attach(currentPurchaseOrder, this.ChangeSet.GetOriginal(currentPurchaseOrder));
        }
        
        public void DeletePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            this.DataContext.PurchaseOrders.Attach(purchaseOrder);
            this.DataContext.PurchaseOrders.DeleteOnSubmit(purchaseOrder);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<PurchaseOrderDetail> GetPurchaseOrderDetails()
        {
            return this.DataContext.PurchaseOrderDetails;
        }
        
        public void InsertPurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            this.DataContext.PurchaseOrderDetails.InsertOnSubmit(purchaseOrderDetail);
        }
        
        public void UpdatePurchaseOrderDetail(PurchaseOrderDetail currentPurchaseOrderDetail)
        {
            this.DataContext.PurchaseOrderDetails.Attach(currentPurchaseOrderDetail, this.ChangeSet.GetOriginal(currentPurchaseOrderDetail));
        }
        
        public void DeletePurchaseOrderDetail(PurchaseOrderDetail purchaseOrderDetail)
        {
            this.DataContext.PurchaseOrderDetails.Attach(purchaseOrderDetail);
            this.DataContext.PurchaseOrderDetails.DeleteOnSubmit(purchaseOrderDetail);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesOrderDetail> GetSalesOrderDetails()
        {
            return this.DataContext.SalesOrderDetails;
        }
        
        public void InsertSalesOrderDetail(SalesOrderDetail salesOrderDetail)
        {
            this.DataContext.SalesOrderDetails.InsertOnSubmit(salesOrderDetail);
        }
        
        public void UpdateSalesOrderDetail(SalesOrderDetail currentSalesOrderDetail)
        {
            this.DataContext.SalesOrderDetails.Attach(currentSalesOrderDetail, this.ChangeSet.GetOriginal(currentSalesOrderDetail));
        }
        
        public void DeleteSalesOrderDetail(SalesOrderDetail salesOrderDetail)
        {
            this.DataContext.SalesOrderDetails.Attach(salesOrderDetail);
            this.DataContext.SalesOrderDetails.DeleteOnSubmit(salesOrderDetail);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesOrderHeader> GetSalesOrderHeaders()
        {
            return this.DataContext.SalesOrderHeaders;
        }
        
        public void InsertSalesOrderHeader(SalesOrderHeader salesOrderHeader)
        {
            this.DataContext.SalesOrderHeaders.InsertOnSubmit(salesOrderHeader);
        }
        
        public void UpdateSalesOrderHeader(SalesOrderHeader currentSalesOrderHeader)
        {
            this.DataContext.SalesOrderHeaders.Attach(currentSalesOrderHeader, this.ChangeSet.GetOriginal(currentSalesOrderHeader));
        }
        
        public void DeleteSalesOrderHeader(SalesOrderHeader salesOrderHeader)
        {
            this.DataContext.SalesOrderHeaders.Attach(salesOrderHeader);
            this.DataContext.SalesOrderHeaders.DeleteOnSubmit(salesOrderHeader);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesOrderHeaderSalesReason> GetSalesOrderHeaderSalesReasons()
        {
            return this.DataContext.SalesOrderHeaderSalesReasons;
        }
        
        public void InsertSalesOrderHeaderSalesReason(SalesOrderHeaderSalesReason salesOrderHeaderSalesReason)
        {
            this.DataContext.SalesOrderHeaderSalesReasons.InsertOnSubmit(salesOrderHeaderSalesReason);
        }
        
        public void UpdateSalesOrderHeaderSalesReason(SalesOrderHeaderSalesReason currentSalesOrderHeaderSalesReason)
        {
            this.DataContext.SalesOrderHeaderSalesReasons.Attach(currentSalesOrderHeaderSalesReason, this.ChangeSet.GetOriginal(currentSalesOrderHeaderSalesReason));
        }
        
        public void DeleteSalesOrderHeaderSalesReason(SalesOrderHeaderSalesReason salesOrderHeaderSalesReason)
        {
            this.DataContext.SalesOrderHeaderSalesReasons.Attach(salesOrderHeaderSalesReason);
            this.DataContext.SalesOrderHeaderSalesReasons.DeleteOnSubmit(salesOrderHeaderSalesReason);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesPerson> GetSalesPersons()
        {
            return this.DataContext.SalesPersons;
        }
        
        public void InsertSalesPerson(SalesPerson salesPerson)
        {
            this.DataContext.SalesPersons.InsertOnSubmit(salesPerson);
        }
        
        public void UpdateSalesPerson(SalesPerson currentSalesPerson)
        {
            this.DataContext.SalesPersons.Attach(currentSalesPerson, this.ChangeSet.GetOriginal(currentSalesPerson));
        }
        
        public void DeleteSalesPerson(SalesPerson salesPerson)
        {
            this.DataContext.SalesPersons.Attach(salesPerson);
            this.DataContext.SalesPersons.DeleteOnSubmit(salesPerson);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesPersonQuotaHistory> GetSalesPersonQuotaHistories()
        {
            return this.DataContext.SalesPersonQuotaHistories;
        }
        
        public void InsertSalesPersonQuotaHistory(SalesPersonQuotaHistory salesPersonQuotaHistory)
        {
            this.DataContext.SalesPersonQuotaHistories.InsertOnSubmit(salesPersonQuotaHistory);
        }
        
        public void UpdateSalesPersonQuotaHistory(SalesPersonQuotaHistory currentSalesPersonQuotaHistory)
        {
            this.DataContext.SalesPersonQuotaHistories.Attach(currentSalesPersonQuotaHistory, this.ChangeSet.GetOriginal(currentSalesPersonQuotaHistory));
        }
        
        public void DeleteSalesPersonQuotaHistory(SalesPersonQuotaHistory salesPersonQuotaHistory)
        {
            this.DataContext.SalesPersonQuotaHistories.Attach(salesPersonQuotaHistory);
            this.DataContext.SalesPersonQuotaHistories.DeleteOnSubmit(salesPersonQuotaHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesReason> GetSalesReasons()
        {
            return this.DataContext.SalesReasons;
        }
        
        public void InsertSalesReason(SalesReason salesReason)
        {
            this.DataContext.SalesReasons.InsertOnSubmit(salesReason);
        }
        
        public void UpdateSalesReason(SalesReason currentSalesReason)
        {
            this.DataContext.SalesReasons.Attach(currentSalesReason, this.ChangeSet.GetOriginal(currentSalesReason));
        }
        
        public void DeleteSalesReason(SalesReason salesReason)
        {
            this.DataContext.SalesReasons.Attach(salesReason);
            this.DataContext.SalesReasons.DeleteOnSubmit(salesReason);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesTaxRate> GetSalesTaxRates()
        {
            return this.DataContext.SalesTaxRates;
        }
        
        public void InsertSalesTaxRate(SalesTaxRate salesTaxRate)
        {
            this.DataContext.SalesTaxRates.InsertOnSubmit(salesTaxRate);
        }
        
        public void UpdateSalesTaxRate(SalesTaxRate currentSalesTaxRate)
        {
            this.DataContext.SalesTaxRates.Attach(currentSalesTaxRate, this.ChangeSet.GetOriginal(currentSalesTaxRate));
        }
        
        public void DeleteSalesTaxRate(SalesTaxRate salesTaxRate)
        {
            this.DataContext.SalesTaxRates.Attach(salesTaxRate);
            this.DataContext.SalesTaxRates.DeleteOnSubmit(salesTaxRate);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesTerritory> GetSalesTerritories()
        {
            return this.DataContext.SalesTerritories;
        }
        
        public void InsertSalesTerritory(SalesTerritory salesTerritory)
        {
            this.DataContext.SalesTerritories.InsertOnSubmit(salesTerritory);
        }
        
        public void UpdateSalesTerritory(SalesTerritory currentSalesTerritory)
        {
            this.DataContext.SalesTerritories.Attach(currentSalesTerritory, this.ChangeSet.GetOriginal(currentSalesTerritory));
        }
        
        public void DeleteSalesTerritory(SalesTerritory salesTerritory)
        {
            this.DataContext.SalesTerritories.Attach(salesTerritory);
            this.DataContext.SalesTerritories.DeleteOnSubmit(salesTerritory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SalesTerritoryHistory> GetSalesTerritoryHistories()
        {
            return this.DataContext.SalesTerritoryHistories;
        }
        
        public void InsertSalesTerritoryHistory(SalesTerritoryHistory salesTerritoryHistory)
        {
            this.DataContext.SalesTerritoryHistories.InsertOnSubmit(salesTerritoryHistory);
        }
        
        public void UpdateSalesTerritoryHistory(SalesTerritoryHistory currentSalesTerritoryHistory)
        {
            this.DataContext.SalesTerritoryHistories.Attach(currentSalesTerritoryHistory, this.ChangeSet.GetOriginal(currentSalesTerritoryHistory));
        }
        
        public void DeleteSalesTerritoryHistory(SalesTerritoryHistory salesTerritoryHistory)
        {
            this.DataContext.SalesTerritoryHistories.Attach(salesTerritoryHistory);
            this.DataContext.SalesTerritoryHistories.DeleteOnSubmit(salesTerritoryHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ScrapReason> GetScrapReasons()
        {
            return this.DataContext.ScrapReasons;
        }
        
        public void InsertScrapReason(ScrapReason scrapReason)
        {
            this.DataContext.ScrapReasons.InsertOnSubmit(scrapReason);
        }
        
        public void UpdateScrapReason(ScrapReason currentScrapReason)
        {
            this.DataContext.ScrapReasons.Attach(currentScrapReason, this.ChangeSet.GetOriginal(currentScrapReason));
        }
        
        public void DeleteScrapReason(ScrapReason scrapReason)
        {
            this.DataContext.ScrapReasons.Attach(scrapReason);
            this.DataContext.ScrapReasons.DeleteOnSubmit(scrapReason);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Shift> GetShifts()
        {
            return this.DataContext.Shifts;
        }
        
        public void InsertShift(Shift shift)
        {
            this.DataContext.Shifts.InsertOnSubmit(shift);
        }
        
        public void UpdateShift(Shift currentShift)
        {
            this.DataContext.Shifts.Attach(currentShift, this.ChangeSet.GetOriginal(currentShift));
        }
        
        public void DeleteShift(Shift shift)
        {
            this.DataContext.Shifts.Attach(shift);
            this.DataContext.Shifts.DeleteOnSubmit(shift);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ShipMethod> GetShipMethods()
        {
            return this.DataContext.ShipMethods;
        }
        
        public void InsertShipMethod(ShipMethod shipMethod)
        {
            this.DataContext.ShipMethods.InsertOnSubmit(shipMethod);
        }
        
        public void UpdateShipMethod(ShipMethod currentShipMethod)
        {
            this.DataContext.ShipMethods.Attach(currentShipMethod, this.ChangeSet.GetOriginal(currentShipMethod));
        }
        
        public void DeleteShipMethod(ShipMethod shipMethod)
        {
            this.DataContext.ShipMethods.Attach(shipMethod);
            this.DataContext.ShipMethods.DeleteOnSubmit(shipMethod);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<ShoppingCartItem> GetShoppingCartItems()
        {
            return this.DataContext.ShoppingCartItems;
        }
        
        public void InsertShoppingCartItem(ShoppingCartItem shoppingCartItem)
        {
            this.DataContext.ShoppingCartItems.InsertOnSubmit(shoppingCartItem);
        }
        
        public void UpdateShoppingCartItem(ShoppingCartItem currentShoppingCartItem)
        {
            this.DataContext.ShoppingCartItems.Attach(currentShoppingCartItem, this.ChangeSet.GetOriginal(currentShoppingCartItem));
        }
        
        public void DeleteShoppingCartItem(ShoppingCartItem shoppingCartItem)
        {
            this.DataContext.ShoppingCartItems.Attach(shoppingCartItem);
            this.DataContext.ShoppingCartItems.DeleteOnSubmit(shoppingCartItem);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SpecialOffer> GetSpecialOffers()
        {
            return this.DataContext.SpecialOffers;
        }
        
        public void InsertSpecialOffer(SpecialOffer specialOffer)
        {
            this.DataContext.SpecialOffers.InsertOnSubmit(specialOffer);
        }
        
        public void UpdateSpecialOffer(SpecialOffer currentSpecialOffer)
        {
            this.DataContext.SpecialOffers.Attach(currentSpecialOffer, this.ChangeSet.GetOriginal(currentSpecialOffer));
        }
        
        public void DeleteSpecialOffer(SpecialOffer specialOffer)
        {
            this.DataContext.SpecialOffers.Attach(specialOffer);
            this.DataContext.SpecialOffers.DeleteOnSubmit(specialOffer);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<SpecialOfferProduct> GetSpecialOfferProducts()
        {
            return this.DataContext.SpecialOfferProducts;
        }
        
        public void InsertSpecialOfferProduct(SpecialOfferProduct specialOfferProduct)
        {
            this.DataContext.SpecialOfferProducts.InsertOnSubmit(specialOfferProduct);
        }
        
        public void UpdateSpecialOfferProduct(SpecialOfferProduct currentSpecialOfferProduct)
        {
            this.DataContext.SpecialOfferProducts.Attach(currentSpecialOfferProduct, this.ChangeSet.GetOriginal(currentSpecialOfferProduct));
        }
        
        public void DeleteSpecialOfferProduct(SpecialOfferProduct specialOfferProduct)
        {
            this.DataContext.SpecialOfferProducts.Attach(specialOfferProduct);
            this.DataContext.SpecialOfferProducts.DeleteOnSubmit(specialOfferProduct);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<StateProvince> GetStateProvinces()
        {
            return this.DataContext.StateProvinces;
        }
        
        public void InsertStateProvince(StateProvince stateProvince)
        {
            this.DataContext.StateProvinces.InsertOnSubmit(stateProvince);
        }
        
        public void UpdateStateProvince(StateProvince currentStateProvince)
        {
            this.DataContext.StateProvinces.Attach(currentStateProvince, this.ChangeSet.GetOriginal(currentStateProvince));
        }
        
        public void DeleteStateProvince(StateProvince stateProvince)
        {
            this.DataContext.StateProvinces.Attach(stateProvince);
            this.DataContext.StateProvinces.DeleteOnSubmit(stateProvince);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Store> GetStores()
        {
            return this.DataContext.Stores;
        }
        
        public void InsertStore(Store store)
        {
            this.DataContext.Stores.InsertOnSubmit(store);
        }
        
        public void UpdateStore(Store currentStore)
        {
            this.DataContext.Stores.Attach(currentStore, this.ChangeSet.GetOriginal(currentStore));
        }
        
        public void DeleteStore(Store store)
        {
            this.DataContext.Stores.Attach(store);
            this.DataContext.Stores.DeleteOnSubmit(store);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<StoreContact> GetStoreContacts()
        {
            return this.DataContext.StoreContacts;
        }
        
        public void InsertStoreContact(StoreContact storeContact)
        {
            this.DataContext.StoreContacts.InsertOnSubmit(storeContact);
        }
        
        public void UpdateStoreContact(StoreContact currentStoreContact)
        {
            this.DataContext.StoreContacts.Attach(currentStoreContact, this.ChangeSet.GetOriginal(currentStoreContact));
        }
        
        public void DeleteStoreContact(StoreContact storeContact)
        {
            this.DataContext.StoreContacts.Attach(storeContact);
            this.DataContext.StoreContacts.DeleteOnSubmit(storeContact);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<TransactionHistory> GetTransactionHistories()
        {
            return this.DataContext.TransactionHistories;
        }
        
        public void InsertTransactionHistory(TransactionHistory transactionHistory)
        {
            this.DataContext.TransactionHistories.InsertOnSubmit(transactionHistory);
        }
        
        public void UpdateTransactionHistory(TransactionHistory currentTransactionHistory)
        {
            this.DataContext.TransactionHistories.Attach(currentTransactionHistory, this.ChangeSet.GetOriginal(currentTransactionHistory));
        }
        
        public void DeleteTransactionHistory(TransactionHistory transactionHistory)
        {
            this.DataContext.TransactionHistories.Attach(transactionHistory);
            this.DataContext.TransactionHistories.DeleteOnSubmit(transactionHistory);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<TransactionHistoryArchive> GetTransactionHistoryArchives()
        {
            return this.DataContext.TransactionHistoryArchives;
        }
        
        public void InsertTransactionHistoryArchive(TransactionHistoryArchive transactionHistoryArchive)
        {
            this.DataContext.TransactionHistoryArchives.InsertOnSubmit(transactionHistoryArchive);
        }
        
        public void UpdateTransactionHistoryArchive(TransactionHistoryArchive currentTransactionHistoryArchive)
        {
            this.DataContext.TransactionHistoryArchives.Attach(currentTransactionHistoryArchive, this.ChangeSet.GetOriginal(currentTransactionHistoryArchive));
        }
        
        public void DeleteTransactionHistoryArchive(TransactionHistoryArchive transactionHistoryArchive)
        {
            this.DataContext.TransactionHistoryArchives.Attach(transactionHistoryArchive);
            this.DataContext.TransactionHistoryArchives.DeleteOnSubmit(transactionHistoryArchive);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<UnitMeasure> GetUnitMeasures()
        {
            return this.DataContext.UnitMeasures;
        }
        
        public void InsertUnitMeasure(UnitMeasure unitMeasure)
        {
            this.DataContext.UnitMeasures.InsertOnSubmit(unitMeasure);
        }
        
        public void UpdateUnitMeasure(UnitMeasure currentUnitMeasure)
        {
            this.DataContext.UnitMeasures.Attach(currentUnitMeasure, this.ChangeSet.GetOriginal(currentUnitMeasure));
        }
        
        public void DeleteUnitMeasure(UnitMeasure unitMeasure)
        {
            this.DataContext.UnitMeasures.Attach(unitMeasure);
            this.DataContext.UnitMeasures.DeleteOnSubmit(unitMeasure);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<Vendor> GetVendors()
        {
            return this.DataContext.Vendors;
        }
        
        public void InsertVendor(Vendor vendor)
        {
            this.DataContext.Vendors.InsertOnSubmit(vendor);
        }
        
        public void UpdateVendor(Vendor currentVendor)
        {
            this.DataContext.Vendors.Attach(currentVendor, this.ChangeSet.GetOriginal(currentVendor));
        }
        
        public void DeleteVendor(Vendor vendor)
        {
            this.DataContext.Vendors.Attach(vendor);
            this.DataContext.Vendors.DeleteOnSubmit(vendor);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<VendorAddress> GetVendorAddresses()
        {
            return this.DataContext.VendorAddresses;
        }
        
        public void InsertVendorAddress(VendorAddress vendorAddress)
        {
            this.DataContext.VendorAddresses.InsertOnSubmit(vendorAddress);
        }
        
        public void UpdateVendorAddress(VendorAddress currentVendorAddress)
        {
            this.DataContext.VendorAddresses.Attach(currentVendorAddress, this.ChangeSet.GetOriginal(currentVendorAddress));
        }
        
        public void DeleteVendorAddress(VendorAddress vendorAddress)
        {
            this.DataContext.VendorAddresses.Attach(vendorAddress);
            this.DataContext.VendorAddresses.DeleteOnSubmit(vendorAddress);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<VendorContact> GetVendorContacts()
        {
            return this.DataContext.VendorContacts;
        }
        
        public void InsertVendorContact(VendorContact vendorContact)
        {
            this.DataContext.VendorContacts.InsertOnSubmit(vendorContact);
        }
        
        public void UpdateVendorContact(VendorContact currentVendorContact)
        {
            this.DataContext.VendorContacts.Attach(currentVendorContact, this.ChangeSet.GetOriginal(currentVendorContact));
        }
        
        public void DeleteVendorContact(VendorContact vendorContact)
        {
            this.DataContext.VendorContacts.Attach(vendorContact);
            this.DataContext.VendorContacts.DeleteOnSubmit(vendorContact);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<WorkOrder> GetWorkOrders()
        {
            return this.DataContext.WorkOrders;
        }
        
        public void InsertWorkOrder(WorkOrder workOrder)
        {
            this.DataContext.WorkOrders.InsertOnSubmit(workOrder);
        }
        
        public void UpdateWorkOrder(WorkOrder currentWorkOrder)
        {
            this.DataContext.WorkOrders.Attach(currentWorkOrder, this.ChangeSet.GetOriginal(currentWorkOrder));
        }
        
        public void DeleteWorkOrder(WorkOrder workOrder)
        {
            this.DataContext.WorkOrders.Attach(workOrder);
            this.DataContext.WorkOrders.DeleteOnSubmit(workOrder);
        }
        
        // TODO:
        // Consider constraining the results of your query method.  If you need additional input you can
        // add parameters to this method or create additional query methods with different names.
        public IQueryable<WorkOrderRouting> GetWorkOrderRoutings()
        {
            return this.DataContext.WorkOrderRoutings;
        }
        
        public void InsertWorkOrderRouting(WorkOrderRouting workOrderRouting)
        {
            this.DataContext.WorkOrderRoutings.InsertOnSubmit(workOrderRouting);
        }
        
        public void UpdateWorkOrderRouting(WorkOrderRouting currentWorkOrderRouting)
        {
            this.DataContext.WorkOrderRoutings.Attach(currentWorkOrderRouting, this.ChangeSet.GetOriginal(currentWorkOrderRouting));
        }
        
        public void DeleteWorkOrderRouting(WorkOrderRouting workOrderRouting)
        {
            this.DataContext.WorkOrderRoutings.Attach(workOrderRouting);
            this.DataContext.WorkOrderRoutings.DeleteOnSubmit(workOrderRouting);
        }
    }
}
