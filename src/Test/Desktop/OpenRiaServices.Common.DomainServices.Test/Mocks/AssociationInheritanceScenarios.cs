using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using OpenRiaServices.Server;

namespace TestDomainServices
{
    #region AssociationsAndInheritance

    /// <summary>
    /// AI_MasterToDerived has 2 associations to derived entity types -- one is multivalued
    /// and the other is singlevalued.
    /// </summary>

    [EnableClientAccess]
    public class AssociationInheritanceScenarios : DomainService
    {
        // Returns a collection that actually contains only derived master types.
        // Inheritance hierarchy looks like:
        //  AI_Master <-- AI_MasterDerived
        //  AI_Detail <-- AI_DetailDerived1
        //            <-- AI_DetailDerived2
        //            <-- AI_DetailDerived3
        //            <-- AI_DetailDerived4
        // Association links look like:
        //  AI_MasterDerived:   (1)---(*) AI_DetailDerived1
        //                      (1)---(*) AI_DetailDerived2
        //                      (1)---(1) AI_DetailDerived3
        //                      (1)---(1) AI_DetailDerived4
        // This allows the following scenarios:
        // (1) There are 2 EntityCollections that don't have the same TEntity (though they derive from a common base)
        // (2) There are 2 EntityRef's that don't have the same TEntity
        // (3) There are both EntityRef and EntityCollections with the same TEntity
        public IEnumerable<AI_Master> GetMasters()
        {
            return this.BuildHierarchy();
        }

        // Just to expose detail types
        public IEnumerable<AI_Detail> GetDetails()
        {
            throw new NotSupportedException();
        }

        // Enable client side CUD
        public void InsertMaster(AI_Master master) { }
        public void UpdateMaster(AI_Master master) { }
        public void DeleteMaster(AI_Master master) { }

        public void InsertDetail(AI_Detail detail) { }
        public void UpdateDetail(AI_Detail detail) { }
        public void DeleteDetail(AI_Detail detail) { }

        // Builds a single MasterDerived instance, returning it
        // in a collection
        private IEnumerable<AI_Master> BuildHierarchy()
        {
            int id = 0;
            AI_MasterDerived master = new AI_MasterDerived();
            master.ID = ++id;

            // Add the single-value associations
            AI_DetailDerived3 d3 = new AI_DetailDerived3()
            {
                ID = ++id,
                Master = master,
                MasterID = master.ID
            };
            master.DetailDerived3 = d3;

            AI_DetailDerived4 d4 = new AI_DetailDerived4()
            {
                ID = ++id,
                Master = master,
                MasterID = master.ID
            };
            master.DetailDerived4 = d4;

            // Add the multi-value associations
            List<AI_DetailDerived1> detailDerived1s = new List<AI_DetailDerived1>();
            List<AI_DetailDerived2> detailDerived2s = new List<AI_DetailDerived2>();

            AI_DetailDerived1 d1a = new AI_DetailDerived1()
            {
                ID = ++id,
                Master = master,
                MasterID = master.ID
            };

            AI_DetailDerived1 d1b = new AI_DetailDerived1()
            {
                ID = ++id,
                Master = master,
                MasterID = master.ID
            };
   
            AI_DetailDerived2 d2a = new AI_DetailDerived2()
            {
                ID = ++id,
                Master = master,
                MasterID = master.ID
            };
            master.DetailDerived1s = new List<AI_DetailDerived1>(new[] { d1a, d1b });
 
            AI_DetailDerived2 d2b = new AI_DetailDerived2()
            {
                ID = ++id,
                Master = master,
                MasterID = master.ID
            };
            master.DetailDerived2s = new List<AI_DetailDerived2>(new[] { d2a, d2b });

            return new AI_Master[] { master };
        }
    }

    // Scenario: our root entity has no associations, which tests
    // the performance optimizations in QueryProcessor.  It also
    // contains the key field used in associations in derived types
    [KnownType(typeof(AI_MasterDerived))]
    public abstract class AI_Master
    {
        [Key]
        public int ID { get; set; }

        public override string ToString()
        {
            return this.GetType().Name + ": (" + this.ID + ")";
        }
    }

    // Scenario: this is a derived type and it exposes
    // single-value and multi-value associations to other
    // derived types.  This tests:
    //   EntityCollection handles derived types
    //   EntityRef handles derived types
    //   Presence of 2 different derived types tests entityset notifications
    public class AI_MasterDerived : AI_Master
    {
        private List<AI_DetailDerived1> _detailDerived1s;
        private List<AI_DetailDerived2> _detailDerived2s;
        private AI_DetailDerived3 _detailDerived3;
        private AI_DetailDerived4 _detailDerived4;

        // Multi-value association to a derived type
        [Include]
        [Association("Master_to_Derived1_Many", "ID", "MasterID")]
        public List<AI_DetailDerived1> DetailDerived1s
        {
            get
            {
                return this._detailDerived1s;
            }
            set
            {
                this._detailDerived1s = value;
            }
        }

        // Multi-value association to a derived type
        [Include]
        [Association("Master_to_Derived2_Many", "ID", "MasterID")]
        public List<AI_DetailDerived2> DetailDerived2s
        {
            get
            {
                return this._detailDerived2s;
            }
            set
            {
                this._detailDerived2s = value;
            }
        }

        // Single value association to D3
        [Include]
        [Association("Master_to_Derived3_One", "ID", "MasterID")]
        public AI_DetailDerived3 DetailDerived3
        {
            get
            {
                return this._detailDerived3;
            }
            set
            {
                this._detailDerived3 = value;
            }
        }

        // Single value association to to D4
        [Include]
        [Association("Master_to_Derived4_One", "ID", "MasterID")]
        public AI_DetailDerived4 DetailDerived4
        {
            get
            {
                return this._detailDerived4;
            }
            set
            {
                this._detailDerived4 = value;
            }
        }
    }

    [KnownType(typeof(AI_DetailDerived1))]
    [KnownType(typeof(AI_DetailDerived2))]
    [KnownType(typeof(AI_DetailDerived3))]
    [KnownType(typeof(AI_DetailDerived4))]
    public abstract class AI_Detail
    {
        [Key]
        public int ID { get; set; }

        // edge case -- 2 different associations on derived types share this
        // ID available only on the base
        public int MasterID { get; set; }

        public override string ToString()
        {
            return this.GetType().Name + ": (" + this.ID + ")";
        }
    }

    public class AI_DetailDerived1 : AI_Detail
    {
        private AI_MasterDerived _master;

        [Include]
        [Association("Master_to_Derived1_Many", "MasterID", "ID", IsForeignKey = true)]
        public AI_MasterDerived Master
        {
            get
            {
                return this._master;
            }
            set
            {
                this._master = value;
            }
        }
    }

    public class AI_DetailDerived2 : AI_Detail
    {
        private AI_MasterDerived _master;

        [Include]
        [Association("Master_to_Derived2_Many", "MasterID", "ID", IsForeignKey = true)]
        public AI_MasterDerived Master
        {
            get
            {
                return this._master;
            }
            set
            {
                this._master = value;
            }
        }
    }

    public class AI_DetailDerived3 : AI_Detail
    {
        private AI_MasterDerived _master;

        [Include]
        [Association("Master_to_Derived3_One", "MasterID", "ID", IsForeignKey = true)]
        public AI_MasterDerived Master
        {
            get
            {
                return this._master;
            }
            set
            {
                this._master = value;
            }
        }
    }

    public class AI_DetailDerived4 : AI_Detail
    {
        private AI_MasterDerived _master;

        [Include]
        [Association("Master_to_Derived4_One", "MasterID", "ID", IsForeignKey = true)]
        public AI_MasterDerived Master
        {
            get
            {
                return this._master;
            }
            set
            {
                this._master = value;
            }
        }
    }

    [EnableClientAccess]
    public class InvalidAssociationScenarios : DomainService
    {
        public IEnumerable<Association_A> GetAs() { return null; }
        public IEnumerable<Association_C> GetCs() { return null; }
    }

    [KnownTypeAttribute(typeof(Association_B))]
    public class Association_A
    {
        [Key]
        public int ID { get; set; }

        [Association("C_B", "CID", "ID", IsForeignKey = true)]
        public Association_C C { get; set; }

        public int CID { get; set; }
    }

    public class Association_B : Association_A
    {
        public string Prop1 { get; set; }
    }

    public class Association_C
    {
        private readonly List<Association_B> _bs = new List<Association_B>();

        [Key]
        public int ID { get; set; }

        public string Prop1 { get; set; }

        [Include]
        [Association("C_B", "ID", "CID")]
        public List<Association_B> Bs
        {
            get
            {
                return this._bs;
            }
        }
    }

    #endregion // AssociationsAndInheritance
}
