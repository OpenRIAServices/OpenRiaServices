extern alias DomainServices;
extern alias DomainServicesTests;
extern alias WebRia;
extern alias SSmDsWeb;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using DataTests.AdventureWorks.LTS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices.LTS;
using VbExpressions;
using DomainServiceDescription = DomainServices::OpenRiaServices.DomainServices.Server.DomainServiceDescription;
using NorthwindDomainService = DomainServicesTests::TestDomainServices.LTS.Northwind;
using LinqResource = SSmDsWeb::OpenRiaServices.DomainServices.Client.Resources;
using SystemLinqDynamic = WebRia::System.Linq.Dynamic;

namespace OpenRiaServices.DomainServices.Client.Test
{
    /// <summary>
    /// Tests of both the client and server query serialization pieces via internal direct access to those
    /// components. These tests run desktop only. Note : the server components being tested are cross compiled
    /// into the IsolatedServerCode library which is referenced by this test assembly.
    /// </summary>
    [TestClass]
    public class QuerySerializationTests
    {
        /// <summary>
        /// Verify that queries involving null comparisons work as expected
        /// </summary>
        [TestMethod]
        public void TestNullable()
        {
            IQueryable<GenericEntity> entities = new GenericEntity[]
            {
                new GenericEntity { Key = 1, Title = "Manager", NullableInt = null },
                new GenericEntity { Key = 2, Title = "Supervisor", NullableInt = -5 },
                new GenericEntity { Key = 3, Title = "Supervisor", NullableInt = 5 }
            }.AsQueryable();

            // We don't currently support the delegate syntax. Once we add support for this, we should 
            // uncomment the following code.
            /*// Where (item.NullableInt > 0)
            IQueryable<GenericEntity> q = (IQueryable<GenericEntity>)Expressions.NullComparison1(entities);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.NullableInt>0)", queryParts.Single().Expression);
            IQueryable<GenericEntity> q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            
            // We don't execute the 'q' query because it'll fail when there are null ints. That's by-VB-design.
            Assert.AreEqual(1, q2.Count());
            Assert.AreEqual(5, q2.First().NullableInt);*/

            // Where (item.NullableInt > 0) (uses LINQ syntax)
            IQueryable<GenericEntity> q = (IQueryable<GenericEntity>)Expressions.NullComparison2(entities);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.NullableInt>0)", queryParts.Single().Expression);
            IQueryable<GenericEntity> q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            int c1 = q.Count();
            int c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
            Assert.AreEqual(5, q2.First().NullableInt);

            // Where (item.NullableInt > item.Key) (uses LINQ syntax)
            q = (IQueryable<GenericEntity>)Expressions.NullComparison3(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.NullableInt>it.Key)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
            Assert.AreEqual(5, q2.First().NullableInt);
        }

        /// <summary>
        /// Verify that queries involving inequality checks work as expected
        /// </summary>
        [TestMethod]
        public void TestEnumComparisons()
        {
            // simple equality
            Cities.State[] states = new Cities.State[0];
            IQueryable<Cities.State> q1 = new Cities.State[0].AsQueryable();
            q1 = q1.Where(p => p.TimeZone == Cities.TimeZone.Eastern);
            IQueryable<Cities.State> q2 = (IQueryable<Cities.State>)RoundtripQuery(q1, states.AsQueryable());

            // inequality against null
            q1 = new Cities.State[0].AsQueryable();
            q1 = q1.Where(p => p.TimeZone > null);
            q2 = (IQueryable<Cities.State>)RoundtripQuery(q1, states.AsQueryable());
            q1 = new Cities.State[0].AsQueryable();
            q1 = q1.Where(p => null < p.TimeZone);
            q2 = (IQueryable<Cities.State>)RoundtripQuery(q1, states.AsQueryable());

            // inequality against an enum value
            q1 = new Cities.State[0].AsQueryable();
            q1 = q1.Where(p => p.TimeZone >= Cities.TimeZone.Eastern);
            q2 = (IQueryable<Cities.State>)RoundtripQuery(q1, states.AsQueryable());
            q1 = new Cities.State[0].AsQueryable();
            q1 = q1.Where(p => Cities.TimeZone.Eastern <= p.TimeZone);
            q2 = (IQueryable<Cities.State>)RoundtripQuery(q1, states.AsQueryable());

            // test where both sides are a member expression
            q1 = new Cities.State[0].AsQueryable();
            q1 = q1.Where(p => p.TimeZone >= p.TimeZone);
            q2 = (IQueryable<Cities.State>)RoundtripQuery(q1, states.AsQueryable());
        }

        [TestMethod]
        public void TestEnumHasFlags()
        {
            EntityWithEnums [] entities = new EntityWithEnums[]
            {
                new EntityWithEnums() { Id = 1, EnumProp1 = QuerySerialisationEnum.A, EnumProp2 = QuerySerialisationEnum.None },
                new EntityWithEnums() { Id = 2, EnumProp1 = QuerySerialisationEnum.A, EnumProp2 = QuerySerialisationEnum.A },
                new EntityWithEnums() { Id = 3, EnumProp1 = QuerySerialisationEnum.B, EnumProp2 = QuerySerialisationEnum.A },
                new EntityWithEnums() { Id = 4, EnumProp1 = QuerySerialisationEnum.A |QuerySerialisationEnum.B, EnumProp2 = QuerySerialisationEnum.A },
                new EntityWithEnums() { Id = 5, EnumProp1 = QuerySerialisationEnum.B, EnumProp2 = QuerySerialisationEnum.B | QuerySerialisationEnum.A },
            };

            // Has flags against constant
            IQueryable<EntityWithEnums> q1 = Array.Empty<EntityWithEnums>().AsQueryable();
            q1 = q1.Where(p => p.EnumProp1.HasFlag(QuerySerialisationEnum.A));
            IQueryable<EntityWithEnums> q2 = (IQueryable<EntityWithEnums>)RoundtripQuery(q1, entities.AsQueryable());
            CollectionAssert.AreEquivalent(q2.ToList(), new [] { entities[0], entities[1], entities[3],});

            // Has flags against integer constant
            q1 = Array.Empty<EntityWithEnums>().AsQueryable();
            q1 = q1.Where(p => p.EnumProp1.HasFlag((QuerySerialisationEnum)(2)));
            q2 = (IQueryable<EntityWithEnums>)RoundtripQuery(q1, entities.AsQueryable());
            CollectionAssert.AreEquivalent(q2.ToList(), new[] { entities[2], entities[3], entities[4] });

            //  Has flags against member constant
            q1 = Array.Empty<EntityWithEnums>().AsQueryable();
            q1 = q1.Where(p => p.EnumProp1.HasFlag(p.EnumProp2));
            q2 = (IQueryable<EntityWithEnums>)RoundtripQuery(q1, entities.AsQueryable());
            CollectionAssert.AreEquivalent(q2.ToList(), entities.Where(e => e.EnumProp1.HasFlag(e.EnumProp2)).ToList());


            q1 = Array.Empty<EntityWithEnums>().AsQueryable();
            q1 = q1.Where(p => p.EnumProp2.HasFlag(p.EnumProp1));
            q2 = (IQueryable<EntityWithEnums>)RoundtripQuery(q1, entities.AsQueryable());
            CollectionAssert.AreEquivalent(q2.ToList(), entities.Where(e => e.EnumProp2.HasFlag(e.EnumProp1)).ToList());
        }

        [TestMethod]
        [Description("Verify that VB checked expressions are treated as unchecked expressions.")]
        public void TestVBCheckedExpressions()
        {
            IQueryable<GenericEntity> entities = new GenericEntity[]
            {
                new GenericEntity { Key = 1, Title = "Manager" },
                new GenericEntity { Key = 2, Title = "Supervisor" }
            }.AsQueryable();

            // Where (item.Key + -1 = 0)
            IQueryable<GenericEntity> q = (IQueryable<GenericEntity>)Expressions.AddAndNegateChecked(entities);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("((it.Key+-1)==0)", queryParts.Single().Expression);
            IQueryable<GenericEntity> q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            int c1 = q.Count();
            int c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
            Assert.AreEqual("Manager", q2.First().Title);

            // Where (item.Key - 1 = 0)
            q = (IQueryable<GenericEntity>)Expressions.SubtractChecked(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("((it.Key-1)==0)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
            Assert.AreEqual("Manager", q2.First().Title);

            // Where (item.Key * 1 = 1)
            q = (IQueryable<GenericEntity>)Expressions.MultiplyChecked(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("((it.Key*1)==1)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
            Assert.AreEqual("Manager", q2.First().Title);

            // Where (CType(item.Key, Single) = 1)
            q = (IQueryable<GenericEntity>)Expressions.ConvertChecked(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.Key==1F)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
            Assert.AreEqual("Manager", q2.First().Title);
        }

        [TestMethod]
        public void TestVBCompareString()
        {
            IQueryable<GenericEntity> entities = new GenericEntity[]
            {
                new GenericEntity { Title = "Manager" },
                new GenericEntity { Title = "Peon" },
                new GenericEntity { Title = "Supreme Leader" },
                new GenericEntity { Title = "X" },
                new GenericEntity { Title = null }
            }.AsQueryable();

            // VB string comparison EQ
            IQueryable<GenericEntity> q = (IQueryable<GenericEntity>)StringComparisons.CompareStringEqual(entities);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.Title==\"Supreme Leader\")", queryParts.Single().Expression);
            IQueryable<GenericEntity> q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            int c1 = q.Count();
            int c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // VB string comparison EQ (case insensitive)
            q = (IQueryable<GenericEntity>)StringComparisonsCaseInsensitive.CompareStringEqualCaseInsensitive(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(String.Compare(it.Title, \"peon\", StringComparison.OrdinalIgnoreCase)==0)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // VB string comparison NE
            q = (IQueryable<GenericEntity>)StringComparisons.CompareStringNotEqual(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.Title!=\"Supreme Leader\")", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // VB string comparison GT
            q = (IQueryable<GenericEntity>)StringComparisons.CompareStringGreaterThan(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(String.Compare(it.Title, \"Supreme Leader\")>0)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // VB string comparison GTE
            q = (IQueryable<GenericEntity>)StringComparisons.CompareStringGreaterThanOrEqual(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(String.Compare(it.Title, \"Supreme Leader\")>=0)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // VB string comparison LT
            q = (IQueryable<GenericEntity>)StringComparisons.CompareStringLessThan(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(String.Compare(it.Title, \"Supreme Leader\")<0)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // VB string comparison LTE
            q = (IQueryable<GenericEntity>)StringComparisons.CompareStringLessThanOrEqual(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(String.Compare(it.Title, \"Supreme Leader\")<=0)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);
        }

        [TestMethod]
        public void TestVBIIf()
        {
            IQueryable<GenericEntity> entities = new GenericEntity[]
            {
                new GenericEntity { Title = "Manager" },
                new GenericEntity { Title = "Peon" },
                new GenericEntity { Title = "Supreme Leader" },
                new GenericEntity { Title = "X" },
                new GenericEntity { Title = null }
            }.AsQueryable();

            // Where (IIf(item.Title = "Supreme Leader", 1, 0) = 1)
            IQueryable<GenericEntity> q = (IQueryable<GenericEntity>)Expressions.IIfWithEqualComparison(entities);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(iif((it.Title==\"Supreme Leader\"),1,0)==1)", queryParts.Single().Expression);
            IQueryable<GenericEntity> q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            int c1 = q.Count();
            int c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where (IIf(item.Title = "Supreme Leader", True, False))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithEqualComparisonWithBools(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("iif((it.Title==\"Supreme Leader\"),True,False)", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where (1 <> IIf(item.Title = "Supreme Leader", 1, 0))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithNotEqualComparison(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(1!=iif((it.Title==\"Supreme Leader\"),1,0))", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where (1 < IIf(item.Title = "Supreme Leader", 1, 0))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithLessThanComparison(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(1<iif((it.Title==\"Supreme Leader\"),1,0))", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where (1 >= IIf(item.Title = "Supreme Leader", 1, 0))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithGreaterThanOrEqualComparison(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(1>=iif((it.Title==\"Supreme Leader\"),1,0))", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where (1 > IIf(item.Title = "Supreme Leader", 1, 0))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithGreaterThanComparison(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(1>iif((it.Title==\"Supreme Leader\"),1,0))", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where (1 <= IIf(item.Title = "Supreme Leader", 1, 0))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithLessThanOrEqualComparison(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(1<=iif((it.Title==\"Supreme Leader\"),1,0))", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);

            // Where ("1" < IIf(item.Title = "Supreme Leader", "1", "0"))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithLessThanStringComparison(entities);
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                QuerySerializer.Serialize(q);
            }, "The binary operator LessThan is not defined for the types 'System.String' and 'System.String'.");

            // Where ("1" = IIf(item.Title = "Supreme Leader", "1", "0"))
            q = (IQueryable<GenericEntity>)Expressions.IIfWithEqualStringComparison(entities);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(\"1\"==iif((it.Title==\"Supreme Leader\"),\"1\",\"0\"))", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(c1, c2);
        }

        [TestMethod]
        public void TestReservedNames()
        {
            IQueryable<EmployeeWithReservedNames> entities = new EmployeeWithReservedNames[]
            {
                new EmployeeWithReservedNames { Id = 1, @true = true, iif = true },
                new EmployeeWithReservedNames { Id = 1, @true = false, iif = false },
            }.AsQueryable();

            IQueryable<EmployeeWithReservedNames> q = entities.Where(e => e.@true);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("it.true", queryParts.Single().Expression);
            IQueryable<EmployeeWithReservedNames> q2 = (IQueryable<EmployeeWithReservedNames>)RoundtripQuery(q, entities);
            int c1 = q.Count();
            int c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);

            q = entities.Where(e => e.iif);
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("it.iif", queryParts.Single().Expression);
            q2 = (IQueryable<EmployeeWithReservedNames>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
        }

        [TestMethod]
        public void TestCharQueries()
        {
            EmployeeWithCharProperty[] emps = new EmployeeWithCharProperty[] { new EmployeeWithCharProperty { GenderAsChar = 'M' }, new EmployeeWithCharProperty { GenderAsChar = 'F' }, new EmployeeWithCharProperty { GenderAsChar = 'F' } };
            IQueryable<EmployeeWithCharProperty> query1 = emps.AsQueryable().Where(p => p.GenderAsChar == 'M');
            IQueryable<EmployeeWithCharProperty> query2 = (IQueryable<EmployeeWithCharProperty>)RoundtripQuery(query1, emps.AsQueryable());
            Assert.AreEqual(1, query2.Count());

            query1 = emps.AsQueryable().Where(p => p.GenderAsChar < 'M');
            query2 = (IQueryable<EmployeeWithCharProperty>)RoundtripQuery(query1, emps.AsQueryable());
            Assert.AreEqual(2, query2.Count());

            query1 = emps.AsQueryable().Where(p => p.GenderAsChar == null);
            query2 = (IQueryable<EmployeeWithCharProperty>)RoundtripQuery(query1, emps.AsQueryable());
            Assert.AreEqual(0, query2.Count());

            // verify that manually created expression trees that use char rather than conversions
            // to int (as the C# compiler does) work
            query1 = emps.AsQueryable();
            ParameterExpression pex = System.Linq.Expressions.Expression.Parameter(typeof(EmployeeWithCharProperty), "p");
            char c = 'm';
            MemberExpression mex = System.Linq.Expressions.Expression.MakeMemberAccess(pex, typeof(EmployeeWithCharProperty).GetProperty("GenderAsChar"));
            BinaryExpression comparison = System.Linq.Expressions.Expression.MakeBinary(ExpressionType.Equal, mex, System.Linq.Expressions.Expression.Constant(c));
            LambdaExpression lex = System.Linq.Expressions.Expression.Lambda(comparison, pex);
            query1 = Queryable.Where(query1, (Expression<Func<EmployeeWithCharProperty, bool>>)lex);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query1);
            Assert.AreEqual("(it.GenderAsChar=='m')", queryParts.Single().Expression);
            query2 = (IQueryable<EmployeeWithCharProperty>)RoundtripQuery(query1, emps.AsQueryable());
        }

        [TestMethod]
        public void TestEscapingInStrings()
        {
            IQueryable<GenericEntity> entities = new GenericEntity[]
            {
                new GenericEntity { Title = "Supreme \"Leader\"" },
                new GenericEntity { Title = "Supreme \\\"Leader\\\"" },
                new GenericEntity { Title = "Supreme \'Leader\'" },
                new GenericEntity { Title = null }
            }.AsQueryable();

            IQueryable<GenericEntity> q = entities.Where(e => e.Title == "Supreme \"Leader\"");
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.Title==\"Supreme \\\"Leader\\\"\")", queryParts.Single().Expression);
            IQueryable<GenericEntity> q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            int c1 = q.Count();
            int c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);

            q = entities.Where(e => e.Title == "Supreme \\\"Leader\\\"");
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.Title==\"Supreme \\\\\\\"Leader\\\\\\\"\")", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);

            q = entities.Where(e => e.Title == "Supreme \'Leader\'");
            queryParts = QuerySerializer.Serialize(q);
            Assert.AreEqual("(it.Title==\"Supreme \'Leader\'\")", queryParts.Single().Expression);
            q2 = (IQueryable<GenericEntity>)RoundtripQuery(q, entities);
            c1 = q.Count();
            c2 = q2.Count();
            Assert.AreEqual(1, c1);
            Assert.AreEqual(c1, c2);
        }

        [TestMethod]
        public void NestedQueriesNotSupported()
        {
            IQueryable<Product> query1 = BaselineTestData.Products.AsQueryable().Where(p => p.PurchaseOrderDetails.Where(q => q.OrderQty > 1).Take(1) != null);
            NotSupportedException expectedException = null;
            try
            {
                RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(LinqResource.QuerySerialization_NestedQueriesNotSupported, expectedException.Message);
        }

        [TestMethod]
        public void TestNullableMethodAccess()
        {
            Employee[] employees = new Employee[] { new Employee { EmployeeID = 1, ManagerID = 1 }, new Employee { EmployeeID = 1, ManagerID = null } };

            Assert.IsTrue(typeof(Employee).GetProperty("ManagerID").PropertyType == typeof(Nullable<int>));

            IQueryable<Employee> query = Array.Empty<Employee>().AsQueryable().Where(p => p.ManagerID == 1);
            IQueryable<Employee> query2 = (IQueryable<Employee>)RoundtripQuery(query, employees.AsQueryable());
            Assert.AreEqual(1, query2.Count());

            query = Array.Empty<Employee>().AsQueryable().Where(p => p.ManagerID == null);
            query2 = (IQueryable<Employee>)RoundtripQuery(query, employees.AsQueryable());
            Assert.AreEqual(1, query2.Count());
        }

        [TestMethod]
        public void TestInaccessibleMethods()
        {
            Employee[] employees = new Employee[] { new Employee { EmployeeID = 1 } };

            IQueryable<Employee> query = Array.Empty<Employee>().AsQueryable().Where(p => p.EmployeeID == LocalMethod(p.EmployeeID));
            IQueryable<Employee> query2 = null;
            NotSupportedException expectedException = null;
            try
            {
                query2 = (IQueryable<Employee>)RoundtripQuery(query, employees.AsQueryable());
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(LinqResource.QuerySerialization_MethodNotAccessible, "LocalMethod", typeof(QuerySerializationTests)), expectedException.Message);

            // test an unsupported static method
            expectedException = null;
            query = Array.Empty<Employee>().AsQueryable().AsQueryable().Where(p => p.EmployeeID == LocalStaticMethod(p.EmployeeID));
            try
            {
                query2 = (IQueryable<Employee>)RoundtripQuery(query, employees.AsQueryable());
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(LinqResource.QuerySerialization_MethodNotAccessible, "LocalStaticMethod", typeof(QuerySerializationTests)), expectedException.Message);
        }

        [TestMethod]
        public void TestArrayMethods()
        {
            // verify ArrayLength expressions are translated properly
            List<DataTests.Northwind.LTS.Category> categories = new List<DataTests.Northwind.LTS.Category>();
            categories.Add(new DataTests.Northwind.LTS.Category
            {
                CategoryID = 1,
                CategoryName = "Spatulas",
                Picture = new byte[] { 1, 2, 3 }
            });

            IQueryable<DataTests.Northwind.LTS.Category> query = new DataTests.Northwind.LTS.Category[0].AsQueryable().Where(p => p.Picture.Length > 0);
            IQueryable<DataTests.Northwind.LTS.Category> query2 = (IQueryable<DataTests.Northwind.LTS.Category>)RoundtripQuery(query, categories.AsQueryable());
            Assert.AreEqual(1, query2.Count());

            // verify ArrayIndex expressions are translated properly
            query = new DataTests.Northwind.LTS.Category[0].AsQueryable().Where(p => p.Picture[0] == 1 && p.Picture[1] == 2);
            query2 = (IQueryable<DataTests.Northwind.LTS.Category>)RoundtripQuery(query, categories.AsQueryable());
            Assert.AreEqual(1, query2.Count());

            // verify an expression as the index
            query = new DataTests.Northwind.LTS.Category[0].AsQueryable().Where(p => p.Picture[p.CategoryID] == 2);
            query2 = (IQueryable<DataTests.Northwind.LTS.Category>)RoundtripQuery(query, categories.AsQueryable());
            Assert.AreEqual(1, query2.Count());
        }

        [TestMethod]
        public void Bug479431_DecimalSerialization()
        {
            List<Product> prods = new List<Product>();
            prods.Add(new Product { ProductID = 1, ListPrice = 55.66M });
            IQueryable<Product> products = prods.AsQueryable();

            // the repro query, comparing '5' against a decimal member
            IQueryable<Product> query = products.Where(p => p.ListPrice == 5);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query);

            DomainServiceDescription northwindDescription = DomainServiceDescription.GetDescription(typeof(NorthwindDomainService));
            IQueryable<Product> query2 = (IQueryable<Product>)SystemLinqDynamic.QueryDeserializer.Deserialize(northwindDescription, products, TranslateQueryParts(queryParts));

            // verify that in the round-tripped query, the constant's decimal type
            // has been retained
            LambdaExpression lex = (LambdaExpression)((UnaryExpression)((MethodCallExpression)query2.Expression).Arguments[1]).Operand;
            ConstantExpression cex = (ConstantExpression)((BinaryExpression)lex.Body).Right;
            Assert.AreEqual(typeof(decimal), cex.Type);

            // now test with number containing a decimal point
            query = products.Where(p => p.ListPrice < 55.77M);
            query2 = (IQueryable<Product>)RoundtripQuery(query, products.AsQueryable());
            Assert.AreEqual(1, query2.Count());
        }

        [TestMethod]
        public void TestQuery_GuidSerialization()
        {
            Guid g1 = Guid.NewGuid();
            Guid g2 = Guid.NewGuid();
            List<Employee> employees = new List<Employee>();
            employees.Add(new Employee
            {
                EmployeeID = 1,
                rowguid = g1
            });
            employees.Add(new Employee
            {
                EmployeeID = 2,
                rowguid = g2
            });

            // the repro case
            Guid g3 = Guid.NewGuid();
            IQueryable<Employee> query = (IQueryable<Employee>)RoundtripQuery(employees.AsQueryable().Where(p => p.rowguid != g3), employees.AsQueryable());
            Assert.AreEqual(2, query.ToArray().Length);

            query = (IQueryable<Employee>)RoundtripQuery(employees.AsQueryable().Where(p => p.rowguid == g1), employees.AsQueryable());
            Assert.AreEqual(1, query.ToArray().Length);

            query = (IQueryable<Employee>)RoundtripQuery(employees.AsQueryable().Where(p => p.rowguid == g2), employees.AsQueryable());
            Assert.AreEqual(1, query.ToArray().Length);
        }

        [TestMethod]
        public void UnsupportedConstantTypes()
        {
            List<PurchaseOrder> pos = new List<PurchaseOrder>();
            pos.Add(new PurchaseOrder
            {
                PurchaseOrderID = 1,
                OrderDate = DateTime.Now
            });
            NotSupportedException expectedException = null;
            try
            {
                QuerySerializer.Serialize(pos.AsQueryable().Where(p => p.GetType() == typeof(PurchaseOrder)));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(string.Format(LinqResource.QuerySerialization_UnsupportedType, typeof(PurchaseOrder).GetType()), expectedException.Message);

            // while the Type can't be passed, verify that we can't use unsupported methods either
            try
            {
                RoundtripQuery(pos.AsQueryable().Where(p => p.GetType().Name == typeof(PurchaseOrder).Name), pos.AsQueryable());
            }
            catch (Exception e)
            {
                Assert.AreEqual("Methods on type 'Object' are not accessible (at index 1)", e.Message);
            }
        }

        [TestMethod]
        public void ConstantPredicate()
        {
            List<PurchaseOrder> pos = new List<PurchaseOrder>();
            pos.Add(new PurchaseOrder
            {
                PurchaseOrderID = 1,
                OrderDate = DateTime.Now
            });
            bool flag = true;
            IQueryable<PurchaseOrder> result = (IQueryable<PurchaseOrder>)RoundtripQuery(pos.AsQueryable().Where(p => flag), pos.AsQueryable());
            Assert.IsTrue(result.Expression.ToString().Contains("Where(Param_0 => True)"));
            Assert.AreEqual(1, result.ToArray().Count());

            result = (IQueryable<PurchaseOrder>)RoundtripQuery(pos.AsQueryable().Where(p => true), pos.AsQueryable());
            Assert.IsTrue(result.Expression.ToString().Contains("Where(Param_0 => True)"));
            Assert.AreEqual(1, result.ToArray().Count());

            Expression<Func<PurchaseOrder, bool>> expr = t => true;
            result = (IQueryable<PurchaseOrder>)RoundtripQuery(pos.AsQueryable().Where(expr), pos.AsQueryable());
            Assert.IsTrue(result.Expression.ToString().Contains("Where(Param_0 => True)"));
            Assert.AreEqual(1, result.ToArray().Count());
        }

        [TestMethod]
        public void BitwiseOperatorsNotSupported()
        {
            IQueryable<Product> prodQuery = Array.Empty<Product>().AsQueryable();

            // Bitwise NOT not supported
            NotSupportedException expectedException = null;
            try
            {
                QuerySerializer.Serialize(prodQuery.Where(p => ~p.ProductID != 0));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(LinqResource.QuerySerialization_BitwiseOperatorsNotSupported, expectedException.Message);

            // Bitwise AND not supported
            expectedException = null;
            try
            {
                QuerySerializer.Serialize(prodQuery.Where(p => (p.ProductID & 0x0F00) != 0));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(LinqResource.QuerySerialization_BitwiseOperatorsNotSupported, expectedException.Message);

            // Bitwise OR not supported
            expectedException = null;
            try
            {
                QuerySerializer.Serialize(prodQuery.Where(p => (p.ProductID | 0x0F00) != 0));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(LinqResource.QuerySerialization_BitwiseOperatorsNotSupported, expectedException.Message);

            // Bitwise XOR not supported
            expectedException = null;
            try
            {
                QuerySerializer.Serialize(prodQuery.Where(p => (p.ProductID ^ 0x0F00) != 0));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(LinqResource.QuerySerialization_BitwiseOperatorsNotSupported, expectedException.Message);

            // Bitwise Left Shift not supported
            expectedException = null;
            try
            {
                QuerySerializer.Serialize(prodQuery.Where(p => (p.ProductID << 3) != 0));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(LinqResource.QuerySerialization_BitwiseOperatorsNotSupported, expectedException.Message);

            // Bitwise Right Shift not supported
            expectedException = null;
            try
            {
                QuerySerializer.Serialize(prodQuery.Where(p => (p.ProductID >> 3) != 0));
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
            Assert.AreEqual(LinqResource.QuerySerialization_BitwiseOperatorsNotSupported, expectedException.Message);
        }

        [TestMethod]
        public void TestQuery_DateTimeSupport()
        {
            DomainServiceDescription northwindDescription = DomainServiceDescription.GetDescription(typeof(NorthwindDomainService));

            List<PurchaseOrder> poData = new List<PurchaseOrder>
            {
                new PurchaseOrder { PurchaseOrderID = 1, OrderDate = new DateTime(2000, 1, 2) },
                new PurchaseOrder { PurchaseOrderID = 2, OrderDate = new DateTime(2001, 2, 2) },
                new PurchaseOrder { PurchaseOrderID = 3, OrderDate = new DateTime(2002, 3, 2) },
                new PurchaseOrder { PurchaseOrderID = 4, OrderDate = new DateTime(2003, 4, 2) },
                new PurchaseOrder { PurchaseOrderID = 5, OrderDate = new DateTime(2004, 4, 2) },
                new PurchaseOrder { PurchaseOrderID = 6, OrderDate = new DateTime(2005, 4, 2) },
                new PurchaseOrder { PurchaseOrderID = 7, OrderDate = new DateTime(2006, 5, 2) },
                new PurchaseOrder { PurchaseOrderID = 8, OrderDate = new DateTime(2007, 6, 2) },
                new PurchaseOrder { PurchaseOrderID = 8, OrderDate = DateTime.Now.AddDays(1) }
            };

            // Test inline DateTime, verifying that the date's kind is preserved
            DateTime dt = new DateTime(2002, 3, 3);
            Expression<Func<PurchaseOrder, bool>> predicate = p => p.OrderDate < new DateTime(2002, 3, 3);
            IQueryable<PurchaseOrder> query = Array.Empty<PurchaseOrder>().AsQueryable().Where(predicate);
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query);
            Assert.IsTrue(queryParts[0].Expression.Contains(string.Format("DateTime({0},\"{1}\")", dt.Ticks, dt.Kind.ToString())));
            IQueryable<PurchaseOrder> resultQuery = (IQueryable<PurchaseOrder>)SystemLinqDynamic.QueryDeserializer.Deserialize(northwindDescription, poData.AsQueryable(), TranslateQueryParts(queryParts));
            Assert.IsTrue(resultQuery.ToString().Contains(string.Format("DateTime({0}, {1})", dt.Ticks, dt.Kind.ToString())));
            Assert.IsTrue(poData.AsQueryable().Where(predicate).OrderBy(p => p.PurchaseOrderID).SequenceEqual(resultQuery.OrderBy(p => p.PurchaseOrderID)));

            // Test member access of DateTime (funcletized local accessor)
            predicate = p => dt > p.OrderDate;
            query = Array.Empty<PurchaseOrder>().AsQueryable().Where(predicate);
            queryParts = QuerySerializer.Serialize(query);
            Assert.IsTrue(queryParts[0].Expression.Contains(string.Format("DateTime({0},\"{1}\")", dt.Ticks, dt.Kind.ToString())));
            resultQuery = (IQueryable<PurchaseOrder>)SystemLinqDynamic.QueryDeserializer.Deserialize(northwindDescription, poData.AsQueryable(), TranslateQueryParts(queryParts));
            Assert.IsTrue(poData.AsQueryable().Where(predicate).OrderBy(p => p.PurchaseOrderID).SequenceEqual(resultQuery.OrderBy(p => p.PurchaseOrderID)));

            // Verify DateTime.Now, DateTime.Today, etc. are treated as remote expressions
            predicate = p => p.OrderDate < DateTime.Now;
            query = Array.Empty<PurchaseOrder>().AsQueryable().Where(predicate);
            queryParts = QuerySerializer.Serialize(query);
            Assert.IsTrue(queryParts[0].Expression.Contains("DateTime.Now"));
            resultQuery = (IQueryable<PurchaseOrder>)SystemLinqDynamic.QueryDeserializer.Deserialize(northwindDescription, poData.AsQueryable(), TranslateQueryParts(queryParts));
            Assert.IsTrue(poData.AsQueryable().Where(predicate).OrderBy(p => p.PurchaseOrderID).SequenceEqual(resultQuery.OrderBy(p => p.PurchaseOrderID)));

            // Test DateTime with time components specified
            dt = new DateTime(2002, 3, 3, 4, 4, 4, 4);
            predicate = p => p.OrderDate < dt;
            query = Array.Empty<PurchaseOrder>().AsQueryable().Where(predicate);
            queryParts = QuerySerializer.Serialize(query);
            Assert.IsTrue(queryParts[0].Expression.Contains(string.Format("DateTime({0},\"{1}\")", dt.Ticks, dt.Kind.ToString())));
            resultQuery = (IQueryable<PurchaseOrder>)SystemLinqDynamic.QueryDeserializer.Deserialize(northwindDescription, poData.AsQueryable(), TranslateQueryParts(queryParts));
            Assert.IsTrue(resultQuery.ToString().Contains(string.Format("DateTime({0}, {1})", dt.Ticks, dt.Kind.ToString())));
            Assert.IsTrue(poData.AsQueryable().Where(predicate).OrderBy(p => p.PurchaseOrderID).SequenceEqual(resultQuery.OrderBy(p => p.PurchaseOrderID)));

            // Verify that the DayOfWeek enumeration can be used. It is evaluated locally to an int,
            // and promoted to the enum type on the server
            predicate = p => p.OrderDate.DayOfWeek == DayOfWeek.Sunday;
            query = Array.Empty<PurchaseOrder>().AsQueryable().Where(predicate);
            queryParts = QuerySerializer.Serialize(query);
            Assert.IsTrue(queryParts[0].Expression.Contains("OrderDate.DayOfWeek==0"));
            resultQuery = (IQueryable<PurchaseOrder>)SystemLinqDynamic.QueryDeserializer.Deserialize(northwindDescription, poData.AsQueryable(), TranslateQueryParts(queryParts));
            Assert.IsTrue(resultQuery.Count() > 0);
            Assert.IsTrue(poData.AsQueryable().Where(predicate).OrderBy(p => p.PurchaseOrderID).SequenceEqual(resultQuery.OrderBy(p => p.PurchaseOrderID)));

            // TODO : currently DateTime constructions involving non-local expressions
            // are not supported
            predicate = p => p.OrderDate < new DateTime(p.OrderDate.Year, p.OrderDate.Month, 3);
            query = Array.Empty<PurchaseOrder>().AsQueryable().Where(predicate);
            NotSupportedException expectedException = null;
            try
            {
                queryParts = QuerySerializer.Serialize(query);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(LinqResource.QuerySerialization_NewExpressionsNotSupported, expectedException.Message);
        }
        //CDB commented out until compilable
        ///// <summary>
        ///// Test various member access expressions, such as collection Count,
        ///// string length, etc.
        ///// </summary>
        //[TestMethod]
        //public void TestMemberAccess()
        //{
        //    Cities.CityData cityData = new Cities.CityData();

        //    IQueryable<Cities.State> statesQuery =
        //                                                              from s in cityData.States.AsQueryable()
        //                                                              where s.Counties.Count > 2 && s.FullName.Length < 6
        //                                                              select s;

        //    DomainServiceDescription cityDescription = DomainServiceDescription.GetDescription(typeof(Cities.CityDomainService));
        //    IQueryable<Cities.State> statesQuery2 = (IQueryable<Cities.State>)RoundtripQuery(cityDescription, statesQuery, cityData.States.AsQueryable());
        //    Assert.IsTrue(statesQuery.SequenceEqual(statesQuery2));
        //}

        private bool LocalMethod(string s)
        {
            return true;
        }

        private int LocalMethod(int a)
        {
            return a;
        }

        private static int LocalStaticMethod(int a)
        {
            return a;
        }

        [TestMethod]
        public void TestUnsupportedSequenceMethods()
        {
            NotSupportedException expectedException = null;
            IQueryable<Product> query = (from p in Array.Empty<Product>().AsQueryable() select p).Reverse();
            try
            {
                List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
            }
            Assert.AreEqual(string.Format(LinqResource.QuerySerialization_UnsupportedQueryOperator, "Reverse"), expectedException.Message);
            expectedException = null;
        }

        /// <summary>
        /// Previously we had an issue related to unintended local evaluation of queries like
        /// query.Take(2), since both PurchaseOrdersQuery and Take(2) are local,
        /// so the entire expression was evaluated locally.
        /// </summary>
        [TestMethod]
        public void TestQuery_VerifyQueryNotEvaluatedLocally()
        {
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(Array.Empty<PurchaseOrder>().AsQueryable().Take(2));
            Assert.AreEqual(1, queryParts.Count);

            // make sure that local IQueryables other than the root are evaluated locally
            int[] ints = new int[] { 1, 2 };
            IQueryable<int> intsQueryable = ints.AsQueryable();
            queryParts = QuerySerializer.Serialize(Array.Empty<PurchaseOrder>().AsQueryable().Where(p => p.PurchaseOrderID > intsQueryable.Count()).Take(intsQueryable.Count()));
            Assert.AreEqual(2, queryParts.Count);

            queryParts = QuerySerializer.Serialize(ProdQuery(5).Where(p => p.PurchaseOrderID > intsQueryable.Count()).Take(intsQueryable.Count()));
            Assert.AreEqual(2, queryParts.Count);

            // verify query with no query operators is handled properly
            queryParts = QuerySerializer.Serialize(Array.Empty<PurchaseOrder>().AsQueryable());
            Assert.AreEqual(0, queryParts.Count);
        }

        private IQueryable<PurchaseOrder> ProdQuery(int x)
        {
            return Array.Empty<PurchaseOrder>().AsQueryable();
        }

        [TestMethod]
        public void TestQuery_UnaryExpressions()
        {
            // Test numeric negation
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where -p.ListPrice < -1000
                     select p;
            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));

            // Test logical negation
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where !(p.ListPrice < 1000)
                     select p;
            query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_ConditionalOperator()
        {
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Name == (p.Color == null ? "FooBar" : "Yellow")
                     select p;
            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));

            // test non-locals as true/false expressions
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.ProductID > (p.Color == "Yellow" ? p.Name.Length + 2 : p.Name.Length - 3)
                     select p;
            query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_StringMethods()
        {
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color != null && p.Color.StartsWith("Yell") && p.Name.Length < 23
                     select p;

            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));

            // verify string concat works
            query1 = BaselineTestData.Products.AsQueryable().OrderBy(p => p.Class + "X");
            query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));

            // test case insensitive string comparisons
            query1 = BaselineTestData.Products.AsQueryable().Where(p => string.Compare(p.Color, "black", StringComparison.OrdinalIgnoreCase) == 0);
            query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            List<Product> results = query2.ToList();
            Assert.IsTrue(results.Any(p => p.Color == "Black"));  // verify the search was case insensitive
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_TestRemoteStaticMethods()
        {
            IQueryable<Product> query1;
            // Call Math.Round static with a non-local expression
            // Call Convert static with a non-local expression
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where (Math.Round(p.ListPrice) - 55.0M) > 40 && Convert.ToInt32(p.ListPrice) < 1000
                     select p;

            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_TestRemoteInstanceMethods()
        {
            IQueryable<Product> query1;
            // Call string.Contains with a non-local expression
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color != null && p.Name.Contains(p.Color)
                     select p;

            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_TestBinaryExpressionAssociation()
        {
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color != null && p.Color.StartsWith("Yell") || p.Name.Length < 12
                     select p;
            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));

            // Same query as above with expressions associated differently
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color != null && (p.Color.StartsWith("Yell") || p.Name.Length < 12)
                     select p;
            query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_TestFrameworkConstants()
        {
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Name != string.Empty && p.ProductID < int.MaxValue
                     select p;

            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        [TestMethod]
        public void TestQuery_QueryOperatorSupport_Select()
        {
            // empty select is supported
            IQueryable query;
            query = from p in Array.Empty<Product>().AsQueryable()
                    select p;

            query = (IQueryable<Product>)RoundtripQuery(query, BaselineTestData.Products.AsQueryable());

            Assert.AreEqual(BaselineTestData.Products.Count(), query.Cast<object>().Count());

            // projections are not supported
            query = from p in Array.Empty<Product>().AsQueryable()
                    select new
                    {
                        p.Name,
                        p.Color
                    };
            Exception expectedException = null;
            try
            {
                List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
                Assert.AreEqual(String.Format(LinqResource.QuerySerialization_ProjectionsNotSupported), e.Message);
            }
            Assert.IsNotNull(expectedException);

            // Verify expected exception for unsupported SelectMany
            query = Array.Empty<PurchaseOrder>().AsQueryable().SelectMany(p => p.PurchaseOrderDetails);
            expectedException = null;
            try
            {
                QuerySerializer.Serialize(query);
            }
            catch (NotSupportedException e)
            {
                expectedException = e;
                Assert.AreEqual(String.Format(LinqResource.QuerySerialization_UnsupportedQueryOperator, "SelectMany"), e.Message);
            }
            Assert.IsNotNull(expectedException);
        }

        [TestMethod]
        public void TestQuery_MultipleDuplicateQueryOps()
        {
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color == "Yellow"
                     orderby p.ListPrice
                     where p.Weight > 20
                     orderby p.Style
                     select p;

            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query1);
            Assert.AreEqual(4, queryParts.Count);
        }

        [TestMethod]
        public void TestQuery_QueryOperatorSupport_Ordering()
        {
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color == "Yellow"
                     orderby p.ListPrice ascending, p.Style descending, p.Weight
                     select p;

            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        private readonly string TestColor = "Yellow";

        /// <summary>
        /// This method is used from the TestQuery_LocalReferences test, to ensure
        /// that local method calls can be used when building queries.
        /// </summary>
        /// <param name="value">The Culture-Invariant string representing the decimal value to return.</param>
        /// <returns>The decimal value.</returns>
        private decimal GetDecimal(string value)
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestQuery_LocalReferences()
        {
            string testName = "Mortimer";
            IQueryable<Product> query1;
            query1 = from p in BaselineTestData.Products.AsQueryable()
                     where p.Color == TestColor && // test field reference
                                                                             p.ListPrice < GetDecimal("555.35") && // test method call
                                                                             p.Name != testName
                     orderby p.ListPrice ascending, p.Style descending, p.Weight
                     select p;

            IQueryable<Product> query2 = (IQueryable<Product>)RoundtripQuery(query1, BaselineTestData.Products.AsQueryable());
            Assert.IsTrue(query1.SequenceEqual(query2));
        }

        private IQueryable RoundtripQuery(IQueryable query, IQueryable data)
        {
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query);

            DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(typeof(NorthwindDomainService));
            return SystemLinqDynamic.QueryDeserializer.Deserialize(domainServiceDescription, data, TranslateQueryParts(queryParts));
        }

        private IQueryable RoundtripQuery(DomainServiceDescription domainServiceDescription, IQueryable query, IQueryable data)
        {
            List<ServiceQueryPart> queryParts = QuerySerializer.Serialize(query);

            return SystemLinqDynamic.QueryDeserializer.Deserialize(domainServiceDescription, data, TranslateQueryParts(queryParts));
        }

        private List<WebRia::OpenRiaServices.DomainServices.Hosting.ServiceQueryPart> TranslateQueryParts(List<ServiceQueryPart> queryParts)
        {
            List<WebRia::OpenRiaServices.DomainServices.Hosting.ServiceQueryPart> returnParts = new List<WebRia::OpenRiaServices.DomainServices.Hosting.ServiceQueryPart>();
            foreach (ServiceQueryPart part in queryParts)
            {
                WebRia::OpenRiaServices.DomainServices.Hosting.ServiceQueryPart newPart = new WebRia::OpenRiaServices.DomainServices.Hosting.ServiceQueryPart(part.QueryOperator, part.Expression);
                returnParts.Add(newPart);
            }
            return returnParts;
        }
    }

    /// <summary>
    /// Runs all the tests from the base class, but using the German culture.
    /// </summary>
    [TestClass]
    public class QuerySerializationTests_Globalization : QuerySerializationTests
    {
        private CultureInfo _defaultCulture;

        [TestInitialize]
        public void SetUp()
        {
            _defaultCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
        }

        [TestCleanup]
        public void TearDown()
        {
            Thread.CurrentThread.CurrentUICulture = _defaultCulture;
        }
    }

    public class EmployeeWithCharProperty
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public char GenderAsChar
        {
            get;
            set;
        }
    }

    public class EmployeeWithReservedNames
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public bool @true
        {
            get;
            set;
        }

        public bool iif
        {
            get;
            set;
        }
    }

    [Flags]
    public enum QuerySerialisationEnum
    {
        None = 0x00,
        A = 0x01,
        B = 0x02,
    }

    public class EntityWithEnums
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public QuerySerialisationEnum EnumProp1
        {
            get;
            set;
        }

        public QuerySerialisationEnum EnumProp2
        {
            get;
            set;
        }
    }
}
