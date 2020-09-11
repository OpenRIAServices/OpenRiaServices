using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using OpenRiaServices.Client.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdventureWorksModel;
using NorthwindModel;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class BusinessLogicClassViewModelTests
    {

        [TestMethod]
        [Description("BusinessLogicClassModel ctor initializes correctly to only a default empty context")]
        public void BusinessLogicViewModel_Ctor_No_Contexts()
        {
            string tempFolder = UnitTestHelper.CreateTempFolder();
            try
            {
                using (BusinessLogicViewModel model = new BusinessLogicViewModel(tempFolder, "FooClass", "C#", "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null))
                {
                    Assert.AreEqual(tempFolder, model.ProjectDirectory);
                    Assert.AreEqual("FooClass", model.ClassName);
                    Assert.AreEqual("C#", model.Language);
                    Assert.AreEqual("ARootNamespace", model.RootNamespace);
                    Assert.AreEqual("AnAssemblyName", model.AssemblyName);

                    Assert.IsFalse(model.IsMetadataClassGenerationRequested, "Expect IsMetadataClassGenerationRequested to default to false");

                    Assert.AreEqual(1, model.ContextViewModels.Count, "Expected default context");
                    Assert.IsNotNull(model.CurrentContextViewModel, "Expected non-null context");
                    Assert.AreEqual("<empty Domain Service class>", model.CurrentContextViewModel.Name, "Wrong name for default context");
                }
            }
            finally
            {
                Directory.Delete(tempFolder);
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassModel ctor initializes correctly with one real context")]
        public void BusinessLogicViewModel_Ctor_One_Context()
        {
            string tempFolder = UnitTestHelper.CreateTempFolder();
            try
            {
                // Try the ctor that takes a type list
                using (BusinessLogicViewModel model = new BusinessLogicViewModel(tempFolder, "FooClass", "C#", "ARootNamespace", "AnAssemblyName", new Type[] { typeof(DataTests.Northwind.LTS.NorthwindDataContext) }, /* IVsHelp object */ null))
                {
                    ContextViewModel context = model.CurrentContextViewModel;
                    Assert.IsNotNull(context, "null context");
                    Assert.IsNotNull(model.ContextViewModels, "null context view models");
                    Assert.AreEqual(2, model.ContextViewModels.Count, "Expected 2 contexts");
                    Assert.AreEqual("NorthwindDataContext (LINQ to SQL)", model.CurrentContextViewModel.Name, "Current context had wrong name");
                }
            }
            finally
            {
                Directory.Delete(tempFolder);
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassModel ctor initializes correctly with one many real contexts")]
        public void BusinessLogicViewModel_Ctor_Many_Contexts()
        {
            Type[] contextTypes = new Type[] {
                typeof(DataTests.Northwind.LTS.NorthwindDataContext),
                typeof(NorthwindEntities),
                typeof(DataTests.Scenarios.EF.Northwind.NorthwindEntities_Scenarios),
                typeof(DataTests.Scenarios.LTS.Northwind.NorthwindScenarios),
                typeof(AdventureWorksEntities),
                typeof(DataTests.AdventureWorks.LTS.AdventureWorks),
            };

            string tempFolder = UnitTestHelper.CreateTempFolder();
            try
            {
                // Try the ctor that takes a type list
                using (BusinessLogicViewModel model = new BusinessLogicViewModel(tempFolder, "FooClass", "C#", "ARootNamespace", "AnAssemblyName", contextTypes, /* IVsHelp object */ null))
                {
                    ContextViewModel context = model.CurrentContextViewModel;
                    Assert.IsNotNull(context, "null context");
                    Assert.IsNotNull(model.ContextViewModels, "null context view models");
                    Assert.AreEqual(contextTypes.Length + 1, model.ContextViewModels.Count, "Expected this many contexts");

                    // Verify the first is the empty one
                    Assert.AreEqual("<empty Domain Service class>", model.ContextViewModels[0].Name, "Empty context should have been first");

                    // Verify they are sorted
                    for (int i = 2; i < model.ContextViewModels.Count; ++i)
                    {
                        string name1 = model.ContextViewModels[i - 1].Name;
                        string name2 = model.ContextViewModels[i].Name;
                        Assert.IsTrue(string.Compare(name1, name2, StringComparison.OrdinalIgnoreCase) < 0, "Expected " + name1 + " to collate less than " + name2);
                    }

                    // Cycle through each context to force it to load its entities
                    for (int i = 1; i < model.ContextViewModels.Count; ++i)
                    {
                        model.CurrentContextViewModel = model.ContextViewModels[i];
                        Assert.AreEqual(model.CurrentContextViewModel, model.ContextViewModels[i], "Failed to set current context");
                        IEnumerable<EntityViewModel> entities = model.CurrentContextViewModel.Entities;
                        Assert.IsTrue(entities.Any(), "Expected at least one entity in model " + model.CurrentContextViewModel.Name);

                    }
                }
            }
            finally
            {
                Directory.Delete(tempFolder);
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassModel IsMetadataClassGenerationRequested property depends on having at least one entity")]
        public void BusinessLogicViewModel_IsMetadataClassGenerationRequested_Property()
        {
            string tempFolder = UnitTestHelper.CreateTempFolder();
            try
            {
                Type contextType = typeof(DataTests.Northwind.LTS.NorthwindDataContext);
                string assemblyName = contextType.Assembly.GetName().Name;
                using (BusinessLogicViewModel model = new BusinessLogicViewModel(tempFolder, "FooClass", "C#", "ARootNamespace", assemblyName, new[] { contextType }, /* IVsHelp object */ null))
                {
                    ContextViewModel currentViewModel = model.CurrentContextViewModel;
                    Assert.IsNotNull(currentViewModel);
                    Assert.IsTrue(currentViewModel.Entities.Count() > 0);

                    // Verify we still cannot generate metadata classes even with a current context (until one entity is selected)
                    Assert.IsFalse(model.IsMetadataClassGenerationRequested, "Expect IsMetadataClassGenerationRequested to remain false until include an entity");

                    // Try to set it to true -- it should remain false as long as there are no entities included
                    model.IsMetadataClassGenerationRequested = true;
                    Assert.IsFalse(model.IsMetadataClassGenerationRequested, "Expect IsMetadataClassGenerationRequested to remain false even if try to set to true");

                    // Now include an entity and ensure we see event and can modify it
                    int sawPropertyChange = 0;
                    int sawAllowPropertyChange = 0;
                    model.PropertyChanged += delegate(object sender, PropertyChangedEventArgs eventArgs)
                    {
                        if (eventArgs.PropertyName.Equals("IsMetadataClassGenerationRequested"))
                            ++sawPropertyChange;
                        if (eventArgs.PropertyName.Equals("IsMetadataClassGenerationAllowed"))
                            ++sawAllowPropertyChange;
                    };

                    // Now, include at least one entity and verify we are allowed to generate buddy classes
                    currentViewModel.Entities.First().IsIncluded = true;

                    // This should have raised property on model that enables the checkbox
                    Assert.AreEqual(1, sawAllowPropertyChange, "Failed to see property change event for IsMetadataClassGenerationAllowed after set it");
                    Assert.IsTrue(model.IsMetadataClassGenerationAllowed, "Expected to be able to toggle IsMetadataClassGenerationAllowed");

                    // But it should still be false -- we haven't asked to set it yet
                    Assert.IsFalse(model.IsMetadataClassGenerationRequested, "Expect IsMetadataClassGenerationRequested to remain false until explicitly set");

                    // Now set it.  It should now allow toggling, and we should see another event
                    model.IsMetadataClassGenerationRequested = true;
                    Assert.AreEqual(1, sawPropertyChange, "Failed to see property change event for IsMetadataClassGenerationRequested after set it");
                    Assert.IsTrue(model.IsMetadataClassGenerationRequested, "Expected to be able to toggle IsMetadataClassGenerationRequested");
                }
            }
            finally
            {
                Directory.Delete(tempFolder);
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassModel ctor throws on bad arguments")]
        public void BusinessLogicViewModel_Ctor_Throw_Bad_Args()
        {
            // ProjectDirectory cannot be empty
            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel(null, "FooClass", "C#", "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "projectDirectory");

            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel(string.Empty, "FooClass", "C#", "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "projectDirectory");

            // Classname cannot be empty
            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", null, "C#", "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "className");

            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", string.Empty, "C#", "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "className");

            // Language cannot be empty
            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "AClassName", null, "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "language");

            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "AClassName", string.Empty, "ARootNamespace", "AnAssemblyName", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "language");

            // AssemblyName cannot be empty
            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "AClassName", "C#", "ARootNamespace", null, Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "assemblyName");

            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "AClassName", "C#", "ARootNamespace", "", Array.Empty<Type>(), /* IVsHelp object */ null);
            }, "assemblyName");

            // Contexts cannot be null
            ExceptionHelper.ExpectArgumentNullExceptionStandard(delegate
            {
                BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "AClassName", "C#", "ARootNamespace", "AnAssemblyName", null, /* IVsHelp object */ null);
            }, "contextTypes");
        }

        [TestMethod]
        [Description("BusinessLogicClassModel throws if class name has errors")]
        public void BusinessLogicViewModel_Throws_Invalid_ClassName()
        {
            using (BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "FooClass", "C#", null, "AnAssemblyName", new[] { typeof(DataTests.Northwind.LTS.NorthwindDataContext) }, /* IVsHelp object */ null))
            {
                ExceptionHelper.ExpectArgumentException(delegate
                {
                    model.ClassName = null;   // null is invalid
                },  @"The identifier '' is not a valid class name.   Please enter a valid class name before proceeding.");

                ExceptionHelper.ExpectArgumentException(delegate
                {
                    model.ClassName = "";   // empty is invalid
                }, @"The identifier '' is not a valid class name.   Please enter a valid class name before proceeding.");

                ExceptionHelper.ExpectArgumentException(delegate
                {
                    model.ClassName = "Foo:";   // colon is invalid
                }, @"The identifier 'Foo:' is not a valid class name.   Please enter a valid class name before proceeding.");
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassModel invokes exception handler on invalid class name")]
        public void BusinessLogicViewModel_Invokes_ExceptionHandler_Invalid_ClassName()
        {
            using (BusinessLogicViewModel model = new BusinessLogicViewModel("FooFolder", "FooClass", "C#", null, "AnAssemblyName", new[] { typeof(DataTests.Northwind.LTS.NorthwindDataContext) }, /* IVsHelp object */ null))
            {
                Exception cachedException = null;
                model.ExceptionHandler = delegate(Exception ex)
                {
                    cachedException = ex;
                    throw ex;       // we need to throw it to cause exception below.  Also blocks setter
                };

                ExceptionHelper.ExpectArgumentException(delegate
                {
                    model.ClassName = "Foo:";   // colon is invalid
                }, @"The identifier 'Foo:' is not a valid class name.   Please enter a valid class name before proceeding.");

                Assert.IsNotNull(cachedException, "Exception handler was not called");
                Assert.AreEqual("FooClass", model.ClassName);   // must not have allowed set to occur

                // Now, use a flavor of exception handler that does not throw and permits the setter to succeed
                cachedException = null;
                model.ExceptionHandler = delegate(Exception ex)
                {
                    cachedException = ex;
                };

                model.ClassName = "Foo:";                       // illegal, but won't throw
                Assert.IsNotNull(cachedException);              // verify we were called back
                Assert.AreEqual("Foo:", model.ClassName);       // invalid set succeeded because we did not throw
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassModel properties indicate change events")]
        public void BusinessLogicViewModel_Property_Changes()
        {
            string tempFolder = UnitTestHelper.CreateTempFolder();
            try
            {
                using (BusinessLogicViewModel model = new BusinessLogicViewModel(tempFolder, "FooClass", "C#", null, "AnAssemblyName",
                    new[] { typeof(NorthwindEntities), typeof(DataTests.Northwind.LTS.NorthwindDataContext) }, /* IVsHelp object */ null))
                {
                    int classNameChanged = 0;
                    int currentContextChanged = 0;
                    int entityIncludedChanged = 0;
                    int entityEditableChanged = 0;

                    model.PropertyChanged += delegate(object sender, PropertyChangedEventArgs eventArgs)
                    {
                        if (eventArgs.PropertyName.Equals("ClassName"))
                            classNameChanged++;
                        else if (eventArgs.PropertyName.Equals("CurrentContextViewModel"))
                            currentContextChanged++;
                    };

                    // Setting classname raises event and changes property
                    model.ClassName = "BarClass";
                    Assert.AreEqual(1, classNameChanged);
                    Assert.AreEqual("BarClass", model.ClassName);

                    // Setting current context raises event and sets property
                    model.CurrentContextViewModel = model.ContextViewModels[1];
                    Assert.AreEqual(model.ContextViewModels[1], model.CurrentContextViewModel);
                    Assert.AreEqual(1, currentContextChanged);

                    // As a side-effect, the entities should also have changed
                    List<EntityViewModel> entities = new List<EntityViewModel>(model.CurrentContextViewModel.Entities);
                    Assert.IsTrue(entities.Count > 0, "Expected current entities to be set");
                    EntityViewModel entity = entities[0];

                    // The list of entities must be sorted
                    for (int i = 0; i < entities.Count - 1; ++i)
                    {
                        Assert.AreEqual(-1, String.Compare(entities[i].Name, entities[i + 1].Name), "Expected sorted entity list but " + entities[i].Name + " is not before " + entities[i + 1].Name);
                    }

                    // Slightly fragile -- assume first entity is Category.  This is a probe into the entity business logic entity
                    Assert.AreEqual("Category", entity.Name);

                    // By default, the entity won't be included or editable
                    Assert.IsFalse(entity.IsIncluded);
                    Assert.IsFalse(entity.IsEditable);

                    // Set the editable flag in the entity and verify it set included as well and raises an event
                    entity.PropertyChanged += delegate(object sender, PropertyChangedEventArgs eventArgs)
                    {
                        if (eventArgs.PropertyName.Equals("IsIncluded"))
                            entityIncludedChanged++;
                        else if (eventArgs.PropertyName.Equals("IsEditable"))
                            entityEditableChanged++;
                    };

                    entity.IsEditable = true;
                    Assert.IsTrue(entity.IsEditable);
                    Assert.IsTrue(entity.IsIncluded);   // setting editable also sets included
                    Assert.AreEqual(1, entityIncludedChanged, "Expected event setting IsIncluded");
                    Assert.AreEqual(1, entityEditableChanged, "Expected event setting IsEditable");
                }
            }
            finally
            {
                Directory.Delete(tempFolder);
            }
        }


        public   class PublicEmptyType { }
        public   class PublicNonEmptyClass { public bool BoolProp { get; set; } public int IntProp { get; set; } }
        public   class PublicNoDefaultCtrClass { private int _x; public PublicNoDefaultCtrClass(int x) { _x = x; } }
        private  class PrivateEmptyClass { }
        private  class PrivateNonEmptyClass { public bool BoolProp { get; set; } public int IntProp { get; set; } }
        private  class PrivateNoDefaultCtrClass { private int _x; public PrivateNoDefaultCtrClass(int x) { _x = x; } }
        internal class InternalEmptyType { }
        internal class InternalNonEmptyClass { public bool BoolProp { get; set; } public int IntProp { get; set; } }
        internal class InternalNoDefaultCtrClass { private int _x; public InternalNoDefaultCtrClass(int x) { _x = x; } }
        public interface InterfaceType { }
        public abstract class PublicAbstractType { }
        private abstract class PrivateAbstractType { }
        internal abstract class InternalAbstractType { }
        public struct PublicStructType { }
        private struct PrivateStructType { }
        internal struct InternalStructType { }


        [TestMethod]
        [Description("BusinessLogicViewModel can only have types that are valid generic params")]
        public void BusinessLogicViewModel_Add_ValidGenericTypeParam_Only()
        {
            // This test is for the CodeGenUtilities.IsValidGenericTypeParam which is used
            // by the BusinessLogicClassViewModel to determine what contexts to load and 
            // what enitites to enable.

            Type[] candidates = new Type[]
            {
                typeof(PublicEmptyType),
                typeof(PublicNonEmptyClass),
                typeof(PublicNoDefaultCtrClass),
                typeof(PrivateEmptyClass),
                typeof(PrivateNonEmptyClass),
                typeof(PrivateNoDefaultCtrClass),
                typeof(InternalEmptyType),
                typeof(InternalNonEmptyClass),
                typeof(InternalNoDefaultCtrClass),
                typeof(InterfaceType),
                typeof(PublicAbstractType),
                typeof(PrivateAbstractType),
                typeof(InternalAbstractType),
                typeof(PublicStructType),
                typeof(PrivateStructType),
                typeof(InternalStructType),
            };

            Type[] validTypes = new Type[]
            {
                typeof(PublicEmptyType),
                typeof(PublicNonEmptyClass),
                typeof(PublicAbstractType),
            };

            List<Type> actualTypes = new List<Type>();

            foreach (Type candidate in candidates)
            {
                if (CodeGenUtilities.IsValidGenericTypeParam(candidate))
                {
                    actualTypes.Add(candidate);
                }
            }

            Assert.AreEqual(validTypes.Length, actualTypes.Count, "Invalid types in resulting list!");

            // The list of contexts must be sorted
            foreach (Type validType in validTypes)
            {
                Assert.IsTrue(actualTypes.Contains(validType), "Valid type missing from resulting list!");
            }
        }

    }
}
