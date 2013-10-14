extern alias SSmDsClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Client;
using System.Xml.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;
    using TypeUtility = SSmDsClient::OpenRiaServices.DomainServices.TypeUtility;

    [TestClass]
    public class InvokeOperationTests : UnitTestBase
    {
        /// <summary>
        /// Verify that roundtripping a double results in no data loss.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        [WorkItem(196039)]  // CSDMain
        public void InvokeOperation_RoundtripDouble()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            Double d = Double.Parse("9.2233720368547758E+18");

            InvokeOperation<double> invoke = ctxt.RoundtripDouble(d);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                double result = invoke.Value;
                Assert.AreEqual(d, result);
            });
            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that synchronous method level validation occurs immediately for
        /// invoke operations.
        [TestMethod]
        [Asynchronous]
        public void ClientValidation_InvokeOperation()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            ExceptionHelper.ExpectValidationException(delegate
            {
                ctxt.InvokeOperationWithParamValidation(-3, "ABC", new CityWithCacheData());
            }, "The field a must be between 0 and 10.", typeof(RangeAttribute), -3);

            EnqueueTestComplete();
        }

        /// <summary>
        /// Verify that server side validation errors are propagated back to the
        /// client for InvokeOperations.
        /// </summary>
        [TestMethod]
        [Asynchronous]
        public void ServerValidation_InvokeOperation()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // Validate using an action so we can assert state for each of the 3 different
            // completion patterns; callback, event, and polling
            Action<InvokeOperation<bool>> validate = (io) =>
            {
                Assert.IsNotNull(io.Error);
                Assert.AreEqual(typeof(DomainOperationException), io.Error.GetType());
                Assert.AreEqual(OperationErrorStatus.ValidationFailed, ((DomainOperationException)io.Error).Status);
                Assert.AreEqual(string.Format(Resource.DomainContext_InvokeOperationFailed_Validation, "InvokeOperationWithParamValidation"), io.Error.Message);
                Assert.AreEqual(1, io.ValidationErrors.Count(),
                    "There should be 1 validation error.");
                ValidationResult error = io.ValidationErrors.Single();
                Assert.AreEqual("Server validation exception thrown!", error.ErrorMessage);
                io.MarkErrorAsHandled();
            };

            InvokeOperation<bool> op = ctxt.InvokeOperationWithParamValidation(5, "ex", new CityWithCacheData(), validate, null);
            op.Completed += (sender, e) =>
            {
                validate((InvokeOperation<bool>)sender);
            };

            EnqueueConditional(delegate
            {
                return op.IsComplete;
            });
            EnqueueCallback(delegate
            {
                validate(op);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Bug706128_InvokeOperationCancellation()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            InvokeOperation invoke = ctxt.ReturnsString_Online("Ruby", TestHelperMethods.DefaultOperationAction, null);

            invoke.Cancel();

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Value);
                Assert.IsTrue(invoke.IsCanceled);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void Bug706146_ValidationErrorsInitialization()
        {
            TestProvider_Scenarios ctxt = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            InvokeOperation invoke = ctxt.ThrowOnlineException(TestHelperMethods.DefaultOperationAction, null);

            // verify that all operation properties can be accessed before completion
            Assert.AreEqual(0, invoke.ValidationErrors.Count());
            Assert.IsNull(invoke.Value);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Value);
                Assert.IsNotNull(invoke.Error);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_TestPropertyChangeNotifications()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            InvokeOperation invoke = null;
            List<string> notifications = new List<string>();

            // verify with userstate
            EnqueueCallback(delegate
            {
                invoke = cities.Echo("hello", TestHelperMethods.DefaultOperationAction, "my user state");

                ((System.ComponentModel.INotifyPropertyChanged)invoke).PropertyChanged += (s, e) =>
                {
                    notifications.Add(e.PropertyName);
                };
            });
            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsFalse(invoke.HasError);
                Assert.AreEqual("Echo: hello", invoke.Value);

                Assert.AreEqual(3, notifications.Count);
                Assert.AreEqual("IsComplete", notifications[0]);
                Assert.AreEqual("CanCancel", notifications[1]);
                Assert.AreEqual("Value", notifications[2]);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_Basic()
        {
            CityDomainContext cities = new CityDomainContext(TestURIs.Cities);
            InvokeOperation invoke = null;

            // verify with userstate
            EnqueueCallback(delegate
            {
                invoke = cities.Echo("hello", TestHelperMethods.DefaultOperationAction, "my user state");
            });
            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Error);
                Assert.AreEqual("Echo", invoke.OperationName);
                Assert.AreEqual(1, invoke.Parameters.Count);
                Assert.AreSame("hello", invoke.Parameters["msg"]);
                Assert.AreEqual("Echo: hello", invoke.Value);
                Assert.AreEqual("my user state", invoke.UserState);
            });

            // verify without userstate
            EnqueueCallback(delegate
            {
                invoke = cities.Echo("hello");
            });
            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.AreEqual(invoke.Value, "Echo: hello");
                Assert.IsNull(invoke.UserState);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_VoidInputAndOutput()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            InvokeOperation invoke = provider.VoidMethod(TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Value);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperation_ReturnsXElement()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            string elementName = "Foo", elementValue = "Bar";

            var xml = XElement.Parse(string.Format("<{0} xmlns=\"foo\">{1}</{0}>", elementName, elementValue));
            InvokeOperation invoke = provider.ReturnsXElement(xml, TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                object returnValue = invoke.Value;
                Assert.IsNotNull(invoke.Value);
                Assert.AreEqual(typeof(XElement), returnValue.GetType());
                Assert.AreEqual(xml.ToString(), returnValue.ToString());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_ReturnsEntity()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType mt = new MixedType();
            InvokeOperation invoke = provider.ReturnsEntity_Online(mt, "MixedType_Other", TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(invoke);

                mt = invoke.Value as MixedType;
                Assert.IsNotNull(mt);
                Assert.AreEqual("MixedType_Other", mt.ID);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_ReturnsEntityCollection()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            InvokeOperation invoke = provider.ReturnsEntityCollection_Online(3, TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(invoke);

                IEnumerable<MixedType> mts = invoke.Value as IEnumerable<MixedType>;
                Assert.AreEqual(3, mts.Count());
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_ReturnsIDictionary()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (var i in new[] { 1, 2, 3 })
            {
                dictionary.Add(i.ToString(), i);
            }

            InvokeOperation invoke = provider.ReturnsDictionary(dictionary, TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                object returnValue = invoke.Value;
                Assert.IsNotNull(invoke.Value);

                Dictionary<string, int> returnValueAsDictonary = returnValue as Dictionary<string, int>;
                Assert.IsNotNull(returnValueAsDictonary);

                foreach (var kvp in dictionary)
                {
                    Assert.IsTrue(returnValueAsDictonary.ContainsKey(kvp.Key));
                    Assert.AreEqual(kvp.Value, returnValueAsDictonary[kvp.Key]);
                }
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_VariousParameterTypes()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            InvokeOperation invoke = provider.VariousParameterTypes("hello", 2, true, TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                // 8 because "hello".Length (5) + 2 + true (1) = 8.
                Assert.AreEqual("VariousParameterTypes", invoke.OperationName);
                Assert.AreEqual(3, invoke.Parameters.Count);
                Assert.AreEqual("hello", invoke.Parameters["str"]);
                Assert.AreEqual(2, invoke.Parameters["integer"]);
                Assert.AreEqual(true, invoke.Parameters["boolean"]);
                Assert.AreEqual(8, (int)invoke.Value);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_VariousParameterTypes_WithInvalidParameter()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            ValidationException expectedException = null;
            try
            {
                InvokeOperation invoke = provider.VariousParameterTypes(null, 2, true, TestHelperMethods.DefaultOperationAction, null);
            }
            catch (ValidationException e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            Assert.AreEqual(typeof(RequiredAttribute), expectedException.ValidationAttribute.GetType());
            Assert.AreEqual("The str field is required.", expectedException.Message);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_Exceptions()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            InvokeOperation invoke = provider.ThrowOnlineException(TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Value);
                Assert.IsNotNull(invoke.Error);
                Assert.IsInstanceOfType(invoke.Error, typeof(DomainOperationException));
                DomainOperationException dpe = invoke.Error as DomainOperationException;
                Assert.AreEqual(string.Format(Resource.DomainContext_InvokeOperationFailed, "ThrowOnlineException", "Invalid operation."), dpe.Message);
                Assert.AreEqual(OperationErrorStatus.NotSupported, dpe.Status);
                Assert.AreEqual(400, dpe.ErrorCode);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_AuthenticationTest()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            InvokeOperation invoke = provider.MethodRequiresAuthentication(TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNull(invoke.Value);
                Assert.IsNotNull(invoke.Error);
                Assert.IsInstanceOfType(invoke.Error, typeof(DomainOperationException));
                DomainOperationException dpe = invoke.Error as DomainOperationException;
                Assert.AreEqual(string.Format(Resource.DomainContext_InvokeOperationFailed, "MethodRequiresAuthentication", "Access to operation 'MethodRequiresAuthentication' was denied."), dpe.Message);
                Assert.AreEqual(OperationErrorStatus.Unauthorized, dpe.Status);
                Assert.AreEqual(401, dpe.ErrorCode);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_EntityParameter()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            A inputA = new A()
            {
                BID1 = 1,
                RequiredString = "Foo"
            };

            InvokeOperation<int> invoke = provider.IncrementBid1ForA(inputA, TestHelperMethods.DefaultOperationAction, null);

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                Assert.IsNotNull(invoke.Value);
                Assert.AreEqual(inputA.BID1 + 1, invoke.Value);
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        public void InvokeOperation_EntityParameter_Invalid()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            A inputA = new A()
            {
                BID1 = 1
            };

            ValidationException expectedException = null;
            try
            {
                var invoke = provider.IncrementBid1ForABy(inputA, 4);
            }
            catch (ValidationException e)
            {
                expectedException = e;
            }

            Assert.IsNotNull(expectedException);
            Assert.AreEqual(typeof(RangeAttribute), expectedException.ValidationAttribute.GetType());
            Assert.AreEqual(4, expectedException.Value);
            Assert.AreEqual("The field delta must be between 5 and 10.", expectedException.Message);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(211819)]
        [Description("Checks that Invoke Operations with IEnumerable of Entities as parameters work properly.")]
        public void InvokeOperation_IEnumerableParameters()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            TestEntityForInvokeOperations[] list = new TestEntityForInvokeOperations[]
            {
                new TestEntityForInvokeOperations(){ Key = 1, StrProp = "Str1", CTProp = new TestCT(){ CTProp1 = 11, CTProp2 = "CtStr1"}},
                new TestEntityForInvokeOperations(){ Key = 2, StrProp = "Str1", CTProp = new TestCT(){ CTProp1 = 22, CTProp2 = "CtStr2"}},
                new TestEntityForInvokeOperations(){ Key = 3, StrProp = "Str1", CTProp = new TestCT(){ CTProp1 = 33, CTProp2 = "CtStr3"}}
            };

            InvokeOperation<IEnumerable<TestEntityForInvokeOperations>> invoke = ctxt.InvokeOpWithIEnumerableParam(list.AsEnumerable());
            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                IEnumerable<TestEntityForInvokeOperations> result = invoke.Value;
                Assert.IsNotNull(result);
                Assert.AreEqual(result.Count(), 3);

                TestEntityForInvokeOperations[] resultArray = result.ToArray();
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(list[i].Key, resultArray[i].Key);
                    Assert.AreEqual(list[i].StrProp, resultArray[i].StrProp);
                    Assert.AreEqual(list[i].CTProp.CTProp1, resultArray[i].CTProp.CTProp1);
                    Assert.AreEqual(list[i].CTProp.CTProp2, resultArray[i].CTProp.CTProp2);
                }
            });
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(211819)]
        [Description("Checks that Invoke Operations with IEnumerable of Entities as parameters and no side effects work properly.")]
        public void InvokeOperationWithNoSideEffects_IEnumerableParameters()
        {
            TestDomainServices.TestProvider_Scenarios ctxt = new TestDomainServices.TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            TestEntityForInvokeOperations[] list = new TestEntityForInvokeOperations[]
            {
                new TestEntityForInvokeOperations(){ Key = 1, StrProp = "Str1", CTProp = new TestCT(){ CTProp1 = 11, CTProp2 = "CtStr1"}},
                new TestEntityForInvokeOperations(){ Key = 2, StrProp = "Str1", CTProp = new TestCT(){ CTProp1 = 22, CTProp2 = "CtStr2"}},
                new TestEntityForInvokeOperations(){ Key = 3, StrProp = "Str1", CTProp = new TestCT(){ CTProp1 = 33, CTProp2 = "CtStr3"}}
            };

            InvokeOperation<IEnumerable<TestEntityForInvokeOperations>> invoke = ctxt.InvokeOpWithIEnumerableParamAndNoSideEffects(list.AsEnumerable());
            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                IEnumerable<TestEntityForInvokeOperations> result = invoke.Value;
                Assert.IsNotNull(result);
                Assert.AreEqual(result.Count(), 3);

                TestEntityForInvokeOperations[] resultArray = result.ToArray();
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(list[i].Key, resultArray[i].Key);
                    Assert.AreEqual(list[i].StrProp, resultArray[i].StrProp);
                    Assert.AreEqual(list[i].CTProp.CTProp1, resultArray[i].CTProp.CTProp1);
                    Assert.AreEqual(list[i].CTProp.CTProp2, resultArray[i].CTProp.CTProp2);
                }
            });
            EnqueueTestComplete();
        }

        #region exhaustive supported types tests
        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation parameters using supported primitive types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperationParams_TestPrimitive()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;
            InvokeOperation<bool> invoke = null;
            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(delegate
            {
                changedObj = provider.MixedTypes.Single(t => (t.ID == "MixedType_Max"));
                valuesObj = provider.MixedTypes.Single(t => (t.ID == "MixedType_Other"));

                // invoke invoke operation
                invoke = provider.TestPrimitive_Online(changedObj, valuesObj.BooleanProp, valuesObj.ByteProp, valuesObj.SByteProp,
                    valuesObj.Int16Prop, valuesObj.UInt16Prop, valuesObj.Int32Prop, valuesObj.UInt32Prop, valuesObj.Int64Prop,
                    valuesObj.UInt64Prop, valuesObj.CharProp, valuesObj.DoubleProp, valuesObj.SingleProp, TestHelperMethods.DefaultOperationAction, null);
            });

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                // verify invocation completed succesfully
                Assert.IsNull(invoke.Error);
                Assert.IsFalse(invoke.ValidationErrors.Any());

                Assert.IsTrue(invoke.Value);

                // verify the entity we passed as the invoke operation param is not changed on the client
                Assert.AreEqual(255, changedObj.ByteProp);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation parameters using supported predefined types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperationParams_TestPredefined()
        {
            InvokeOperation<bool> invoke = null;
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;

            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(delegate
            {
                changedObj = provider.MixedTypes.Single(t => (t.ID == "MixedType_Max"));
                valuesObj = provider.MixedTypes.Single(t => (t.ID == "MixedType_Other"));

                // invoke invoke operation
                invoke = provider.TestPredefined_Online(changedObj, valuesObj.StringProp, valuesObj.DecimalProp,
                    valuesObj.DateTimeProp, valuesObj.TimeSpanProp, valuesObj.StringsProp, valuesObj.UriProp,
                    valuesObj.GuidProp, valuesObj.BinaryProp, valuesObj.XElementProp, valuesObj.ByteArrayProp,
                    valuesObj.EnumProp, valuesObj.GuidsProp, valuesObj.UInt64sProp, valuesObj.DateTimeOffsetProp, TestHelperMethods.DefaultOperationAction, null);
            });

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                // verify invocation completed succesfully
                Assert.IsNull(invoke.Error);
                Assert.IsFalse(invoke.ValidationErrors.Any());

                Assert.IsTrue(invoke.Value);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation parameters using supported nullable types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperationParams_TestNullable()
        {
            InvokeOperation<bool> invoke = null;
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            MixedType changedObj = null;
            MixedType valuesObj = null;

            LoadOperation lo = provider.Load(provider.GetMixedTypesQuery(), false);

            // wait for Load to complete, then invoke some domain methods
            EnqueueConditional(() => lo.IsComplete);

            EnqueueCallback(delegate
            {
                changedObj = provider.MixedTypes.Single(t => (t.ID == "MixedType_Max"));
                valuesObj = provider.MixedTypes.Single(t => (t.ID == "MixedType_Other"));

                // invoke invoke operation
                invoke = provider.TestNullable_Online(changedObj, valuesObj.NullableBooleanProp, valuesObj.NullableByteProp, valuesObj.NullableSByteProp,
                    valuesObj.NullableInt16Prop, valuesObj.NullableUInt16Prop, valuesObj.NullableInt32Prop, valuesObj.NullableUInt32Prop,
                    valuesObj.NullableInt64Prop, valuesObj.NullableUInt64Prop, valuesObj.NullableCharProp, valuesObj.NullableDoubleProp,
                    valuesObj.NullableSingleProp, valuesObj.NullableDecimalProp, valuesObj.NullableDateTimeProp,
                    valuesObj.NullableTimeSpanProp, valuesObj.NullableGuidProp, valuesObj.NullableEnumProp,
                    valuesObj.NullableTimeSpanListProp, valuesObj.NullableDateTimeOffsetProp, TestHelperMethods.DefaultOperationAction, null);
            });

            EnqueueConditional(() => invoke.IsComplete);
            EnqueueCallback(delegate
            {
                // verify invocation completed succesfully
                Assert.IsNull(invoke.Error, "InvokeEventArgs.Error should be null");
                Assert.IsFalse(invoke.ValidationErrors.Any());

                Assert.IsTrue(invoke.Value);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation using different return types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperationReturn_TestPrimitive()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // execute tests with different types
            this.VerifyOnlineMethodReturn<Boolean>(provider, provider.ReturnsBoolean_Online, true);
            this.VerifyOnlineMethodReturn<Byte>(provider, provider.ReturnsByte_Online, 123);
            this.VerifyOnlineMethodReturn<SByte>(provider, provider.ReturnsSByte_Online, 123);
            this.VerifyOnlineMethodReturn<Int16>(provider, provider.ReturnsInt16_Online, 123);
            this.VerifyOnlineMethodReturn<UInt16>(provider, provider.ReturnsUInt16_Online, 123);
            this.VerifyOnlineMethodReturn<Int32>(provider, provider.ReturnsInt32_Online, 123);
            this.VerifyOnlineMethodReturn<UInt32>(provider, provider.ReturnsUInt32_Online, 123);
            this.VerifyOnlineMethodReturn<Int64>(provider, provider.ReturnsInt64_Online, 123);
            this.VerifyOnlineMethodReturn<UInt64>(provider, provider.ReturnsUInt64_Online, 123);
            this.VerifyOnlineMethodReturn<Double>(provider, provider.ReturnsDouble_Online, 123.123);
            this.VerifyOnlineMethodReturn<Single>(provider, provider.ReturnsSingle_Online, (Single)123.123);
            this.VerifyOnlineMethodReturn<Char>(provider, provider.ReturnsChar_Online, (char)123);
            this.VerifyOnlineMethodReturn<TimeSpan>(provider, provider.ReturnsTimeSpan_Online, TimeSpan.FromSeconds(123));
            this.VerifyOnlineMethodReturn<IEnumerable<string>>(provider, provider.ReturnsStrings_Online, new string[] { "hello", "world" }.ToList());
            this.VerifyOnlineMethodReturn<DateTime[]>(provider, provider.ReturnsDateTimes_Online, new DateTime[] { new DateTime(2009, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2009, 1, 6, 0, 5, 0, DateTimeKind.Utc) });
            this.VerifyOnlineMethodReturn<DateTimeOffset[]>(provider, provider.ReturnsDateTimeOffsets_Online, new DateTimeOffset[] { new DateTimeOffset(new DateTime(2009, 1, 1, 0, 0, 0), new TimeSpan(3, 0, 0)),  new DateTimeOffset(new DateTime(2009, 1, 6, 0, 5, 0), new TimeSpan(-6, 10, 0)) });
            this.VerifyOnlineMethodReturn<IEnumerable<TimeSpan>>(provider, provider.ReturnsTimeSpans_Online, new TimeSpan[] { TimeSpan.FromSeconds(123), TimeSpan.FromMinutes(3) }.ToList());
            this.VerifyOnlineMethodReturn<Uri>(provider, provider.ReturnsUri_Online, new Uri("http://localhost"));
            this.VerifyOnlineMethodReturn<Char>(provider, provider.ReturnsChar_Online, (char)0);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation using a big string")]
        public void InvokeOperationReturn_TestBigString()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            string str = new string('x', 10000);
            this.VerifyOnlineMethodReturn<string>(provider, provider.ReturnsString_Online, str);
            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation using different return types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperationReturn_TestPredefined()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // execute tests with different types
            this.VerifyOnlineMethodReturn<String>(provider, provider.ReturnsString_Online, "Hello There!");
            this.VerifyOnlineMethodReturn<Decimal>(provider, provider.ReturnsDecimal_Online, 123);
            this.VerifyOnlineMethodReturn<Guid>(provider, provider.ReturnsGuid_Online, new Guid("12345678-1234-1234-1234-123456789012"));
            this.VerifyOnlineMethodReturn<DateTime>(provider, provider.ReturnsDateTime_Online, new DateTime(2009, 9, 10));
            this.VerifyOnlineMethodReturn<DateTimeOffset>(provider, provider.ReturnsDateTimeOffset_Online, new DateTimeOffset(new DateTime(2009, 1, 1, 0, 0, 0), new TimeSpan(-3, 0, 0)));
            this.VerifyOnlineMethodReturn<byte[]>(provider, provider.ReturnsByteArray_Online, new byte[] { 111, 222 });
            this.VerifyOnlineMethodReturn<byte[]>(provider, provider.ReturnsBinary_Online, new byte[] { 111, 222 });
            this.VerifyOnlineMethodReturn<TestEnum>(provider, provider.ReturnsEnum_Online, TestEnum.Value1);
            this.VerifyOnlineMethodReturn<TestEnum>(provider, provider.ReturnsEnum_Online, TestEnum.Value1 | TestEnum.Value2);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation using different return types")]
        [FullTrustTest] // ISerializable types cannot be deserialized in medium trust.
        public void InvokeOperationReturn_TestNullable()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // execute tests with different types
            this.VerifyOnlineMethodReturn<Boolean?>(provider, provider.ReturnsNullableBoolean_Online, true);
            this.VerifyOnlineMethodReturn<Byte?>(provider, provider.ReturnsNullableByte_Online, 123);
            this.VerifyOnlineMethodReturn<SByte?>(provider, provider.ReturnsNullableSByte_Online, 123);
            this.VerifyOnlineMethodReturn<Int16?>(provider, provider.ReturnsNullableInt16_Online, 123);
            this.VerifyOnlineMethodReturn<UInt16?>(provider, provider.ReturnsNullableUInt16_Online, 123);
            this.VerifyOnlineMethodReturn<Int32?>(provider, provider.ReturnsNullableInt32_Online, 123);
            this.VerifyOnlineMethodReturn<UInt32?>(provider, provider.ReturnsNullableUInt32_Online, 123);
            this.VerifyOnlineMethodReturn<Int64?>(provider, provider.ReturnsNullableInt64_Online, 123);
            this.VerifyOnlineMethodReturn<UInt64?>(provider, provider.ReturnsNullableUInt64_Online, 123);
            this.VerifyOnlineMethodReturn<Double?>(provider, provider.ReturnsNullableDouble_Online, 123.123);
            this.VerifyOnlineMethodReturn<Single?>(provider, provider.ReturnsNullableSingle_Online, (Single)123.123);
            this.VerifyOnlineMethodReturn<Char?>(provider, provider.ReturnsNullableChar_Online, (Char)123);
            this.VerifyOnlineMethodReturn<Decimal?>(provider, provider.ReturnsNullableDecimal_Online, 123);
            this.VerifyOnlineMethodReturn<Guid?>(provider, provider.ReturnsNullableGuid_Online, new Guid("12345678-1234-1234-1234-123456789012"));
            this.VerifyOnlineMethodReturn<DateTime?>(provider, provider.ReturnsNullableDateTime_Online, new DateTime(2009, 9, 10));
            this.VerifyOnlineMethodReturn<DateTimeOffset?>(provider, provider.ReturnsNullableDateTimeOffset_Online, new DateTimeOffset(new DateTime(2009, 1, 1, 0, 0, 0), new TimeSpan(3, 0, 0)));
            this.VerifyOnlineMethodReturn<TestEnum?>(provider, provider.ReturnsNullableEnum_Online, TestEnum.Value1);
            this.VerifyOnlineMethodReturn<TestEnum?>(provider, provider.ReturnsNullableEnum_Online, TestEnum.Value1 | TestEnum.Value2);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation using different nullable return types and returning null values")]
        public void InvokeOperationReturn_TestNullable_WithNull()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);

            // execute tests with different types
            this.VerifyOnlineMethodReturn<Boolean?>(provider, provider.ReturnsNullableBoolean_Online, null);
            this.VerifyOnlineMethodReturn<Byte?>(provider, provider.ReturnsNullableByte_Online, null);
            this.VerifyOnlineMethodReturn<SByte?>(provider, provider.ReturnsNullableSByte_Online, null);
            this.VerifyOnlineMethodReturn<Int16?>(provider, provider.ReturnsNullableInt16_Online, null);
            this.VerifyOnlineMethodReturn<UInt16?>(provider, provider.ReturnsNullableUInt16_Online, null);
            this.VerifyOnlineMethodReturn<Int32?>(provider, provider.ReturnsNullableInt32_Online, null);
            this.VerifyOnlineMethodReturn<UInt32?>(provider, provider.ReturnsNullableUInt32_Online, null);
            this.VerifyOnlineMethodReturn<Int64?>(provider, provider.ReturnsNullableInt64_Online, null);
            this.VerifyOnlineMethodReturn<UInt64?>(provider, provider.ReturnsNullableUInt64_Online, null);
            this.VerifyOnlineMethodReturn<Double?>(provider, provider.ReturnsNullableDouble_Online, null);
            this.VerifyOnlineMethodReturn<Single?>(provider, provider.ReturnsNullableSingle_Online, null);
            this.VerifyOnlineMethodReturn<Char?>(provider, provider.ReturnsNullableChar_Online, null);
            this.VerifyOnlineMethodReturn<Decimal?>(provider, provider.ReturnsNullableDecimal_Online, null);
            this.VerifyOnlineMethodReturn<Guid?>(provider, provider.ReturnsNullableGuid_Online, null);
            this.VerifyOnlineMethodReturn<DateTime?>(provider, provider.ReturnsNullableDateTime_Online, null);
            this.VerifyOnlineMethodReturn<DateTimeOffset?>(provider, provider.ReturnsNullableDateTimeOffset_Online, null);
            this.VerifyOnlineMethodReturn<TestEnum?>(provider, provider.ReturnsNullableEnum_Online, null);

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation behavior with an invoke operation that has side effects.")]
        public void InvokeOperation_HasSideEffects()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            InvokeOperation<string> invokeOp = provider.ReturnHttpMethodWithSideEffects_Online(TestHelperMethods.DefaultOperationAction, null);

            this.EnqueueConditional(() => invokeOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsFalse(invokeOp.HasError, "Expected operation to complete without error.");
                Assert.AreEqual("POST", invokeOp.Value);
            });
            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verify invoke operation behavior with an invoke operation that doesn't have side effects.")]
        public void InvokeOperation_HasNoSideEffects()
        {
            TestProvider_Scenarios provider = new TestProvider_Scenarios(TestURIs.TestProvider_Scenarios);
            InvokeOperation<string> invokeOp = provider.ReturnHttpMethodWithoutSideEffects_Online(TestHelperMethods.DefaultOperationAction, null);

            this.EnqueueConditional(() => invokeOp.IsComplete);
            this.EnqueueCallback(() =>
            {
                Assert.IsFalse(invokeOp.HasError, "Expected operation to complete without error.");
                Assert.AreEqual("GET", invokeOp.Value);
            });
            this.EnqueueTestComplete();
        }

        #endregion
        #region test helpers

        private delegate InvokeOperation<T> ReturnTestDelegate<T>(T value, Action<InvokeOperation<T>> callback, object userState);

        /// <summary>
        /// Test helper for invoke operation return type testing
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="provider">an instance to the test scenarios domain service</param>
        /// <param name="testMethod">the invoke operation to be tested</param>
        /// <param name="inputValue">input value to the test method. This should
        /// be of type T</param>
        private void VerifyOnlineMethodReturn<T>(TestProvider_Scenarios provider, ReturnTestDelegate<T> testMethod, T inputValue)
        {
            InvokeOperation invoke = null;

            EnqueueCallback(delegate
            {
                Type[] genericArguments = typeof(T).GetGenericArguments();
                string genericType = (genericArguments != null && genericArguments.Length > 0) ? ("<" + genericArguments[0].Name + ">") : "";
                Console.WriteLine("Verify invoke operation using return type '{0}{1}'...", typeof(T).Name, genericType);

                // call invoke operation with the input value
                invoke = testMethod(inputValue, TestHelperMethods.DefaultOperationAction, null);
            });

            // wait for invoke operation to return. 
            EnqueueConditional(() => invoke.IsComplete);

            EnqueueCallback(delegate
            {
                // verify invocation completed succesfully
                Assert.IsNull(invoke.Error, string.Format("InvokeEventArgs.Error should be null.\r\nMessage: {0}\r\nStack Trace:\r\n{1}", invoke.Error != null ? invoke.Error.Message : string.Empty, invoke.Error != null ? invoke.Error.StackTrace : string.Empty));
                Assert.IsFalse(invoke.ValidationErrors.Any());

                // verify the inputValue is correctly round-tripped back as returnValue
                if (inputValue == null)
                {
                    Assert.IsNull(invoke.Value);
                }
                else
                {
                    if (TypeUtility.FindIEnumerable(typeof(T)) == null)
                    {
                        Assert.AreEqual(inputValue.GetType(), invoke.Value.GetType());
                    }
                    else
                    {
                        Assert.AreEqual(TypeUtility.GetElementType(inputValue.GetType()), TypeUtility.GetElementType(invoke.Value.GetType()));
                    }

                    if (TypeUtility.GetNonNullableType(typeof(T)) == typeof(DateTime))
                    {
                        DateTime returnedDate = (DateTime)invoke.Value;
                        Assert.AreEqual(((DateTime)(object)inputValue).Kind, returnedDate.Kind);
                        Assert.AreEqual(inputValue, returnedDate);
                    }
                    else if (TypeUtility.GetNonNullableType(typeof(T)) == typeof(DateTimeOffset))
                    {
                        DateTimeOffset returnedDate = (DateTimeOffset)invoke.Value;
                        Assert.AreEqual(((DateTimeOffset)(object)inputValue).Offset.Ticks, returnedDate.Offset.Ticks);
                        Assert.AreEqual(inputValue, returnedDate);
                    }
                    else if (typeof(T) == typeof(byte[]))
                    {
                        // if T is byte[], we verify count matches as well as the elements matching
                        Assert.AreEqual(typeof(byte[]), invoke.Value.GetType());
                        byte[] returnedArray = invoke.Value as byte[];
                        byte[] inputArray = inputValue as byte[];

                        Assert.AreEqual(inputArray.Length, returnedArray.Length);
                        for (int i = 0; i < returnedArray.Length; i++)
                        {
                            Assert.AreEqual(inputArray[i], returnedArray[i], string.Format("array elements {0} should be equal", i));
                        }
                    }
                    else if (TypeUtility.IsPredefinedListType(typeof(T)))
                    {
                        IEnumerable<object> inputEnumerable = ((IEnumerable)inputValue).Cast<object>();
                        IEnumerable<object> resultEnumerable = ((IEnumerable)invoke.Value).Cast<object>();
                        Assert.IsTrue(inputEnumerable.SequenceEqual(resultEnumerable));
                    }
                    else
                    {
                        Assert.AreEqual(inputValue, invoke.Value);
                    }
                }

                Console.WriteLine("...Completed");
            });
        }
        #endregion
    }
}
