using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdventureWorksModel;

namespace DataModels.ScenarioModels
{
    /// <summary>
    /// This class is used to make sure we don't instantiate ObjectContext at code gen
    /// </summary>
    public class ObjectContextInstantiationScenarios : AdventureWorksEntities
    {
        public ObjectContextInstantiationScenarios()
        {
            throw new InvalidOperationException("error");
        }
    }

    /// <summary>
    /// This class is used to test instantiation errors at code gen
    /// </summary>
    public class DataContextInstantiationScenarios : DataTests.AdventureWorks.LTS.AdventureWorks
    {
        public DataContextInstantiationScenarios()
        {
            throw new InvalidOperationException("error");
        }

        public DataContextInstantiationScenarios(string s) : this()
        {
        }
    }

    /// <summary>
    /// This class is used to make sure one can inherit from ObjectContext and BO wizard still works.
    /// </summary>
    public class ObjectContextInheritanceScenarios : AdventureWorksEntities
    {
        public ObjectContextInheritanceScenarios()
        {
        }
    }

    /// <summary>
    /// This class is used to make sure one can inherit from DataContext and BO wizard still works.
    /// </summary>
    public class DataContextInheritanceScenarios : DataTests.AdventureWorks.LTS.AdventureWorks
    {
        public DataContextInheritanceScenarios()
        {
        }

        public DataContextInheritanceScenarios(string s)
            : this()
        {
        }
    }
}
