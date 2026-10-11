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
    /// This class is used to make sure one can inherit from ObjectContext and BO wizard still works.
    /// </summary>
    public class ObjectContextInheritanceScenarios : AdventureWorksEntities
    {
        public ObjectContextInheritanceScenarios()
        {
        }
    }

}
