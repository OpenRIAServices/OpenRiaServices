#region Namespaces

using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using OpenRiaServices.EntityFramework;
using OpenRiaServices.Hosting.WCF.OData.Test.Models;
using OpenRiaServices.Server;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Linq;
using System.Data.Linq;
using System.Data.Services.Providers;
#endregion

namespace OpenRiaServices.Hosting.WCF.OData.Test
{
    [EnableClientAccess]
    public class NorthwindDomainService : LinqToEntitiesDomainService<NorthwindEntities>
    {
        [Query(IsDefault = true)]
        public IQueryable<Customer> GetCustomers()
        {
            return this.ObjectContext.Customers;
        }

        [Query(IsDefault = true)]
        public IEnumerable<Order> ObtainOrders()
        {
            return this.ObjectContext.Orders;
        }

        [Query]
        public IQueryable<Customer> GetCustomersByCountry(string country)
        {
            return this.ObjectContext.Customers.Where(c => c.Country == country);
        }

        [Query]
        public IEnumerable<Customer> GetCustomersByID(string id)
        {
            return this.ObjectContext.Customers.Where(c => c.CustomerID == id);
        }

        [Query(IsComposable = false)]
        public Order GetOrderByID(int orderId)
        {
            return this.ObjectContext.Orders.SingleOrDefault(o => o.OrderID == orderId);
        }

        [Query]
        public IEnumerable<Order> GetOrdersForCustomerLaterThan(string customerId, DateTime ordersAfter)
        {
            return this.ObjectContext.Orders.Where(o => o.CustomerID == customerId && o.OrderDate > ordersAfter);
        }

        [Query(HasSideEffects = true)]
        public IEnumerable<Order> GetOrdersForCustomer(string customerId)
        {
            return this.ObjectContext.Orders.Where(o => o.CustomerID == customerId);
        }

        [Query(IsComposable = false, HasSideEffects = true)]
        public Order GetOrderByOrderDate(DateTime orderDate)
        {
            return this.ObjectContext.Orders.SingleOrDefault(o => o.OrderDate == orderDate);
        }

        [Invoke]
        public int GetOrderCount()
        {
            ValidationResult error = new ValidationResult("Validation Error: GetOrderCount");
            throw new ValidationException(error, null, null);
        }
    }

    public class NotVisiblePersonBaseWithoutDataContract
    {
        [Key]
        public int ID { get; set; }
        public Guid Visible1 { get; set; }

        [Exclude]
        public string NotVisible1 { get; set; }

        [IgnoreDataMember]
        public string NotVisible2 { get; set; }
        public IEnumerable<char> NotVisible3 { get; set; }
    }

    [DataContract]
    public class NotVisiblePersonBaseWithDataContract : NotVisiblePersonBaseWithoutDataContract
    {
        [DataMember]
        public byte Visible2 { get; set; }

        public string NotVisible4 { get; set; }
        [Exclude]
        public string NotVisible5 { get; set; }
        [IgnoreDataMember]
        public float NotVisible6 { get; set; }
        [DataMember]
        public IEnumerable<bool> NotVisible7 { get; set; }
    }

    [KnownType(typeof(PersonWithBirthday))]
    [KnownType(typeof(Teacher))]
    [KnownType(typeof(SubstituteTeacher))]
    [KnownType(typeof(Student))]
    [KnownType(typeof(PartTimeStudent))]
    [KnownType(typeof(FullTimeStudent))]
    [DataContract]
    public class Person : NotVisiblePersonBaseWithDataContract
    {
        [DataMember(Name = "Name", IsRequired = true)]
        [Include("Length", "NameLength")]
        public string PersonName { get; set; }

        public string NotVisible8 { get; set; }
        [Exclude]
        public string NotVisible9 { get; set; }
        [IgnoreDataMember]
        public float NotVisible10 { get; set; }
        [DataMember]
        public IEnumerable<byte> NotVisible11 { get; set; }

        [DataMember]
        [Association("BestFriend", "ID", "ID")]
        public Person NotVisible16 { get; set; }

        [DataMember]
        [Composition]
        [Association("BestFriend2", "ID", "ID")]
        public Person NotVisible17 { get; set; }

        [DataMember]
        [ExternalReference]
        [Association("BestFriend3", "ID", "ID")]
        public Person NotVisible18 { get; set; }

        [DataMember]
        [Include]
        [Association("BestFriend4", "ID", "ID")]
        public Person NotVisible19 { get; set; }

        [Include("Length", "ProjectedLength")]
        public string NotVisible20 { get; set; }

        [DataMember]
        [Include("PersonName", "BestFriendName")]
        public Person NotVisible21 { get; set; }
    }

    [DataContract]
    public class PersonWithBirthday : Person
    {
        [DataMember]
        public DateTime Birthday { get; set; }
    }

    [DataContract]
    public class NotVisiblePersonTypeWithDataContract : PersonWithBirthday
    {
        [DataMember]
        public byte[] Visible3 { get; set; }
        [DataMember]
        public decimal Visible4 { get; set; }

        public short NotVisible12 { get; set; }
        [Exclude]
        public IntPtr NotVisible13 { get; set; }
        [IgnoreDataMember]
        public float NotVisible14 { get; set; }
        [DataMember]
        public IEnumerable<Guid> NotVisible15 { get; set; }
    }

    [DataContract]
    public class Teacher : NotVisiblePersonTypeWithDataContract
    {
        [DataMember]
        public string Subject { get; set; }
    }

    [DataContract]
    public class SubstituteTeacher : Teacher
    {
    }

    [DataContract]
    public class Student : NotVisiblePersonTypeWithDataContract
    {
        [DataMember]
        public double GPA { get; set; }
    }

    [DataContract]
    public class PartTimeStudent : Student
    {
        [DataMember]
        public string Status { get; set; }
    }

    [DataContract]
    public class FullTimeStudent : Student
    {
        [DataMember]
        public DateTime ClassOf { get; set; }
    }

    [EnableClientAccess]
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    [ServiceContract(Name = "MyPersonnelDomainService", Namespace = "MyNamespace")]
    public class PersonnelDomainService : DomainService
    {
        private static List<Person> persons;

        static PersonnelDomainService()
        {
            persons = new List<Person>
            {
                new Person()
                {
                    ID = 2,
                    PersonName = "Andrew",
                },
                new PersonWithBirthday()
                {
                    ID = 1,
                    PersonName = "John",
                    Birthday = new DateTime(1980, 1, 1),
                },
                new Teacher()
                {
                    ID = 3,
                    PersonName = "Bob",
                    Birthday = new DateTime(1977, 7, 7),
                    Subject = "Math",
                    NotVisible20 = "length11str"
                },
                new SubstituteTeacher()
                {
                    ID = 4,
                    PersonName = "Paul",
                    Birthday = new DateTime(1966, 6, 6),
                    Subject = "History"
                },
                new PartTimeStudent()
                {
                    ID = 5,
                    PersonName = "Jim",
                    Birthday = new DateTime(1977, 7, 7),
                    Status = "Non-Matriculated",
                    GPA = 3.88,
                },
                new FullTimeStudent()
                {
                    ID = 6,
                    PersonName = "Matt",
                    Birthday = new DateTime(1990, 1, 1),
                    GPA = 3.99,
                    ClassOf = new DateTime(2012,1,1)
                },
            };

            persons.Single(p => p.ID == 3).NotVisible21 = persons.Single(p => p.ID == 2);
        }

        [Query(IsDefault = true, IsComposable = true)]
        public IQueryable<Person> GetPersons()
        {
            return persons.AsQueryable<Person>();
        }

        //
        // Person Service Ops
        //

        [Query]
        public IQueryable<Person> GetNPersonsQuery(int count)
        {
            return persons.AsQueryable<Person>().Take(count);
        }

        [Query]
        public IQueryable<Person> GetNPersonsQueryNullable(int? count)
        {
            if (!count.HasValue)
            {
                count = 999;
            }

            return persons.AsQueryable<Person>().Take(count.Value);
        }

        [Invoke]
        public IEnumerable<Person> GetNPersonsInvoke(int count)
        {
            return persons.AsEnumerable<Person>().Take(count);
        }

        [Invoke]
        public IEnumerable<Person> GetNPersonsInvokeNullable(int? count)
        {
            if (!count.HasValue)
            {
                count = 999;
            }

            return persons.AsEnumerable<Person>().Take(count.Value);
        }

        [Query(IsComposable = false)]
        public Person GetPersonByNameQuery(string name)
        {
            return persons.SingleOrDefault(p => p.PersonName == name);
        }

        [Invoke]
        public Person GetPersonByNameInvoke(string name)
        {
            return persons.SingleOrDefault(p => p.PersonName == name);
        }

        [Query(IsComposable = false)]
        public Person GetPersonByIdQuery(int id)
        {
            return persons.Single(p => p.ID == id);
        }

        [Invoke]
        public Person GetPersonByIdInvoke(int id)
        {
            return persons.Single(p => p.ID == id);
        }

        [Invoke]
        public int GetPersonCountInvoke()
        {
            return persons.Count;
        }

        //
        // FullTimeStudent Service Ops
        //

        [Query]
        public IQueryable<FullTimeStudent> GetNFullTimeStudentsQuery(int count)
        {
            return persons.OfType<FullTimeStudent>().AsQueryable<FullTimeStudent>().Take(count);
        }

        [Query]
        public IQueryable<FullTimeStudent> GetNFullTimeStudentsQueryNullable(int? count)
        {
            if (!count.HasValue)
            {
                count = 999;
            }

            return persons.OfType<FullTimeStudent>().AsQueryable<FullTimeStudent>().Take(count.Value);
        }

        [Invoke]
        public IEnumerable<FullTimeStudent> GetNFullTimeStudentsInvoke(int count)
        {
            return persons.OfType<FullTimeStudent>().AsEnumerable<FullTimeStudent>().Take(count);
        }

        [Invoke]
        public IEnumerable<FullTimeStudent> GetNFullTimeStudentsInvokeNullable(int? count)
        {
            if (!count.HasValue)
            {
                count = 999;
            }

            return persons.OfType<FullTimeStudent>().AsEnumerable<FullTimeStudent>().Take(count.Value);
        }

        [Query(IsComposable = false)]
        public FullTimeStudent GetFullTimeStudentByNameQuery(string name)
        {
            return persons.OfType<FullTimeStudent>().SingleOrDefault(p => p.PersonName == name);
        }

        [Invoke]
        public FullTimeStudent GetFullTimeStudentByNameInvoke(string name)
        {
            return persons.OfType<FullTimeStudent>().SingleOrDefault(p => p.PersonName == name);
        }

        [Query(IsComposable = false)]
        public FullTimeStudent GetFullTimeStudentByIdQuery(int id)
        {
            return persons.OfType<FullTimeStudent>().Single(p => p.ID == id);
        }

        [Invoke]
        public FullTimeStudent GetFullTimeStudentByIdInvoke(int id)
        {
            return persons.OfType<FullTimeStudent>().Single(p => p.ID == id);
        }

        [Invoke]
        public int GetFullTimeStudentCountInvoke()
        {
            return persons.Count;
        }

        //
        // Operations without attributes
        //

        public IQueryable<Person> GetPersonsQueryableNoAttr()
        {
            return persons.AsQueryable();
        }

        public IEnumerable<Person> GetPersonsEnumerableNoAttr()
        {
            return persons.AsEnumerable();
        }

        public IQueryable<Student> GetStudentsQueryableNoAttr()
        {
            return persons.OfType<Student>().AsQueryable();
        }

        public IEnumerable<Student> GetStudentsEnumerableNoAttr()
        {
            return persons.OfType<Student>().AsEnumerable();
        }

        public Person GetPersonNoAttr()
        {
            return persons.FirstOrDefault();
        }

        public Student GetStudentNoAttr()
        {
            return persons.OfType<Student>().FirstOrDefault();
        }

        public int GetPersonCountNoAttr()
        {
            return persons.Count;
        }

        [OpenRiaServices.Server.Ignore]
        public int IgoredServiceOp()
        {
            return persons.Count;
        }

        public IQueryable<Person> ServiceOpWithUnsupportedParam1(TimeSpan ts)
        {
            throw new NotSupportedException("Should not be exposed by OData endpoint since TimeSpan is not a supported primitive type by Astoria.");
        }

        public IQueryable<Person> ServiceOpWithUnsupportedParam2(TimeSpan? ts)
        {
            throw new NotSupportedException("Should not be exposed by OData endpoint since TimeSpan is not a supported primitive type by Astoria.");
        }

        [Insert]
        public void InsertPerson(Person person)
        {
            persons.Add(person);
        }

        [Update]
        public void UpdatePerson(Person person)
        {
            Person current = persons.FirstOrDefault(p => p.PersonName == person.PersonName);
            if (null != current)
            {
                foreach (PropertyInfo p in person.GetType().GetProperties().Where(p => p.CanWrite))
                {
                    p.SetValue(current, p.GetValue(person, null), null);
                }
            }
        }

        [Delete]
        public void DeletePerson(Person person)
        {
            persons.Remove(person);
        }
    }

    public class NoClientAccessPersonnelDomainService : DomainService
    {
        [Query(IsDefault = true)]
        public IQueryable<Person> GetPersons()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }

        [Invoke]
        public IEnumerable<Person> GetPersonsInvoke()
        {
            return (Array.Empty<Person>()).AsEnumerable();
        }

        [Query]
        public IQueryable<Person> GetPersonsQuery()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }
    }

    [EnableClientAccess]
    public class NoODataOperaionDomainService : DomainService
    {
        [Query(IsDefault = false)]
        public IQueryable<Person> GetPersons()
        {
            throw new InvalidOperationException("Person is not exposed by a set.");
        }

        [Query(IsComposable = false)]
        public Person GetPersonByTimeSpan(TimeSpan? ts)
        {
            throw new NotSupportedException("TimeSpan? is not an OData supported type.");
        }

        [Invoke]
        public TimeSpan? EchoTimeSpan(TimeSpan? ts)
        {
            throw new NotSupportedException("TimeSpan? is not an OData supported type.");
        }
    }

    [EnableClientAccess]
    [RequiresAuthentication()]
    public class FullAuthenticatedPersonnelDomainService : DomainService
    {
        [Query(IsDefault = true)]
        public IQueryable<Person> GetPersons()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }

        [Invoke]
        public IEnumerable<Person> GetPersonsInvoke()
        {
            return (Array.Empty<Person>()).AsEnumerable();
        }

        [Query]
        public IQueryable<Person> GetPersonsQuery()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }
    }

    [EnableClientAccess]
    public class PartialAuthenticatedPersonnelDomainService : DomainService
    {
        [Query(IsDefault = true)]
        [RequiresAuthentication()]
        public IQueryable<Person> GetPersons()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }

        [Invoke]
        [RequiresAuthentication()]
        public IEnumerable<Person> GetPersonsInvokeAuth()
        {
            return (Array.Empty<Person>()).AsEnumerable();
        }

        [Query]
        [RequiresAuthentication()]
        public IQueryable<Person> GetPersonsQueryAuth()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }

        [Invoke]
        [RequiresRole("Admin")]
        public IEnumerable<Person> GetPersonsInvokeRole()
        {
            return (Array.Empty<Person>()).AsEnumerable();
        }

        [Query]
        [RequiresRole("Admin")]
        public IQueryable<Person> GetPersonsQueryRole()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }

        [Invoke]
        public IEnumerable<Person> GetPersonsInvoke()
        {
            return (Array.Empty<Person>()).AsEnumerable();
        }

        [Query]
        public IQueryable<Person> GetPersonsQuery()
        {
            return (Array.Empty<Person>()).AsQueryable();
        }
    }

    public class NotVisibleNoneEntityBase
    {
        [Key]
        public int ID { get; set; }
    }

    [KnownType(typeof(VisibleEntityType))]
    public class VisibleEntityBase : NotVisibleNoneEntityBase
    {
    }

    public class VisibleEntityType : VisibleEntityBase
    {
    }

    [EnableClientAccess]
    public class DummyDomainService : DomainService
    {
        [Query(IsDefault = true)]
        public IQueryable<VisibleEntityType> GetVisibleEntitiesQuery()
        {
            return (Array.Empty<VisibleEntityType>()).AsQueryable();
        }

        [Invoke]
        public IEnumerable<VisibleEntityType> GetVisibleEntitiesInvoke()
        {
            return (Array.Empty<VisibleEntityType>()).AsQueryable();
        }

        [Query]
        public IQueryable<VisibleEntityBase> GetNonVisibleEntitiesQuery()
        {
            return (Array.Empty<VisibleEntityBase>()).AsQueryable();
        }

        [Invoke]
        public IEnumerable<VisibleEntityBase> GetNonVisibleEntitiesInvoke()
        {
            return (Array.Empty<VisibleEntityBase>()).AsQueryable();
        }

        [Insert]
        public void InsertVisibleEntity(VisibleEntityType e)
        {
            throw new NotSupportedException();
        }

        [Update]
        public void UpdateVisibleEntity(VisibleEntityType e)
        {
            throw new NotSupportedException();
        }

        [Delete]
        public void DeleteVisibleEntity(VisibleEntityType e)
        {
            throw new NotSupportedException();
        }

        [Insert]
        public void InsertNotVisibleEntity(VisibleEntityBase e)
        {
            throw new NotSupportedException();
        }

        [Update]
        public void UpdateNotVisibleEntity(VisibleEntityBase e)
        {
            throw new NotSupportedException();
        }

        [Delete]
        public void DeleteNotVisibleEntity(VisibleEntityBase e)
        {
            throw new NotSupportedException();
        }
    }

    [EnableClientAccess]
    public class EchoDomainService : DomainService
    {
        [Invoke]
        public string EchoString(string i) { return i; }
        [Invoke]
        public Boolean EchoBoolean(Boolean i) { return i; }
        [Invoke]
        public Boolean? EchoNullableBoolean(Boolean? i) { return i; }
        [Invoke]
        public Byte EchoByte(Byte i) { return i; }
        [Invoke]
        public Byte? EchoNullableByte(Byte? i) { return i; }
        [Invoke]
        public DateTime EchoDateTime(DateTime i) { return i; }
        [Invoke]
        public DateTime? EchoNullableDateTime(DateTime? i) { return i; }
        [Invoke]
        public Decimal EchoDecimal(Decimal i) { return i; }
        [Invoke]
        public Decimal? EchoNullableDecimal(Decimal? i) { return i; }
        [Invoke]
        public Double EchoDouble(Double i) { return i; }
        [Invoke]
        public Double? EchoNullableDouble(Double? i) { return i; }
        [Invoke]
        public Guid EchoGuid(Guid i) { return i; }
        [Invoke]
        public Guid? EchoNullableGuid(Guid? i) { return i; }
        [Invoke]
        public Int16 EchoInt16(Int16 i) { return i; }
        [Invoke]
        public Int16? EchoNullableInt16(Int16? i) { return i; }
        [Invoke]
        public Int32 EchoInt32(Int32 i) { return i; }
        [Invoke]
        public Int32? EchoNullableInt32(Int32? i) { return i; }
        [Invoke]
        public Int64 EchoInt64(Int64 i) { return i; }
        [Invoke]
        public Int64? EchoNullableInt64(Int64? i) { return i; }
        [Invoke]
        public SByte EchoSByte(SByte i) { return i; }
        [Invoke]
        public SByte? EchoNullableSByte(SByte? i) { return i; }
        [Invoke]
        public Single EchoSingle(Single i) { return i; }
        [Invoke]
        public Single? EchoNullableSingle(Single? i) { return i; }
        [Invoke]
        public byte[] EchoByteArray(byte[] i) { return i; }
        [Invoke]
        public XElement EchoXElement(XElement i) { return i; }
        [Invoke]
        public Binary EchoBinary(Binary i) { return i; }
    }

    [EnableClientAccess]
    public class NotImplementedDomainService : LinqToEntitiesDomainService<NorthwindEntities>
    {
        [Query(IsDefault = true)]
        public IQueryable<Customer> GetCustomers()
        {
            throw new NotImplementedException();
        }

        [Query(IsComposable = false)]
        public Customer GetCustomerByName(string name)
        {
            throw new NotImplementedException();
        }

        [Invoke]
        public int GetCustomerCount()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class ThrowInConstructorDomainService : LinqToEntitiesDomainService<NorthwindEntities>
    {
        public ThrowInConstructorDomainService()
        {
            throw new InvalidOperationException("Bad Service -- throw in domain service constructor!");
        }

        [Query(IsDefault = true)]
        public IQueryable<Customer> GetCustomers()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess(RequiresSecureEndpoint = true)]
    public class RequireSecureEndpointDomainService : LinqToEntitiesDomainService<NorthwindEntities>
    {
        [Query(IsDefault = true)]
        public IQueryable<Customer> GetCustomers()
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class DomainDataServiceUnitTestsAPITest
    {
        [TestMethod]
        public void DomainDataServiceProviderTest()
        {
            // Hit code paths in DomainDataServiceProvider which we would not be able to hit during runtime.
            DomainServiceDescription ds = DomainServiceDescription.GetDescription(typeof(PersonnelDomainService));
            DomainDataServiceMetadata metadata = new DomainDataServiceMetadata(ds);
            DomainDataServiceProvider provider = new DomainDataServiceProvider(metadata, new object());

            // GetResourceAssociationSet()
            Assert.IsNull(provider.GetResourceAssociationSet(null, null, null));

            // TryGetResourceType()
            ResourceType resourceType;
            Assert.IsTrue(provider.TryResolveResourceType(typeof(FullTimeStudent).FullName, out resourceType));
            Assert.AreEqual(resourceType.Name, typeof(FullTimeStudent).Name);
            Assert.IsFalse(provider.TryResolveResourceType("foo", out resourceType));
            Assert.IsNull(resourceType);

            // IsNullPropagationRequired
            Assert.IsTrue(provider.IsNullPropagationRequired);

            // GetOpenPropertyValue
            bool notImplementedExceptionReceived = false;
            try
            {
                provider.GetOpenPropertyValue(null, null);
                Assert.Fail("Expect exception but received none.");
            }
            catch (NotImplementedException)
            {
                notImplementedExceptionReceived = true;
            }

            Assert.IsTrue(notImplementedExceptionReceived, "NotImplementedException expected but received none.");

            // GetOpenPropertyValues
            notImplementedExceptionReceived = false;
            try
            {
                provider.GetOpenPropertyValues(null);
                Assert.Fail("Expect exception but received none.");
            }
            catch (NotImplementedException)
            {
                notImplementedExceptionReceived = true;
            }

            Assert.IsTrue(notImplementedExceptionReceived, "NotImplementedException expected but received none.");
        }

        [TestMethod]
        public void DomainDataServiceErrorHandlerTest()
        {
            MethodInfo method = typeof(DomainDataServiceErrorHandler).GetNestedType("ErrorSerializer", BindingFlags.NonPublic).GetMethod("ExtractErrorValues", BindingFlags.NonPublic | BindingFlags.Static);

            DomainDataServiceException dse = new DomainDataServiceException("Error message here...");
            Assert.AreEqual("Error message here...", dse.Message);

            dse = new DomainDataServiceException();
            var args = new object[] { dse, null, null, null };
            Assert.AreEqual(dse, method.Invoke(null, args));
            Assert.AreEqual(string.Empty, args[1]);
            Assert.AreEqual("An error occurred while processing request for domain data services.", args[2]);
            Assert.AreEqual(System.Globalization.CultureInfo.CurrentCulture.Name, args[3]);

            args = new object[] { null, null, null, null };
            Assert.AreEqual(null, method.Invoke(null, args));
            Assert.AreEqual(string.Empty, args[1]);
            Assert.AreEqual("An error occurred while processing request for domain data services.", args[2]);
            Assert.AreEqual(System.Globalization.CultureInfo.CurrentCulture.Name, args[3]);

            Type xmlWriterStreamType = typeof(DomainDataServiceErrorHandler).GetNestedType("DelegateBodyWriter", BindingFlags.NonPublic).GetNestedType("XmlWriterStream", BindingFlags.NonPublic);
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(new MemoryStream());
            object xmlWriterStream = xmlWriterStreamType.InvokeMember("XmlWriterStream", BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Instance, null, null, new object[] { writer });
            Assert.IsFalse((bool)xmlWriterStreamType.InvokeMember("CanRead", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, xmlWriterStream, Array.Empty<object>()));
            Assert.IsTrue((bool)xmlWriterStreamType.InvokeMember("CanWrite", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, xmlWriterStream, Array.Empty<object>()));
            var testCases = new Action[]
            {
                () => xmlWriterStreamType.InvokeMember("Length", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, xmlWriterStream, null),
                () => xmlWriterStreamType.InvokeMember("Position", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, xmlWriterStream, null),
                () => xmlWriterStreamType.InvokeMember("Position", BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance, null, xmlWriterStream, new object[] {1}),
                () => xmlWriterStreamType.InvokeMember("Read", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, xmlWriterStream, new object[] {null, 0, 0}),
                () => xmlWriterStreamType.InvokeMember("Seek", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, xmlWriterStream, new object[] {0, SeekOrigin.Current}),
                () => xmlWriterStreamType.InvokeMember("SetLength", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, xmlWriterStream, new object[] {0}),
            };
            foreach (var test in testCases)
            {
                try
                {
                    test();
                    Assert.Fail("Expect NotSupportedException, received none.");
                }
                catch (TargetInvocationException e)
                {
                    Assert.IsInstanceOfType(e.InnerException, typeof(NotSupportedException));
                }
            }
        }

        [TestMethod]
        public void HttpProcessUtilsTest()
        {
            var testCases = new[]
            {
                new { Accept=" Application", ExceptionMsg="Media type is unspecified." },
                new { Accept=" Application ", ExceptionMsg="Media type requires a '/' character." },
                new { Accept=" Application/ ", ExceptionMsg="Media type requires a subtype definition." },
                new { Accept=" Application/Json q=1", ExceptionMsg="Media type requires a ';' character before a parameter definition." },
                new { Accept=" Application/Json ; ", ExceptionMsg=String.Empty },
                new { Accept=" Application/Json ; paramWithNoValue ", ExceptionMsg="Media type is missing a parameter value." },
                new { Accept=" Application/Json ; badParam=badValue\" ", ExceptionMsg="Value for MIME type parameter 'badParam' is incorrect because it contained escape characters even though it was not quoted." },
                new { Accept=" Application/Json ; badParam=\"badValue\\", ExceptionMsg="Value for MIME type parameter 'badParam' is incorrect because it terminated with escape character. Escape characters must always be followed by a character in a parameter value." },
                new { Accept=" Application/Json ; badParam=\"badValue ", ExceptionMsg="Value for MIME type parameter 'badParam' is incorrect because the closing quote character could not be found while the parameter value started with a quote character." },
                new { Accept=" Application/Json ; param=\"value\" ", ExceptionMsg=String.Empty },
                new { Accept=" Application/Json ; param=\"value\\\"\" ", ExceptionMsg=String.Empty },
                new { Accept=" Application/Json ; q=1 ", ExceptionMsg=String.Empty },
                new { Accept="text/x-dvi; q=.8; mxb=100000; mxt=5.0, text/x-c", ExceptionMsg="Unsupported media type requested." },
                new { Accept="text/x-dvi; q=.8; mxb=100000; mxt=5.0, text/x-c, AppliCAtION/jSON; q=0.98765", ExceptionMsg=String.Empty },
                new { Accept=" AppliCAtION/jSON; q=1.98765", ExceptionMsg="Malformed value in request header." },
                new { Accept=" AppliCAtION/jSON; q=2", ExceptionMsg="Malformed value in request header." },
                new { Accept=" AppliCAtION/jSON; q=0.1ab", ExceptionMsg="Malformed value in request header." },
                new { Accept="*/*", ExceptionMsg=String.Empty },
                new { Accept="application/*; q=0.2 ; param=value ", ExceptionMsg=String.Empty },
                new { Accept="application/Xml; q=0.2", ExceptionMsg=String.Empty },
            };

            foreach (var test in testCases)
            {
                System.Diagnostics.Trace.WriteLine(test.Accept);

                bool result;
                try
                {
                    result = HttpProcessUtils.IsJsonRequest(RequestKind.ServiceOperation, test.Accept);
                    Assert.IsTrue(string.IsNullOrEmpty(test.ExceptionMsg));

                    if (test.Accept.ToLower().Contains("application/json"))
                    {
                        Assert.IsTrue(result);
                    }
                    else
                    {
                        Assert.IsFalse(result);
                    }
                }
                catch (Exception e)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(test.ExceptionMsg));
                    Assert.AreEqual(test.ExceptionMsg, e.Message);
                }
            }
        }

        [TestMethod]
        public void UriUtilsTest()
        {
            try
            {
                UriUtils.EnumerateSegments(new Uri("http://host/service1.svc/OData", UriKind.Absolute), new Uri("http://host/service.svc/", UriKind.Absolute));
                Assert.Fail("Expect exception but received none.");
            }
            catch (DomainDataServiceException e)
            {
                Assert.AreEqual("Request URI does not have the correct base URI.", e.Message);
            }

            Assert.AreEqual("http://somebase/service.svc", UriUtils.CombineUriStrings(null, "http://somebase/service.svc"));
            Assert.AreEqual("http://somebase/service.svc", UriUtils.CombineUriStrings("http://somebase/", "/service.svc"));
            Assert.AreEqual("http://somebase/service.svc", UriUtils.CombineUriStrings("http://somebase/", "service.svc"));
            Assert.AreEqual("http://somebase/service.svc", UriUtils.CombineUriStrings("http://somebase", "/service.svc"));
            Assert.AreEqual("http://somebase/service.svc", UriUtils.CombineUriStrings("http://somebase", "service.svc"));
        }
    }

    [TestClass]
    public class DomainDataServiceUnitTestsPositive
    {
        [TestMethod]
        public void NorthwindServiceDocument()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath;
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();
                String[] xpath = { @"app:service/app:workspace/app:collection[@href='CustomerSet']/atom:title" };

                for (int i = 0; i < xpath.Length; i++)
                {
                    var xpathExpression = xpath[i];
                    XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                    if (result.Count == 0)
                    {
                        TestUtil.TraceXml(document);
                        throw new InvalidOperationException("Selection of [" + xpath + "] failed to return one or more nodes in last traced XML.");
                    }

                    XmlNode currentResult = result[0];

                    Assert.AreEqual("CustomerSet", currentResult.FirstChild.Value);
                }
            }
        }

        [TestMethod]
        public void PersonnelServiceDocument()
        {
            LocalWebServerHelper.WebConfigTrustLevelFragment = null;
#if !MEDIUM_TRUST
            foreach (string trustLevel in new[] { "  <trust level=\"Full\"/>\r\n", string.Empty })
#else
            foreach (string trustLevel in new[] { "  <trust level=\"Medium\"/>\r\n", string.Empty })
#endif
            {
                using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "WebConfigTrustLevelFragment"))
                {
                    LocalWebServerHelper.WebConfigTrustLevelFragment = trustLevel;
                    using (TestWebRequest req = TestWebRequest.CreateForLocal())
                    {
                        req.ServiceType = typeof(PersonnelDomainService);
                        req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath;
                        req.SendRequest();
                        XmlDocument document = req.GetResponseStreamAsXmlDocument();
                        String[] xpath =
                        {
                            "boolean(/app:service/app:workspace/app:collection[@href='PersonSet']/atom:title='PersonSet')",
                            "boolean(count(/app:service/app:workspace/app:collection/atom:title)=1)"
                        };

                        TestUtil.VerifyXPathExpressionResults(document, true, xpath);
                    }
                }
            }
        }

        [TestMethod]
        public void NorthwindMetadataDocument()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "$metadata";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                TestUtil.AssertSelectNodes(document, "/edmx:Edmx/edmx:DataServices[@adsm:DataServiceVersion='1.0']");

                String[] xpath = { @"edmx:Edmx/edmx:DataServices/csdl1:Schema[@Namespace='OpenRiaServices.Hosting.WCF.OData.Test.Models']/csdl1:EntityType[@Name='Customer']/csdl1:Property[@Name='Address']",
                                   @"edmx:Edmx/edmx:DataServices/csdl1:Schema[@Namespace='OpenRiaServices.Hosting.WCF.OData.Test']/csdl1:EntityContainer[@Name='NorthwindDomainService']/csdl1:EntitySet[@Name='CustomerSet']",
                                   @"edmx:Edmx/edmx:DataServices/csdl1:Schema[@Namespace='OpenRiaServices.Hosting.WCF.OData.Test']/csdl1:EntityContainer[@Name='NorthwindDomainService']/csdl1:FunctionImport[@Name='GetCustomersByCountry']"
                                 };

                for (int i = 0; i < xpath.Length; i++)
                {
                    var xpathExpression = xpath[i];
                    XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                    if (result.Count == 0)
                    {
                        TestUtil.TraceXml(document);
                        throw new InvalidOperationException("Selection of [" + xpath + "] failed to return one or more nodes in last traced XML.");
                    }

                    XmlNode currentResult = result[0];

                    if (i == 0)
                    {
                        Assert.AreEqual("Edm.String", currentResult.Attributes["Type"].Value);
                    }
                    else
                        if (i == 1)
                    {
                        Assert.AreEqual("OpenRiaServices.Hosting.WCF.OData.Test.Models.Customer", currentResult.Attributes["EntityType"].Value);
                    }
                    else
                    {
                        Assert.AreEqual("CustomerSet", currentResult.Attributes["EntitySet"].Value);
                        Assert.AreEqual("GET", currentResult.Attributes["m:HttpMethod"].Value);
                    }
                }
            }
        }

        [TestMethod]
        public void PersonnelMetadataDocument()
        {
            LocalWebServerHelper.WebConfigTrustLevelFragment = null;
#if !MEDIUM_TRUST
            foreach (string trustLevel in new[] { "  <trust level=\"Full\"/>\r\n", string.Empty })
#else
            foreach (string trustLevel in new[] { "  <trust level=\"Medium\"/>\r\n", string.Empty })
#endif
            {
                using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "WebConfigTrustLevelFragment"))
                {
                    LocalWebServerHelper.WebConfigTrustLevelFragment = trustLevel;
                    using (TestWebRequest req = TestWebRequest.CreateForLocal())
                    {
                        req.ServiceType = typeof(PersonnelDomainService);
                        req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "$metadata";
                        req.SendRequest();
                        XmlDocument document = req.GetResponseStreamAsXmlDocument();

                        System.Diagnostics.Trace.WriteLine(document.InnerXml);

                        TestUtil.AssertSelectNodes(document, "/edmx:Edmx/edmx:DataServices[@adsm:DataServiceVersion='1.0']");

                        String[] xpath =
                        {
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType)=7)",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='NotVisiblePersonBaseWithoutDataContract'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='NotVisiblePersonBaseWithDataContract'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person' and not(@BaseType)])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property)=7)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Key/csdl1:PropertyRef[@Name='ID'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='ID' and @Type='Edm.Int32'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='Name' and @Type='Edm.String'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='Visible1' and @Type='Edm.Guid'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='Visible2' and @Type='Edm.Byte'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='ProjectedLength' and @Type='Edm.Int32'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='NameLength' and @Type='Edm.Int32'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Person']/csdl1:Property[@Name='BestFriendName' and @Type='Edm.String'])",

                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='NotVisiblePersonTypeWithDataContract'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='PersonWithBirthday' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.Person'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='PersonWithBirthday']/csdl1:Property)=1)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='PersonWithBirthday']/csdl1:Property[@Name='Birthday' and @Type='Edm.DateTime'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Teacher' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.PersonWithBirthday'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Teacher']/csdl1:Property)=3)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Teacher']/csdl1:Property[@Name='Subject' and @Type='Edm.String'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Teacher']/csdl1:Property[@Name='Visible3' and @Type='Edm.Binary'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Teacher']/csdl1:Property[@Name='Visible4' and @Type='Edm.Decimal'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='SubstituteTeacher' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.Teacher'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='SubstituteTeacher']/csdl1:Property)=0)",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Student' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.PersonWithBirthday'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Student']/csdl1:Property)=3)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Student']/csdl1:Property[@Name='GPA' and @Type='Edm.Double'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Student']/csdl1:Property[@Name='Visible3' and @Type='Edm.Binary'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='Student']/csdl1:Property[@Name='Visible4' and @Type='Edm.Decimal'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='PartTimeStudent' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.Student'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='PartTimeStudent']/csdl1:Property)=1)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='PartTimeStudent']/csdl1:Property[@Name='Status' and @Type='Edm.String'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='FullTimeStudent' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.Student'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='FullTimeStudent']/csdl1:Property)=1)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='FullTimeStudent']/csdl1:Property[@Name='ClassOf' and @Type='Edm.DateTime'])",

                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible1'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible2'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible3'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible4'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible5'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible6'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible7'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible8'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible9'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible10'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible11'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible12'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible13'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible14'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible15'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible16'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible17'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible18'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible19'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible20'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='NotVisible21'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='BestFriend'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='BestFriend2'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='BestFriend3'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType/csdl1:Property[@Name='BestFriend4'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer[@Name='PersonnelDomainService'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet[@Name='PersonSet'])",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet)=1)",
                            "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport)=25)",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNPersonsQuery' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNPersonsInvoke' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNPersonsQueryNullable' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNPersonsInvokeNullable' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonByNameQuery' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonByNameInvoke' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonByIdQuery' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonByIdInvoke' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonCountInvoke' and not(@EntitySet) and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNFullTimeStudentsQuery' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNFullTimeStudentsInvoke' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNFullTimeStudentsQueryNullable' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetNFullTimeStudentsInvokeNullable' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetFullTimeStudentByNameQuery' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetFullTimeStudentByNameInvoke' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetFullTimeStudentByIdQuery' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetFullTimeStudentByIdInvoke' and @EntitySet='PersonSet' and @adsm:HttpMethod='POST'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetFullTimeStudentCountInvoke' and not(@EntitySet) and @adsm:HttpMethod='POST'])",

                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonsQueryableNoAttr' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonsEnumerableNoAttr' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetStudentsQueryableNoAttr' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetStudentsEnumerableNoAttr' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonNoAttr' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetStudentNoAttr' and @EntitySet='PersonSet' and @adsm:HttpMethod='GET'])",
                            "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetPersonCountNoAttr' and not(@EntitySet) and @adsm:HttpMethod='POST'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='IgoredServiceOp'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='ServiceOpWithUnsupportedParam1'])",
                            "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='ServiceOpWithUnsupportedParam2'])",
                        };

                        TestUtil.VerifyXPathExpressionResults(document, true, xpath);
                    }
                }
            }
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore] // Test times out, the reson should be identified so it can be resolved
        [TestCategory("Failing")]
        public void PersonnelServiceOperations()
        {
            var testCases = new[]
            {
                new { Url = "GetPersons()", StatusCode = 404 },
                new { Url = "GetNPersonsQuery()?count=2", StatusCode = 200 },
                new { Url = "GetNPersonsQuery()?count=", StatusCode = 400 },
                new { Url = "GetNPersonsQuery()", StatusCode = 400 },
                new { Url = "GetNPersonsInvoke()?count=2", StatusCode = 200 },
                new { Url = "GetNPersonsInvoke()?count=", StatusCode = 400 },
                new { Url = "GetNPersonsInvoke()", StatusCode = 400 },
                new { Url = "GetNPersonsQueryNullable()?count=2", StatusCode = 200 },
                new { Url = "GetNPersonsQueryNullable()?count=", StatusCode = 200 },
                new { Url = "GetNPersonsQueryNullable()", StatusCode = 200 },
                new { Url = "GetNPersonsInvokeNullable()?count=2", StatusCode = 200 },
                new { Url = "GetNPersonsInvokeNullable()?count=", StatusCode = 200 },
                new { Url = "GetNPersonsInvokeNullable()", StatusCode = 200 },
                new { Url = "GetPersonByNameQuery()?name='Matt'", StatusCode = 200 },
                new { Url = "GetPersonByNameQuery()?name=", StatusCode = 404 },
                new { Url = "GetPersonByNameQuery()", StatusCode = 404 },
                new { Url = "GetPersonByNameInvoke()?name='Matt'", StatusCode = 200 },
                new { Url = "GetPersonByNameInvoke()?name=", StatusCode = 404 },
                new { Url = "GetPersonByNameInvoke()", StatusCode = 404 },

                new { Url = "GetNFullTimeStudentsQuery()?count=2", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsQuery()?count=", StatusCode = 400 },
                new { Url = "GetNFullTimeStudentsQuery()", StatusCode = 400 },
                new { Url = "GetNFullTimeStudentsInvoke()?count=2", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsInvoke()?count=", StatusCode = 400 },
                new { Url = "GetNFullTimeStudentsInvoke()", StatusCode = 400 },
                new { Url = "GetNFullTimeStudentsQueryNullable()?count=2", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsQueryNullable()?count=", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsQueryNullable()", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsInvokeNullable()?count=2", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsInvokeNullable()?count=", StatusCode = 200 },
                new { Url = "GetNFullTimeStudentsInvokeNullable()", StatusCode = 200 },
                new { Url = "GetFullTimeStudentByNameQuery()?name='Matt'", StatusCode = 200 },
                new { Url = "GetFullTimeStudentByNameQuery()?name=", StatusCode = 404 },
                new { Url = "GetFullTimeStudentByNameQuery()", StatusCode = 404 },
                new { Url = "GetFullTimeStudentByNameInvoke()?name='Matt'", StatusCode = 200 },
                new { Url = "GetFullTimeStudentByNameInvoke()?name=", StatusCode = 404 },
                new { Url = "GetFullTimeStudentByNameInvoke()", StatusCode = 404 },

                new { Url = "GetPersonCountInvoke()", StatusCode = 200 },
                new { Url = "GetFullTimeStudentCountInvoke()", StatusCode = 200 },

                new { Url = "GetNPersonBaseQuery()", StatusCode = 404 },
                new { Url = "GetPersonBaseByIdQuery()", StatusCode = 404 },
                new { Url = "GetNPersonBaseInvoke()", StatusCode = 404 },
                new { Url = "GetPersonBaseByIdInvoke()", StatusCode = 404 },
            };

            LocalWebServerHelper.WebConfigTrustLevelFragment = null;
#if !MEDIUM_TRUST
            foreach (string trustLevel in new[] { "  <trust level=\"Full\"/>\r\n", string.Empty })
#else
            foreach (string trustLevel in new[] { "  <trust level=\"Medium\"/>\r\n", string.Empty })
#endif
            {
                using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "WebConfigTrustLevelFragment"))
                {
                    LocalWebServerHelper.WebConfigTrustLevelFragment = trustLevel;
                    using (TestWebRequest req = TestWebRequest.CreateForLocal())
                    {
                        req.ServiceType = typeof(PersonnelDomainService);
                        foreach (var testCase in testCases)
                        {
                            System.Diagnostics.Trace.TraceInformation("Test Case [Url = {0}, StatusCode = {1}]", testCase.Url, testCase.StatusCode);

                            string methodName = testCase.Url;
                            if (testCase.Url.IndexOf('(') > 0)
                            {
                                methodName = testCase.Url.Substring(0, testCase.Url.IndexOf('('));
                            }

                            req.HttpMethod = testCase.Url.Contains("Invoke") ? "POST" : "GET";
                            req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + testCase.Url;
                            Exception ex = TestUtil.RunCatching(req.SendRequest);

                            if (testCase.StatusCode != 200)
                            {
                                Assert.IsNotNull(ex, "Expecting an exception but received none.");
                                Assert.AreEqual(testCase.StatusCode, req.ResponseStatusCode);

                                string responseXml = (new StreamReader(((WebException)ex).Response.GetResponseStream())).ReadToEnd();
                                string expectedMsg = null;
                                switch (req.ResponseStatusCode)
                                {
                                    case 400:
                                        expectedMsg = "Syntax error while processing request parameter.";
                                        break;

                                    case 404:
                                        if (!testCase.Url.Contains("ByName"))
                                        {
                                            expectedMsg = "The DomainService method corresponding to the given request could not be found.";
                                        }
                                        else
                                        {
                                            expectedMsg = string.Format("Resource not found for the segment '{0}'.", methodName);
                                        }
                                        break;

                                    default:
                                        Assert.Fail("Unexpected status code: " + req.ResponseStatusCode.ToString());
                                        break;
                                }

                                Assert.IsFalse(string.IsNullOrEmpty(expectedMsg));
                                Assert.IsTrue(responseXml.Contains(expectedMsg));
                            }
                            else
                            {
                                Assert.IsNull(ex, "Not expecting an exception but received one.");
                                List<string> xpaths = new List<string>();

                                if (testCase.Url.Contains("NPersons") || testCase.Url.Contains("NFullTimeStudents"))
                                {
                                    xpaths.Add("boolean(/atom:feed/atom:entry)");
                                }
                                else if (!testCase.Url.Contains("Count"))
                                {
                                    xpaths.Add("boolean(/atom:entry)");
                                }
                                else
                                {
                                    xpaths.Add(string.Format("boolean(/ads:{0})", methodName));
                                }

                                XmlDocument document = req.GetResponseStreamAsXmlDocument();
                                TestUtil.VerifyXPathExpressionResults(document, true, xpaths.ToArray());
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void DummyDataServiceMetadataDocument()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(DummyDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "$metadata";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                TestUtil.AssertSelectNodes(document, "/edmx:Edmx/edmx:DataServices[@adsm:DataServiceVersion='1.0']");

                String[] xpath =
                {
                    "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType)=2)",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='VisibleEntityBase' and not(@BaseType)])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='VisibleEntityBase']/csdl1:Key/csdl1:PropertyRef[@Name='ID'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='VisibleEntityBase']/csdl1:Property[@Name='ID' and @Type='Edm.Int32'])",

                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='VisibleEntityType' and @BaseType='OpenRiaServices.Hosting.WCF.OData.Test.VisibleEntityBase'])",
                    "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='VisibleEntityType']/csdl1:Property)",

                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer[@Name='DummyDomainService'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet[@Name='VisibleEntityTypeSet' and @EntityType='OpenRiaServices.Hosting.WCF.OData.Test.VisibleEntityType'])",
                    "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet)=1)",
                    "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport)=1)",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='GetVisibleEntitiesInvoke' and @EntitySet='VisibleEntityTypeSet' and @adsm:HttpMethod='POST'])",
                };

                TestUtil.VerifyXPathExpressionResults(document, true, xpath);
            }
        }

        [TestMethod]
        public void NoNamespaceDomainServiceMetadataDocument()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NoNamespaceDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "$metadata";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                TestUtil.AssertSelectNodes(document, "/edmx:Edmx/edmx:DataServices[@adsm:DataServiceVersion='1.0']");

                String[] xpath =
                {
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema[@Namespace='NoNamespaceDomainService'])",
                    "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType)=1)",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType[@Name='NoNamespaceEntity'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer[@Name='NoNamespaceDomainService'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet[@Name='NoNamespaceEntitySet'])",
                    "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet)=1)",
                    "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport)",
                };

                TestUtil.VerifyXPathExpressionResults(document, true, xpath);
            }
        }

        [TestMethod]
        public void EchoDomainServiceMetadataDocument()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(EchoDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "$metadata";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                TestUtil.AssertSelectNodes(document, "/edmx:Edmx/edmx:DataServices[@adsm:DataServiceVersion='1.0']");

                String[] xpath =
                {
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema[@Namespace='OpenRiaServices.Hosting.WCF.OData.Test'])",
                    "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityType)",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer[@Name='EchoDomainService' and @adsm:IsDefaultEntityContainer='true'])",
                    "not    (/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:EntitySet)",
                    "boolean(count(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport)=26)",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoString' and @ReturnType='Edm.String' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.String' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoBoolean' and @ReturnType='Edm.Boolean' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Boolean' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableBoolean' and @ReturnType='Edm.Boolean' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Boolean' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoByte' and @ReturnType='Edm.Byte' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Byte' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableByte' and @ReturnType='Edm.Byte' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Byte' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoDateTime' and @ReturnType='Edm.DateTime' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.DateTime' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableDateTime' and @ReturnType='Edm.DateTime' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.DateTime' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoDecimal' and @ReturnType='Edm.Decimal' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Decimal' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableDecimal' and @ReturnType='Edm.Decimal' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Decimal' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoDouble' and @ReturnType='Edm.Double' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Double' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableDouble' and @ReturnType='Edm.Double' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Double' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoGuid' and @ReturnType='Edm.Guid' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Guid' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableGuid' and @ReturnType='Edm.Guid' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Guid' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoInt16' and @ReturnType='Edm.Int16' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Int16' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableInt16' and @ReturnType='Edm.Int16' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Int16' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoInt32' and @ReturnType='Edm.Int32' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Int32' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableInt32' and @ReturnType='Edm.Int32' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Int32' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoInt64' and @ReturnType='Edm.Int64' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Int64' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableInt64' and @ReturnType='Edm.Int64' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Int64' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoSByte' and @ReturnType='Edm.SByte' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.SByte' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableSByte' and @ReturnType='Edm.SByte' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.SByte' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoSingle' and @ReturnType='Edm.Single' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Single' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoNullableSingle' and @ReturnType='Edm.Single' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Single' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoByteArray' and @ReturnType='Edm.Binary' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Binary' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoXElement' and @ReturnType='Edm.String' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.String' and @Mode='In'])",
                    "boolean(/edmx:Edmx/edmx:DataServices/csdl1:Schema/csdl1:EntityContainer/csdl1:FunctionImport[@Name='EchoBinary' and @ReturnType='Edm.Binary' and @adsm:HttpMethod='POST']/csdl1:Parameter[@Name='i' and @Type='Edm.Binary' and @Mode='In'])",
                };

                TestUtil.VerifyXPathExpressionResults(document, true, xpath);
            }
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore] // Test times out, the reson should be identified so it can be resolved
        [TestCategory("Failing")]
        public void InvokeEchoService()
        {
            var testCases = new[]
            {
                new { Uri = "EchoString?i='thIS is A teST!'", ExpectedResult = "boolean(/ads:EchoString='thIS is A teST!')", ExpectedStatusCode = 200 },
                new { Uri = "EchoString?i='thIS is A ''teST!'''", ExpectedResult = "boolean(/ads:EchoString=\"thIS is A 'teST!'\")", ExpectedStatusCode = 200 },
                new { Uri = "EchoString?i='thIS is A ''teST!''''''''''''", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoString?i=thIS is A teST!", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoString?i=binary'1234567898765432123456789876543210'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoString?i='", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoBoolean?i=true", ExpectedResult = "boolean(/ads:EchoBoolean='true')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableBoolean?i=false", ExpectedResult = "boolean(/ads:EchoNullableBoolean='false')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableBoolean?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoByte?i=1", ExpectedResult = "boolean(/ads:EchoByte='1')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableByte?i=0", ExpectedResult = "boolean(/ads:EchoNullableByte='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableByte?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoDateTime?i=datetime'2010-10-10'", ExpectedResult = "boolean(/ads:EchoDateTime='2010-10-10T00:00:00')", ExpectedStatusCode = 200 },
                new { Uri = "EchoDateTime?i='2010-10-10'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoDateTime?i=datetime2010-10-10", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoDateTime?i=datetime'bad datetime'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoNullableDateTime?i=datetime'2010-10-10'", ExpectedResult = "boolean(/ads:EchoNullableDateTime='2010-10-10T00:00:00')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableDateTime?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoDecimal?i=1234567898765432123456789M", ExpectedResult = "boolean(/ads:EchoDecimal='1234567898765432123456789')", ExpectedStatusCode = 200 },
                new { Uri = "EchoDecimal?i=12345678987654321e10M", ExpectedResult = "boolean(/ads:EchoDecimal='123456789876543210000000000')", ExpectedStatusCode = 200 },
                new { Uri = "EchoDecimal?i=1234567898765432123456789", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoDecimal?i=BadDecimal1234567898765432123456789M", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoNullableDecimal?i=1234567898765432123456789M", ExpectedResult = "boolean(/ads:EchoNullableDecimal='1234567898765432123456789')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableDecimal?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoDouble?i=123456789.87654321D", ExpectedResult = "boolean(/ads:EchoDouble='123456789.87654321')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableDouble?i=123456789.87654321D", ExpectedResult = "boolean(/ads:EchoNullableDouble='123456789.87654321')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableDouble?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoGuid?i=guid'" + Guid.Empty.ToString() + "'", ExpectedResult = "boolean(/ads:EchoGuid='" + Guid.Empty.ToString() + "')", ExpectedStatusCode = 200 },
                new { Uri = "EchoGuid?i='" + Guid.Empty.ToString() + "'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoGuid?i=guid" + Guid.Empty.ToString(), ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoGuid?i=guid'BadGuid'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoNullableGuid?i=guid'" + Guid.Empty.ToString() + "'", ExpectedResult = "boolean(/ads:EchoNullableGuid='" + Guid.Empty.ToString() + "')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableGuid?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoInt16?i=0", ExpectedResult = "boolean(/ads:EchoInt16='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableInt16?i=0", ExpectedResult = "boolean(/ads:EchoNullableInt16='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableInt16?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoInt32?i=0", ExpectedResult = "boolean(/ads:EchoInt32='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoInt32?i=BadInt32", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoNullableInt32?i=0", ExpectedResult = "boolean(/ads:EchoNullableInt32='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableInt32?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoInt64?i=0L", ExpectedResult = "boolean(/ads:EchoInt64='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoInt64?i=0", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoNullableInt64?i=0L", ExpectedResult = "boolean(/ads:EchoNullableInt64='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableInt64?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoSByte?i=0", ExpectedResult = "boolean(/ads:EchoSByte='0')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableSByte?i=1", ExpectedResult = "boolean(/ads:EchoNullableSByte='1')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableSByte?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoSingle?i=123.321f", ExpectedResult = "boolean(/ads:EchoSingle='123.321')", ExpectedStatusCode = 200 },
                new { Uri = "EchoSingle?i=123.321", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoNullableSingle?i=123.321f", ExpectedResult = "boolean(/ads:EchoNullableSingle='123.321')", ExpectedStatusCode = 200 },
                new { Uri = "EchoNullableSingle?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoByteArray?i=X'0123456789aAbBcCdDeEfF'", ExpectedResult = "boolean(/ads:EchoByteArray='ASNFZ4mqu8zd7v8=')", ExpectedStatusCode = 200 },
                new { Uri = "EchoByteArray?i=binary'0123456789aAbBcCdDeEfF'", ExpectedResult = "boolean(/ads:EchoByteArray='ASNFZ4mqu8zd7v8=')", ExpectedStatusCode = 200 },
                new { Uri = "EchoByteArray?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoBinary?i=X'1234567898765432123456789876543210'", ExpectedResult = "boolean(/ads:EchoBinary='EjRWeJh2VDISNFZ4mHZUMhA=')", ExpectedStatusCode = 200 },
                new { Uri = "EchoBinary?i=binary'1234567898765432123456789876543210'", ExpectedResult = "boolean(/ads:EchoBinary='EjRWeJh2VDISNFZ4mHZUMhA=')", ExpectedStatusCode = 200 },
                new { Uri = "EchoBinary?i=binary1234567898765432123456789876543210", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoBinary?i=binary'123456789876543212345678987654321'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoBinary?i=binary'0XYZ'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoBinary?i=binary'0XY'''''Z'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoBinary?i=binary'", ExpectedResult = "Syntax error while processing request parameter.", ExpectedStatusCode = 400 },
                new { Uri = "EchoBinary?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
                new { Uri = "EchoXElement?i='<MyElement>Value1</MyElement>'", ExpectedResult = "boolean(contains(/ads:EchoXElement, 'Value1') and contains(/ads:EchoXElement, 'MyElement'))", ExpectedStatusCode = 200 },
                new { Uri = "EchoXElement?i=", ExpectedResult = "Resource not found for the segment", ExpectedStatusCode = 404 },
            };

            LocalWebServerHelper.WebConfigTrustLevelFragment = null;
#if !MEDIUM_TRUST
            foreach (string trustLevel in new[] { "  <trust level=\"Full\"/>\r\n", string.Empty })
#else
            foreach (string trustLevel in new[] { "  <trust level=\"Medium\"/>\r\n", string.Empty })
#endif
            {
                using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "WebConfigTrustLevelFragment"))
                {
                    LocalWebServerHelper.WebConfigTrustLevelFragment = trustLevel;
                    using (TestWebRequest req = TestWebRequest.CreateForLocal())
                    {
                        req.ServiceType = typeof(EchoDomainService);
                        req.HttpMethod = "POST";
                        foreach (var testCase in testCases)
                        {
                            req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + testCase.Uri;
                            Exception ex = TestUtil.RunCatching(req.SendRequest);
                            if (testCase.ExpectedStatusCode == 200)
                            {
                                Assert.AreEqual(200, req.ResponseStatusCode);
                                Assert.IsNull(ex);
                                XmlDocument document = req.GetResponseStreamAsXmlDocument();
                                TestUtil.VerifyXPathExpressionResults(document, true, testCase.ExpectedResult);
                            }
                            else
                            {
                                Assert.IsNotNull(ex);
                                Assert.AreEqual(testCase.ExpectedStatusCode, req.ResponseStatusCode);
                                Assert.IsTrue((new StreamReader((ex as WebException).Response.GetResponseStream())).ReadToEnd().Contains(testCase.ExpectedResult));
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void QueryResultSetQueryable()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "CustomerSet";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:feed/atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(91, result.Count);
            }
        }

        [TestMethod]
        public void QueryResultSetEnumerable()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "OrderSet";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:feed/atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(830, result.Count);
            }
        }

        [TestMethod]
        public void QueryServiceOperationQueryable()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "GetCustomersByCountry?country='USA'";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:feed/atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(13, result.Count);
            }
        }

        [TestMethod]
        public void QueryServiceOperationEnumerable()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "GetCustomersByID()?id='ALFKI'";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:feed/atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(1, result.Count);
            }
        }

        [TestMethod]
        public void QueryServiceOperationSingleton()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "GetOrderByID()?orderId=10248";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(1, result.Count);
            }
        }

        [TestMethod]
        public void QueryServiceOperationMultiParam()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "GetOrdersForCustomerLaterThan()?customerId='VINET'&ordersAfter=datetime'1997-01-01'";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:feed/atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(2, result.Count);
            }
        }

        [TestMethod]
        public void QueryServiceOperationEnumerablePost()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.HttpMethod = "POST";
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "GetOrdersForCustomer()?customerId='VINET'";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:feed/atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(5, result.Count);
            }
        }

        [TestMethod]
        public void QueryServiceOperationSingletonPost()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.HttpMethod = "POST";
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "GetOrderByOrderDate()?orderDate=datetime'1996-07-04'";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                var xpathExpression = "atom:entry";
                XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                Assert.AreEqual(1, result.Count);
            }
        }

        [TestMethod]
        public void IgnoreExcluded()
        {
            LocalWebServerHelper.WebConfigTrustLevelFragment = null;
#if !MEDIUM_TRUST
            foreach (string trustLevel in new[] { "  <trust level=\"Full\"/>\r\n", string.Empty })
#else
            foreach (string trustLevel in new[] { "  <trust level=\"Medium\"/>\r\n", string.Empty })
#endif
            {
                using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "WebConfigTrustLevelFragment"))
                {
                    LocalWebServerHelper.WebConfigTrustLevelFragment = trustLevel;
                    using (TestWebRequest req = TestWebRequest.CreateForLocal())
                    {
                        req.ServiceType = typeof(PersonnelDomainService);
                        req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "PersonSet";
                        req.SendRequest();
                        XmlDocument document = req.GetResponseStreamAsXmlDocument();

                        List<string> xpaths = new List<string>();
                        foreach (Type t in GetAllTypesInHierarchy(typeof(Person)))
                        {
                            foreach (PropertyInfo pi in t.GetProperties())
                            {
                                if (pi.GetCustomAttributes(typeof(ExcludeAttribute), true).Any())
                                {
                                    xpaths.Add(string.Format("not(atom:feed/atom:entry/atom:content/adsm:properties/ads:{0})", pi.Name));
                                }
                            }
                        }

                        Assert.IsTrue(xpaths.Count > 0, "There should be at least 1 property with [Exclude]");
                        TestUtil.VerifyXPathExpressionResults(document, true, xpaths.ToArray());
                    }
                }
            }
        }

        private Type[] GetAllTypesInHierarchy(Type typeInHierarchy)
        {
            Type rootType = typeInHierarchy;
            while (rootType.BaseType != null && rootType.BaseType.Namespace == typeInHierarchy.Namespace)
            {
                rootType = rootType.BaseType;
            }

            return rootType.Assembly.GetTypes().Where(t => rootType.IsAssignableFrom(t)).OrderBy(t => t, new TypeInheritanceComparer()).ToArray();
        }

        /// <summary>
        /// Compares two types based on inheritance relationship.
        /// </summary>
        private class TypeInheritanceComparer : IComparer<Type>
        {
            /// <summary>
            /// Compares two types based on inheritance relationship.
            /// </summary>
            /// <param name="x">Left type.</param>
            /// <param name="y">Right type.</param>
            /// <returns>0 if equal, -1 if left is base of right, 1 otherwise.</returns>
            public int Compare(Type x, Type y)
            {
                return (x == y) ? 0 : x.IsAssignableFrom(y) ? -1 : 1;
            }
        }

        [TestMethod]
        public void IgnoreNonDataMember()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(PersonnelDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "PersonSet";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                List<string> xpaths = new List<string>();
                foreach (Type t in GetAllTypesInHierarchy(typeof(Person)))
                {
                    foreach (PropertyInfo pi in t.GetProperties())
                    {
                        if (pi.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Any())
                        {
                            xpaths.Add(string.Format("not(atom:feed/atom:entry/atom:content/adsm:properties/ads:{0})", pi.Name));
                        }
                    }
                }

                Assert.IsTrue(xpaths.Count > 0, "There should be at least 1 property with [IgnoreDataMember]");
                TestUtil.VerifyXPathExpressionResults(document, true, xpaths.ToArray());
            }
        }

        [TestMethod]
        public void UseDataMemberName()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(PersonnelDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "PersonSet";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                List<string> xpaths = new List<string>();
                foreach (Type t in GetAllTypesInHierarchy(typeof(Person)))
                {
                    foreach (PropertyInfo pi in t.GetProperties())
                    {
                        DataMemberAttribute attr = pi.GetCustomAttributes(typeof(DataMemberAttribute), true).Cast<DataMemberAttribute>().SingleOrDefault(a => !string.IsNullOrEmpty(a.Name));
                        if (attr != null)
                        {
                            Assert.IsFalse(string.IsNullOrEmpty(attr.Name), "Expect Name to be set.");
                            xpaths.Add(string.Format("boolean(atom:feed/atom:entry/atom:content/adsm:properties/ads:{0})", attr.Name));
                        }
                    }
                }

                Assert.IsTrue(xpaths.Count > 0, "There should be at least 1 property with DataMemberAttribute.Name set");
                TestUtil.VerifyXPathExpressionResults(document, true, xpaths.ToArray());
            }
        }

        [TestMethod]
        public void VerifyProjectedProperties()
        {
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(PersonnelDomainService);
                req.RequestUriString = req.BaseUri + TestUtil.ODataEndPointPath + "PersonSet";
                req.SendRequest();
                XmlDocument document = req.GetResponseStreamAsXmlDocument();

                String[] xpath =
                {
                    "boolean(/atom:feed/atom:entry[atom:link[@rel='edit' and contains(@href, 'PersonSet(3)')] and atom:content/adsm:properties/ads:NameLength=3])",
                    "boolean(/atom:feed/atom:entry[atom:link[@rel='edit' and contains(@href, 'PersonSet(3)')] and atom:content/adsm:properties/ads:ProjectedLength=11])",
                    "boolean(/atom:feed/atom:entry[atom:link[@rel='edit' and contains(@href, 'PersonSet(3)')] and atom:content/adsm:properties/ads:BestFriendName='Andrew'])"
                };

                TestUtil.VerifyXPathExpressionResults(document, true, xpath);
            }
        }
    }

    [TestClass]
    public class DomainDataServiceUnitTestsNegative
    {
        [TestMethod]
        public void DisallowJson()
        {
            string[] requestUrisGet = new[] {
                TestUtil.ODataEndPointPath,
                TestUtil.ODataEndPointPath + "$metadata",
                TestUtil.ODataEndPointPath + "CustomerSet",
                TestUtil.ODataEndPointPath + "GetOrderByID()?orderId=10248",
                TestUtil.ODataEndPointPath + "GetCustomersByID()?id='ALFKI'"
            };

            string[] requestUrisPost = new[] {
                TestUtil.ODataEndPointPath + "GetOrdersForCustomer()?customerId='VINET'",
                TestUtil.ODataEndPointPath + "GetOrderByOrderDate()?orderDate=datetime'1996-07-04'"
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                foreach (var requestUri in requestUrisGet)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    req.Accept = "APPLICATION/json";
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, ((e as WebException).Response as HttpWebResponse).StatusCode);
                }

                foreach (var requestUri in requestUrisPost)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    req.HttpMethod = "POST";
                    req.Accept = "AppliCation/jsOn";
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, ((e as WebException).Response as HttpWebResponse).StatusCode);
                }
            }
        }

        [TestMethod]
        public void DisallowPostForMetadata()
        {
            string[] requestUrisPost = new[] {
                TestUtil.ODataEndPointPath,
                TestUtil.ODataEndPointPath + "$metadata",
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);

                foreach (var requestUri in requestUrisPost)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    req.HttpMethod = "POST";
                    req.Accept = "application/JSon";
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.MethodNotAllowed, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    Assert.AreEqual("GET", ((e as WebException).Response as HttpWebResponse).Headers[HttpResponseHeader.Allow]);
                }
            }
        }

        [TestMethod]
        public void DisallowPostForGetMethods()
        {
            string[] requestUrisPost = new[] {
                TestUtil.ODataEndPointPath + "CustomerSet",
                TestUtil.ODataEndPointPath + "GetCustomersByCountry?country='USA'",
                TestUtil.ODataEndPointPath + "GetOrderByID()?orderId=10248"
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);

                foreach (var requestUri in requestUrisPost)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    req.HttpMethod = "POST";
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.MethodNotAllowed, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    Assert.AreEqual("GET", ((e as WebException).Response as HttpWebResponse).Headers[HttpResponseHeader.Allow]);
                }
            }
        }

        [TestMethod]
        public void DisallowQueryOptions()
        {
            string[] requestUrisGet = new[] {
                TestUtil.ODataEndPointPath + "CustomerSet?$filter= 1 eq 1",
                TestUtil.ODataEndPointPath + "CustomerSet?$orderby=CustomerID",
                TestUtil.ODataEndPointPath + "CustomerSet?$top=1",
                TestUtil.ODataEndPointPath + "CustomerSet?$skip=1",
                TestUtil.ODataEndPointPath + "CustomerSet?$skiptoken='ALFKI'",
                TestUtil.ODataEndPointPath + "CustomerSet?$expand=Orders",
                TestUtil.ODataEndPointPath + "CustomerSet?$orderby=CustomerID&$expand=Orders",
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);

                foreach (var requestUri in requestUrisGet)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.BadRequest, ((e as WebException).Response as HttpWebResponse).StatusCode);

                    XmlDocument document = req.GetResponseStreamAsXmlDocument();
                    var xpathExpression = "adsm:error/adsm:message/child::text()";
                    XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                    Assert.AreEqual(1, result.Count);
                    Assert.IsTrue(result[0].Value.Contains("Query options"));
                }
            }
        }

        [TestMethod]
        public void DisallowMultiSegments()
        {
            string[] requestUrisGet = new[] {
                TestUtil.ODataEndPointPath + "CustomerSet/$count",
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);

                foreach (var requestUri in requestUrisGet)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.BadRequest, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("Requests that have multiple segments are not allowed."));
                }
            }
        }

        [TestMethod]
        public void DisallowKeys()
        {
            string[] requestUrisGet = new[] {
                TestUtil.ODataEndPointPath + "CustomerSet('ALFKI')",
                TestUtil.ODataEndPointPath + "GetCustomersByCountry('BAABA')?country='USA'",
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);

                foreach (var requestUri in requestUrisGet)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.AreEqual(HttpStatusCode.BadRequest, ((e as WebException).Response as HttpWebResponse).StatusCode);

                    XmlDocument document = req.GetResponseStreamAsXmlDocument();
                    var xpathExpression = "adsm:error/adsm:message/child::text()";
                    XmlNodeList result = document.SelectNodes(xpathExpression, TestUtil.TestNamespaceManager);
                    Assert.AreEqual(1, result.Count);
                    Assert.IsTrue(result[0].Value.Contains("key values"));
                }
            }
        }

        [TestMethod]
        public void DisallowUnauthenticatedAccess1()
        {
            var testCases = new[] {
                new { Url = TestUtil.ODataEndPointPath, AuthenticationException = false, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", AuthenticationException = false, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "PersonSet", AuthenticationException = true, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsQueryAuth", AuthenticationException = true, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsInvokeAuth", AuthenticationException = true, Method = "POST" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsQueryRole", AuthenticationException = true, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsInvokeRole", AuthenticationException = true, Method = "POST" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsQuery", AuthenticationException = false, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsInvoke", AuthenticationException = false, Method = "POST" },
            };

            LocalWebServerHelper.AuthenticationModeFragment = null;
            using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "AuthenticationModeFragment"))
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                LocalWebServerHelper.AuthenticationModeFragment = "  <authentication mode=\"None\"/>\r\n";

                req.ServiceType = typeof(PartialAuthenticatedPersonnelDomainService);

                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    if (testCase.AuthenticationException)
                    {
                        Assert.IsNotNull(e, "Expect exception but did not receive one!");
                        Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                        Assert.AreEqual(HttpStatusCode.Unauthorized, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    }
                    else
                    {
                        Assert.IsNull(e, "Did not expect exception but received one!");
                    }
                }
            }
        }

        [TestMethod]
        public void DisallowUnauthenticatedAccess2()
        {
            var testCases = new[] {
                new { Url = TestUtil.ODataEndPointPath, AuthenticationException = false, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", AuthenticationException = false, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "PersonSet", AuthenticationException = true, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsQuery", AuthenticationException = true, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsInvoke", AuthenticationException = true, Method = "POST" },
            };

            LocalWebServerHelper.AuthenticationModeFragment = null;
            using (TestUtil.RestoreStaticValueOnDispose(typeof(LocalWebServerHelper), "AuthenticationModeFragment"))
            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                LocalWebServerHelper.AuthenticationModeFragment = "  <authentication mode=\"None\"/>\r\n";

                req.ServiceType = typeof(FullAuthenticatedPersonnelDomainService);

                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    if (testCase.AuthenticationException)
                    {
                        Assert.IsNotNull(e, "Expect exception but did not receive one!");
                        Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                        Assert.AreEqual(HttpStatusCode.Unauthorized, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    }
                    else
                    {
                        Assert.IsNull(e, "Did not expect exception but received one!");
                    }
                }
            }
        }

        [TestMethod]
        public void DisallowNoClientAccess()
        {
            var testCases = new[] {
                new { Url = TestUtil.ODataEndPointPath, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "PersonSet", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsQuery", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonsInvoke", Method = "POST" },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NoClientAccessPersonnelDomainService);

                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    Assert.IsNotNull(e, "Expect exception but did not receive one!");
                    Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                    Assert.AreEqual(HttpStatusCode.NotFound, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    if (testCase.Method == "GET")
                    {
                        Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("There was no channel actively listening"));
                    }
                }
            }
        }

        [TestMethod]
        public void SkipEndPointCreationForNoOperation()
        {
            var testCases = new[] {
                new { Url = TestUtil.ODataEndPointPath, Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "PersonSet", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersons", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetPersonByTimeSpan", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "EchoTimeSpan", Method = "POST" },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NoODataOperaionDomainService);

                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    Assert.IsNotNull(e, "Expect exception but did not receive one!");
                    Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                    Assert.AreEqual(HttpStatusCode.NotFound, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    if (testCase.Method == "GET")
                    {
                        Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("There was no channel actively listening"));
                    }
                }
            }
        }

        [TestMethod]
        public void DisallowServiceOpWithUnsupportedParam()
        {
            var testCases = new[] {
                new { Url = TestUtil.ODataEndPointPath + "ServiceOpWithUnsupportedParam", Method = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "ServiceOpWithUnsupportedParam", Method = "POST" },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(PersonnelDomainService);

                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    Assert.IsNotNull(e, "Expect exception but did not receive one!");
                    Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                    Assert.AreEqual(HttpStatusCode.NotFound, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("The DomainService method corresponding to the given request could not be found."));
                }
            }
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore] // Test times out, the reson should be identified so it can be resolved
        [TestCategory("Failing")]
        public void DisallowInvalidMethods()
        {
            var testCases = new[]
            {
                new { Url = TestUtil.ODataEndPointPath, AllowedMethods = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", AllowedMethods = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "PersonSet", AllowedMethods = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetNPersonsQuery?count=1", AllowedMethods = "GET" },
                new { Url = TestUtil.ODataEndPointPath + "GetNPersonsInvoke?count=1", AllowedMethods = "POST" },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(PersonnelDomainService);
                foreach (string method in new[] { "POST", "PUT", "MERGE", "HEAD", "DELETE", "GET" })
                {
                    foreach (var testCase in testCases)
                    {
                        req.RequestUriString = req.BaseUri + testCase.Url;
                        req.HttpMethod = method;
                        Exception e = TestUtil.RunCatching(req.SendRequest);

                        if (testCase.AllowedMethods.Contains(method))
                        {
                            Assert.IsNull(e, "Do not expect exception but received one.");
                        }
                        else
                        {
                            Assert.IsNotNull(e, "Expect exception but did not receive one!");
                            Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);

                            var statusCode = ((e as WebException).Response as HttpWebResponse)?.StatusCode;
                            if (statusCode != HttpStatusCode.LengthRequired)
                                Assert.AreEqual(HttpStatusCode.MethodNotAllowed, ((e as WebException).Response as HttpWebResponse).StatusCode);
                            //                            Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("The domain service method corresponding to the given request could not be found."));
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void ExceptionTest()
        {
            var testCases = new[]
            {
                new { Url = TestUtil.ODataEndPointPath, Method = "GET", ExpectException = false },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", Method = "GET", ExpectException = false },
                new { Url = TestUtil.ODataEndPointPath + "CustomerSet", Method = "GET", ExpectException = true },
                new { Url = TestUtil.ODataEndPointPath + "GetCustomerByName?name='bob'", Method = "GET", ExpectException = true },
                new { Url = TestUtil.ODataEndPointPath + "GetCustomerCount", Method = "POST", ExpectException = true },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NotImplementedDomainService);
                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    if (!testCase.ExpectException)
                    {
                        Assert.IsNull(e, "Do not expect exception but received one.");
                    }
                    else
                    {
                        Assert.IsNotNull(e, "Expect exception but did not receive one!");
                        Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                        Assert.AreEqual(HttpStatusCode.InternalServerError, ((e as WebException).Response as HttpWebResponse).StatusCode);
                        Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("The method or operation is not implemented."));
                    }
                }
            }
        }

        [TestMethod]
        public void ThrowInDomainServiceConstructorTest()
        {
            var testCases = new[]
            {
                new { Url = TestUtil.ODataEndPointPath, Method = "GET", ExpectException = false },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", Method = "GET", ExpectException = false },
                new { Url = TestUtil.ODataEndPointPath + "CustomerSet", Method = "GET", ExpectException = true },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(ThrowInConstructorDomainService);
                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    if (!testCase.ExpectException)
                    {
                        Assert.IsNull(e, "Do not expect exception but received one.");
                    }
                    else
                    {
                        Assert.IsNotNull(e, "Expect exception but did not receive one!");
                        Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                        Assert.AreEqual(HttpStatusCode.InternalServerError, ((e as WebException).Response as HttpWebResponse).StatusCode);
                        Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("Bad Service -- throw in domain service constructor!"));
                    }
                }
            }
        }

        [TestMethod]
        public void RequiresSecureEndpointTest()
        {
            var testCases = new[]
            {
                new { Url = TestUtil.ODataEndPointPath, Method = "GET", ExpectException = true },
                new { Url = TestUtil.ODataEndPointPath + "$metadata", Method = "GET", ExpectException = true },
                new { Url = TestUtil.ODataEndPointPath + "CustomerSet", Method = "GET", ExpectException = true },
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(RequireSecureEndpointDomainService);
                foreach (var testCase in testCases)
                {
                    req.RequestUriString = req.BaseUri + testCase.Url;
                    req.HttpMethod = testCase.Method;
                    Exception e = TestUtil.RunCatching(req.SendRequest);

                    if (!testCase.ExpectException)
                    {
                        Assert.IsNull(e, "Do not expect exception but received one.");
                    }
                    else
                    {
                        Assert.IsNotNull(e, "Expect exception but did not receive one!");
                        Assert.IsTrue(e is WebException && (e as WebException).Response is HttpWebResponse);
                        Assert.AreEqual(HttpStatusCode.NotFound, ((e as WebException).Response as HttpWebResponse).StatusCode);
                        Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("There was no channel actively listening at"));
                    }
                }
            }
        }

        [TestMethod]
        public void VerifyValidationError()
        {
            string[] requestUris = new[] {
                TestUtil.ODataEndPointPath + "GetOrderCount",
            };

            using (TestWebRequest req = TestWebRequest.CreateForLocal())
            {
                req.ServiceType = typeof(NorthwindDomainService);
                req.HttpMethod = "POST";
                foreach (var requestUri in requestUris)
                {
                    req.RequestUriString = req.BaseUri + requestUri;
                    Exception e = TestUtil.RunCatching(req.SendRequest);
                    Assert.IsNotNull(e);
                    Assert.AreEqual(HttpStatusCode.InternalServerError, ((e as WebException).Response as HttpWebResponse).StatusCode);
                    Assert.IsTrue((new StreamReader((e as WebException).Response.GetResponseStream())).ReadToEnd().Contains("An error occurred during execution of the DomainService operation. Inspect the OperationErrors property for more information."));
                }
            }
        }
    }
}

public class NoNamespaceEntity
{
    [Key]
    public int ID { get; set; }
}

[EnableClientAccess]
class NoNamespaceDomainService : DomainService
{
    private static List<NoNamespaceEntity> entitySet = new List<NoNamespaceEntity>();

    [Query(IsDefault = true)]
    public IQueryable<NoNamespaceEntity> GetEntitySet()
    {
        return entitySet.AsQueryable<NoNamespaceEntity>();
    }
}
