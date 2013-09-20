using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;

namespace SharedEntities
{
    public class EntityA
    {
        private EntityB _entityB;
        private EntityC _entityC;

        [Key]
        public int Id { get; set; }
        public int IdB { get; set; }
        public int IdC { get; set; }

        [Association("A_B", "IdB", "Id")]
        [Include]
        public EntityB EntityB
        {
            get
            {
                return this._entityB;
            }
            set
            {
                this._entityB = value;
                if (value != null)
                {
                    this.IdB = value.Id;
                }
            }
        }

        [Association("A_C", "IdC", "Id")]
        [Include]
        public EntityC EntityC
        {
            get
            {
                return this._entityC;
            }
            set
            {
                this._entityC = value;
                if (value != null)
                {
                    this.IdC = value.Id;
                }
            }
        }
    }

    public class EntityB
    {
        [Key]
        public int Id { get; set; }
    }

    public class EntityC
    {
        [Key]
        public int Id { get; set; }
    }

    [KnownType(typeof(EntityY))]
    public class EntityX
    {
        private int _id;

        [Key]
        public int Id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
                this.ZProp = value;
            }
        }

        public int ZProp { get; set; }
    }

    public class EntityY : EntityX
    {
        private EntityZ _entityZ;

        public int IdZ { get; set; }

        [Association("Y_Z", "IdZ", "Id")]
        [Include]
        public EntityZ EntityZ
        {
            get
            {
                return this._entityZ;
            }
            set
            {
                this._entityZ = value;
                if (value != null)
                {
                    this.IdZ = value.Id;
                }
            }
        }
    }

    public class EntityZ
    {
        [Key]
        public int Id { get; set; }
    }

    class SharedHelper
    {
        private static List<EntityA> _entityA;
        private static List<EntityX> _entityX;
        private static List<EntityY> _entityY;
        private static int ids = 1;

        static SharedHelper()
        {
            _entityA = new List<EntityA>();
            _entityX = new List<EntityX>();
            _entityY = new List<EntityY>();

            for (int i = 0; i < 20; i++)
            {
                if ((i % 3) == 0)
                {
                    EntityA a = CreateEntityA();
                    _entityA.Add(a);
                }
                else if ((i % 3) == 1)
                {
                    EntityX x = CreateEntityX();
                    _entityX.Add(x);
                }
                else
                {
                    EntityY y = CreateEntityY();
                    _entityY.Add(y);
                }
            }
        }

        private static int NextId
        {
            get
            {
                return ids++;
            }
        }

        public static IQueryable<EntityA> GetEntityAB()
        {
            Func<EntityA, EntityA> transform = (a) =>
            {
                return new EntityA()
                {
                    Id = a.Id,
                    IdB = a.IdB,
                    IdC = a.IdC,
                    EntityB = a.EntityB,
                };
            };

            return _entityA.Select<EntityA, EntityA>(transform).AsQueryable<EntityA>();
        }

        public static IQueryable<EntityA> GetEntityAC()
        {
            Func<EntityA, EntityA> transform = (a) =>
            {
                return new EntityA()
                {
                    Id = a.Id,
                    IdB = a.IdB,
                    IdC = a.IdC,
                    EntityC = a.EntityC,
                };
            };

            return _entityA.Select<EntityA, EntityA>(transform).AsQueryable<EntityA>();
        }

        public static IQueryable<EntityX> GetEntityX()
        {
            return _entityX.AsQueryable();
        }

        public static IQueryable<EntityY> GetEntityY()
        {
            return _entityY.AsQueryable();
        }

        private static EntityA CreateEntityA()
        {
            return new EntityA()
            {
                // init A
                Id = NextId,
                EntityB = new EntityB()
                {
                    Id = NextId,
                },
                EntityC = new EntityC()
                {
                    Id = NextId,
                },
            };
        }

        private static EntityX CreateEntityX()
        {
            return new EntityX()
            {
                Id = NextId,
            };
        }

        private static EntityY CreateEntityY()
        {
            return new EntityY()
            {
                Id = NextId,
                EntityZ = new EntityZ()
                {
                    Id = NextId,
                },
            };
        }
    }

    [EnableClientAccess]
    public class ExposeChildEntityDomainService : DomainService
    {
        [Query]
        public IEnumerable<EntityA> GetA()
        {
            return SharedHelper.GetEntityAB();
        }

        [Query]
        public IEnumerable<EntityB> GetB()
        {
            throw new NotImplementedException();
        }

        [Query]
        public IEnumerable<EntityX> GetX()
        {
            throw new NotImplementedException();
        }

        [Query]
        public IEnumerable<EntityY> GetY()
        {
            return SharedHelper.GetEntityY();
        }

        [Update]
        public void UpdateB(EntityB entityB)
        {
            // Do nothing
        }

        [Update(UsingCustomMethod = true)]
        public void UpdateAThroughChild(EntityA entityA)
        {
            // Do nothing
        }
    }

    [EnableClientAccess]
    public class ExposeParentEntityDomainService : DomainService
    {
        [Query]
        public IEnumerable<EntityA> GetA()
        {
            return SharedHelper.GetEntityAC();
        }

        [Query]
        public IEnumerable<EntityC> GetC()
        {
            throw new NotImplementedException();
        }

        [Query]
        public IEnumerable<EntityX> GetX()
        {
            return SharedHelper.GetEntityX();
        }

        [Update]
        public void UpdateC(EntityC entityC)
        {
            // Do nothing
        }

        [Update(UsingCustomMethod = true)]
        public void UpdateAThroughParent(EntityA entityA)
        {
            // Do nothing
        }
    }
}
