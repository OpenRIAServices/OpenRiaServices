using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Client;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.DomainServices.Hosting.Test
{
    [TestClass]
    public class ValidationUtilitiesTests
    {
        [TestMethod]
        [Description("Validating a method with no attributes succeeds")]
        public void Validation_Method_No_Attributes()
        {
            ValTestNoAttributesClass instance = new ValTestNoAttributesClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(instance, null);

            object[] parameters = { "hello", 2.0 };
            bool isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithoutAttributes", context, parameters, null);
            Assert.IsTrue(isValid);

            List<ValidationResult> output = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithoutAttributes", context, parameters, output);
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, output.Count);
        }

        [TestMethod]
        [Description("Validating a method succeeds")]
        public void Validation_Method_Valid()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { "hello", 2.0 };
            bool isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            Assert.IsTrue(isValid);

            List<ValidationResult> output = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, output);
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, output.Count);
        }

        [TestMethod]
        [Description("Validating a parameterless method succeeds")]
        public void Validation_Method_Valid_No_Parameters()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = null;
            bool isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithNoParameters", context, parameters, null);
            Assert.IsTrue(isValid);

            List<ValidationResult> output = new List<ValidationResult>();
            isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithNoParameters", context, parameters, output);
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, output.Count);
        }

        [TestMethod]
        [Description("Validating an invalid method with good parameters returns false")]
        public void Validation_Method_Invalid_Good_Params()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);
            string message = "-MethodDisplayName";

            vtc._failMethod = true; // tells validation method below to fail the IsValid

            object[] parameters = { "hello", 2.0 }; // legal params
            ExceptionHelper.ExpectValidationException(delegate
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, message, typeof(CustomValidationAttribute), vtc);

            List<ValidationResult> output = new List<ValidationResult>();
            bool isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, output);
            Assert.IsFalse(isValid);
            Assert.AreEqual(1, output.Count);
            UnitTestHelper.AssertListContains(output, message);    // hyphen means it came from default formatter
        }

        [TestMethod]
        [Description("Validating an valid method with bad parameters returns false")]
        public void Validation_Method_Invalid_No_Params()
        {
            ValTestClass vtc = new ValTestClass();
            vtc._failMethod = true; // forces failure
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, null, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters' accepting zero parameters could not be found.");

            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, null, new List<ValidationResult>());
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters' accepting zero parameters could not be found.");
        }

        [TestMethod]
        [Description("Validating an valid method with bad parameters returns false")]
        public void Validation_Method_Invalid_Bad_Params()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);
            string message = "The field FirstParameterDisplayName must be a string with a maximum length of 5.";

            object[] parameters = { "LongerThan5Chars", 2.0 }; // legal params
            ExceptionHelper.ExpectValidationException(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, message, typeof(StringLengthAttribute), parameters[0]);

            List<ValidationResult> output = new List<ValidationResult>();
            bool isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, output);
            Assert.IsFalse(isValid);
            Assert.AreEqual(1, output.Count);
            UnitTestHelper.AssertListContains(output, message);
        }

        [TestMethod]
        [Description("Validating an valid method with a required nullable value type returns false")]
        public void Validation_Method_Invalid_Null_Required_Nullable()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);
            string message = "The doubleParam field is required.";

            object[] parameters = { null }; // param is nullable but marked [Required]
            ExceptionHelper.ExpectValidationException(delegate
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithRequiredNullableParameter", context, parameters, null);
            }, message, typeof(RequiredAttribute), parameters[0]);

            List<ValidationResult> output = new List<ValidationResult>();
            bool isValid = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithRequiredNullableParameter", context, parameters, output);
            Assert.IsFalse(isValid);
            Assert.AreEqual(1, output.Count);
            UnitTestHelper.AssertListContains(output, message);
        }

        [TestMethod]
        [Description("Validating an invalid method with good parameters throws")]
        public void Validation_Method_Invalid_Good_Params_Throws()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { "hello", 2.0 }; // legal params

            vtc._failMethod = true; // tells validation method below to fail the IsValid

            ExceptionHelper.ExpectValidationException(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "-MethodDisplayName", typeof(CustomValidationAttribute), vtc);
        }

        [TestMethod]
        [Description("Validating an valid method with bad parameters throws")]
        public void Validation_Method_Invalid_Bad_Params_Throws()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { "LongerThan5Chars", 2.0 }; // legal params

            ExceptionHelper.ExpectValidationException(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "The field FirstParameterDisplayName must be a string with a maximum length of 5.", typeof(StringLengthAttribute), "LongerThan5Chars");
        }

        [TestMethod]
        [Description("Validating an valid method with wrong parameter types throws ArgumentException")]
        public void Validation_Fail_Method_Params_Type_MisMatch()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { 1.0, 2.0 }; // first param should be string

            // IsValid entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.Double', 'System.Double')' could not be found. Parameter count: 2.");

            // Validate entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.Double', 'System.Double')' could not be found. Parameter count: 2.");

        }

        [TestMethod]
        [Description("Method validation errors must match the expected method DisplayAttribute")]
        public void Validation_Method_Errors_Must_Match_Expected_Method_DisplayAttribute()
        {
            ValTestClass instance = new ValTestClass() { _failMethod = true };
            ValidationContext context = new ValidationContext(instance, null, null);
            MethodInfo method = typeof(ValTestClass).GetMethod("MethodWithNoParametersAndDisplayAttribute");
            List<ValidationResult> results = new List<ValidationResult>();

            ExceptionHelper.ExpectException<ValidationException>(delegate
            {
                ValidationUtilities.ValidateMethodCall("MethodWithNoParametersAndDisplayAttribute", context, null);
            }, "-Display Name MethodWithNoParametersAndDisplayAttribute");

            bool result = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithNoParametersAndDisplayAttribute", context, null, results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("-Display Name MethodWithNoParametersAndDisplayAttribute", results[0].ErrorMessage);
        }

        [TestMethod]
        [Description("Method validation errors must match the expected method description")]
        public void Validation_Method_Errors_Must_Match_Expected_Method_Description()
        {
            ValTestClass instance = new ValTestClass() { _failMethod = true };
            ValidationContext context = new ValidationContext(instance, null, null);
            MethodInfo method = typeof(ValTestClass).GetMethod("MethodWithNoParameters");
            List<ValidationResult> results = new List<ValidationResult>();

            ExceptionHelper.ExpectException<ValidationException>(delegate
            {
                ValidationUtilities.ValidateMethodCall("MethodWithNoParameters", context, null);
            }, "-MethodWithNoParameters");

            bool result = ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithNoParameters", context, null, results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("-MethodWithNoParameters", results[0].ErrorMessage);
        }

        [TestMethod]
        [Description("TryValidateObject should fail validation because of collection errors")]
        public void Validation_Object_Invalid_Collection_Errors()
        {
            ValTestClassWithCollection entity = new ValTestClassWithCollection();
            entity.Strings.Add("Invalid");

            ValidationContext validationContext = new ValidationContext(entity, null, null);
            List<ValidationResult> validationResults = new List<ValidationResult>();

            bool result = ValidationUtilities.TryValidateObject(entity, validationContext, validationResults);
            Assert.IsFalse(result,
                "Validation should fail.");
            Assert.AreEqual(1, validationResults.Count(),
                "There should be 1 validation error");

            Assert.AreEqual("Value contains an invalid string", validationResults[0].ErrorMessage,
                "The validation error should concern invalid string");
            Assert.AreEqual(1, validationResults[0].MemberNames.Count(),
                "The validation error should be for a single member");
            Assert.AreEqual("Strings", validationResults[0].MemberNames.First(),
                "The validation error should be for Strings");
        }

        #region Method Parameters
        [TestMethod]
        [Description("Validating a method with too few parameters throws ArgumentException")]
        public void Validation_Fail_Method_Too_Few_Params()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { "xxx" }; // actually requires 2 params

            // IsValid entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.String')' could not be found. Parameter count: 1.");

            // Validate entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.String')' could not be found. Parameter count: 1.");
        }

        [TestMethod]
        [Description("Validating an valid method with too many parameters throws ArgumentException")]
        public void Validation_Fail_Method_Too_Many_Params()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { "xxx", 2.0, 5 }; // actually requires 2 params

            // IsValid entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.String', 'System.Double', 'System.Int32')' could not be found. Parameter count: 3.");

            // Validate entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.String', 'System.Double', 'System.Int32')' could not be found. Parameter count: 3.");
        }

        [TestMethod]
        [Description("Validating a method with a null value type parameter throws ArgumentException")]
        public void Validation_Fail_Method_Null_ValueType()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = { "xxx", null }; // 2nd param should be double

            // IsValid entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.String', null)' could not be found. Parameter count: 2.");

            // Validate entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters('System.String', null)' could not be found. Parameter count: 2.");
        }

        [TestMethod]
        [Description("Validating an valid method with no parameters throws ArgumentException")]
        public void Validation_Fail_Method_No_Params()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = new object[0]; // actually requires 2 params

            // IsValid entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters' accepting zero parameters could not be found.");

            // Validate entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters' accepting zero parameters could not be found.");
        }

        [TestMethod]
        [Description("Validating an valid method with null parameter list returns false")]
        public void Validation_Method_Invalid_Null_Params()
        {
            ValTestClass vtc = new ValTestClass();
            ValidationContext context = ValidationUtilities.CreateValidationContext(vtc, null);

            object[] parameters = null; // actually requires 2 params

            // IsValid entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.TryValidateCustomUpdateMethodCall("MethodWithParameters", context, parameters, null);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters' accepting zero parameters could not be found.");

            // Validate entry point
            ExceptionHelper.ExpectException<MissingMethodException>(delegate()
            {
                ValidationUtilities.ValidateMethodCall("MethodWithParameters", context, parameters);
            }, "Method 'OpenRiaServices.DomainServices.Hosting.Test.ValidationUtilitiesTests+ValTestClass.MethodWithParameters' accepting zero parameters could not be found.");
        }

        #endregion Method Parameters

        #region Test Classes

        public class ValTestNoAttributesClass
        {
            public string PropertyWithoutAttributes { get; set; }
            public void MethodWithoutAttributes(string param1, double param2) { }
        }

        [CustomValidation(typeof(ValTestValidator), "IsValTestValid")]
        public class ValTestClass
        {
            internal bool _failMethod = false;

            [Required]
            public string RequiredProperty { get; set; }

            [StringLength(10)]
            [Display(Name = "StringPropertyDisplayName")]
            public string StringLengthProperty { get; set; }

            [Range(1.0, 5.0)]
            public double DoubleProperty { get; set; }

            // Deliberately omit error message formatter to force it to choose default
            [CustomValidation(typeof(ValTestValidator), "IsValTestMethodValid")]
            [Display(Name = "MethodDisplayName")]
            public void MethodWithParameters(
                [Required] [StringLength(5)] [Display(Name = "FirstParameterDisplayName")] string param1,
                [Required] [Range(1.0, 10.0)] double param2) { }

            [CustomValidation(typeof(ValTestValidator), "IsValTestMethodValid")]
            public void MethodWithOptionalParameter(
                [Required] [StringLength(5)] string param1,
                [Range(1.0, 10.0)] double param2) { }

            public void MethodWithRequiredNullableParameter(
                [Required] double? doubleParam
            ) { }

            public void MethodWithOptionalNullableParameter(double? doubleParam) { }

            [CustomValidation(typeof(ValTestValidator), "IsValTestMethodValid")]
            public void MethodWithNoParameters() { }

            [CustomValidation(typeof(ValTestValidator), "IsValTestMethodValid")]
            [Display(Name = "Display Name MethodWithNoParametersAndDisplayAttribute")]
            public void MethodWithNoParametersAndDisplayAttribute() { }

            [Required]
            [Range(1.0, 5.0)]
            public double? NullableDoubleProperty { get; set; }

            [StringLength(2)]
            [Display(Name = "")]
            public string StringPropertyWithEmptyDisplayName { get; set; }
        }

        public class ValTestClassWithCollection
        {
            private readonly List<string> _strings = new List<string>();

            // This property is used to test validation for collection types and properties without setters
            [CustomValidation(typeof(ValTestValidator), "ContainsInvalidString")]
            public List<string> Strings { get { return this._strings; } }
        }

        public static class ValTestValidator
        {
            // Cross-field validation -- 2 properties must be non-null and equal
            public static ValidationResult IsValTestValid(object vtcObject, ValidationContext context)
            {
                ValidationResult validationResult = null;
                ValTestClass vtc = vtcObject as ValTestClass;
                bool result = vtc != null &&
                                vtc.RequiredProperty != null &&
                                vtc.StringLengthProperty != null &&
                                vtc.StringLengthProperty.Equals(vtc.RequiredProperty, StringComparison.Ordinal);

                if (!result)
                {
                    validationResult = new ValidationResult("!" + context.DisplayName);
                }

                return validationResult ?? ValidationResult.Success;
            }

            // Method level validator
            public static ValidationResult IsValTestMethodValid(object vtcObject, ValidationContext context)
            {
                ValidationResult validationResult = null;
                ValTestClass vtc = vtcObject as ValTestClass;
                bool result = vtc != null && vtc._failMethod == false;
                if (!result)
                {
                    validationResult = new ValidationResult("-" + context.DisplayName);
                }

                return validationResult ?? ValidationResult.Success;
            }

            // Validation method always fails if value is null.
            public static ValidationResult IsValTest_InvalidIfNull(object value, ValidationContext context)
            {
                ValidationResult validationResult = null;
                bool valid = (value != null);
                if (!valid)
                {
                    validationResult = new ValidationResult("Invalid!  Value cannot be null.");
                }

                return validationResult ?? ValidationResult.Success;
            }

            // Validation method verifies expected ValidationContext is received during property-level validation.
            public static ValidationResult IsValTest_IsValidationContextValid(object value, ValidationContext context)
            {
                ValTestClass_PropertyLevel_Validation originalObjectReference = context.ObjectInstance as ValTestClass_PropertyLevel_Validation;
                ValidationResult validationResult = null;

                if ((string)context.Items[typeof(int)] != "int expected")
                {
                    validationResult = new ValidationResult("int expected");
                }
                else if ((string)context.Items[typeof(string)] != "string expected")
                {
                    validationResult = new ValidationResult("string expected");
                }
                else if (originalObjectReference.ObjectProperty != value) // special case, expected to be instance
                {
                    validationResult = new ValidationResult("instance expected");
                }

                return validationResult ?? ValidationResult.Success;
            }

            public static ValidationResult ContainsInvalidString(List<string> strings, ValidationContext context)
            {
                if (strings.Contains("Invalid"))
                {
                    return new ValidationResult("Value contains an invalid string", new[] { context.MemberName });
                }

                return ValidationResult.Success;
            }
        }

        public class ValTestClass_PropertyLevel_Validation
        {
            [CustomValidation(typeof(ValTestValidator), "IsValTest_IsValidationContextValid")]
            public object ObjectProperty { get; set; }
        }

        #endregion
    }
}
