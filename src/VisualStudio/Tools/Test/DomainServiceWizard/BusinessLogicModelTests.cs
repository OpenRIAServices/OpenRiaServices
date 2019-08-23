using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdventureWorksModel;
using NorthwindModel;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class BusinessLogicClassModelTests
    {

        [TestMethod]
        [Description("BusinessLogicModel ctor that accepts logger initializes correctly")]
        public void BusinessLogicModel_Ctor_Logger()
        {
            // Null logger throws
            ExceptionHelper.ExpectArgumentNullExceptionStandard((GenericDelegate)delegate
            {
                Tools.BusinessLogicModel model = new Tools.BusinessLogicModel((Action<string>)null);
            }, "logger");

            // Valid logger is accepted
            string logMessage = null;
            using (BusinessLogicModel model = new BusinessLogicModel(s => logMessage = s))
            {
            }
        }

        [TestMethod]
        [Description("BusinessLogicModel throws if not initialized")]
        public void BusinessLogicModel_Throws_Not_Initialized()
        {
            string logMessage = null;
            using (BusinessLogicModel model = new BusinessLogicModel(s => logMessage = s))
            {
                ExceptionHelper.ExpectInvalidOperationException(() =>
                {
                    // Asking it to perform any work will throw
                   model.GetContextDataItems();
                }, Resources.BusinessLogicClass_Not_Initialized
                );
            }
        }

        [TestMethod]
        [Description("BusinessLogicModel invalid context is reported")]
        public void BusinessLogicModel_Reports_Invalid_Context()
        {
            string logMessage = null;
            using (BusinessLogicModel model = new BusinessLogicModel(s => logMessage = s))
            {
                BusinessLogicData bld = new BusinessLogicData()
                {
                    AssemblyPaths = new string[0],
                    ReferenceAssemblyPaths = new string[0],
                    ContextTypeNames = new string[] { typeof(string).AssemblyQualifiedName },
                    Language = "C#"
                };

                model.Initialize(bld);

                ContextData[] contextDataItems = model.GetContextDataItems();
                Assert.AreEqual(1, contextDataItems.Length, "Expected default context");

                string expectedMessage = string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_InvalidContextType, typeof(string).FullName);
                Assert.AreEqual(expectedMessage, logMessage, "logger was not called");
            }
        }

        [TestMethod]
        [Description("BusinessLogicModel invalid assembly is reported")]
        public void BusinessLogicModel_Reports_Invalid_Assembly()
        {
            string assemblyFileName = Path.GetTempFileName();
            this.BusinessLogicModel_Reports_Invalid_Assembly(assemblyFileName, new string[] { assemblyFileName }, new string[0]);
        }

        [TestMethod]
        [Description("BusinessLogicModel invalid reference assembly is reported")]
        public void BusinessLogicModel_Reports_Invalid_Reference_Assembly()
        {
            string assemblyFileName = Path.GetTempFileName();
            this.BusinessLogicModel_Reports_Invalid_Assembly(assemblyFileName, new string[0], new string[] { assemblyFileName });
        }

        private void BusinessLogicModel_Reports_Invalid_Assembly(string assemblyFileName, string[] assemblyPaths, string[] referenceAssemblyPaths)
        {
            string logMessage = null;
            using (BusinessLogicModel model = new BusinessLogicModel(s => logMessage = s))
            {
                BusinessLogicData bld = new BusinessLogicData()
                {
                    AssemblyPaths = assemblyPaths,
                    ReferenceAssemblyPaths = referenceAssemblyPaths,
                    ContextTypeNames = new string[0],
                    Language = "C#"
                };

                // Format the expected message by duplicating the failure path
                string expectedMessage = null;
                try
                {
                    AssemblyName.GetAssemblyName(assemblyFileName);
                }
                catch (Exception ex)
                {
                    expectedMessage = string.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Failed_Load, assemblyFileName, ex.Message);
                }

                model.Initialize(bld);

                // Asking for context should attempt to load assembly and log the result
                ContextData[] contextDataItems = model.GetContextDataItems();
                Assert.AreEqual(1, contextDataItems.Length, "Expected default context");
                Assert.AreEqual(expectedMessage, logMessage, "logger was not called");
            }
        }
    }
}
