using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.ServiceModel.DomainServices.Tools.TextTemplate.Test
{
    [EnableClientAccess]
    public class TestDomainService1 : DomainService
    {
        #region Query Methods

        public IQueryable<SpecialTypesEntity> GetSpecialTypesEntity()
        {
            return SpecialTypesEntity.GetData();
        }

        [Invoke]
        public SharedInt32Enum? OM_RoundTrip_NullableInt32Enum(SharedInt32Enum? int32Enum)
        {
            return int32Enum;
        }

        [Update(UsingCustomMethod = true)]
        public void DMTypeTestCityAndInt32Enum(SpecialTypesEntity city, SharedInt32Enum int32Enum)
        {
        }

        #endregion

        [Query]
        public IQueryable<@class> GetClasses()
        {
            return null;
        }

        [Query]
        public IQueryable<@namespace> GetNS()
        {
            return null;
        }

        [Update(UsingCustomMethod = true)]
        public void DMTypeTestCityAndInt32Enum(@namespace city, SharedInt32Enum int32Enum)
        {
        }
    }

    public partial class SpecialTypesEntity
    {
        public static IQueryable<SpecialTypesEntity> GetData()
        {
            List<SpecialTypesEntity> ret = new List<SpecialTypesEntity>();
            return ret.AsQueryable();
        }

        [Key]
        public int Id { get; set; }

        public string CustomValidateMe { get; set; }

        private float?[] nullableFloatArrayProperty;

        public string @char { get; set; }

        public string @as { get; set; }

        public float?[] NullableFloatArrayProperty
        {
            get { return this.nullableFloatArrayProperty; }
            set { this.nullableFloatArrayProperty = value; }
        }
    }

    public partial class @class
    {

        [Key]
        public int Id { get; set; }

        public string CustomValidateMe { get; set; }
    }

    public partial class @namespace
    {

        [Key]
        public int Id { get; set; }

        public string CustomValidateMe { get; set; }
    }

    public enum SharedInt32Enum : sbyte
    {
        SharedInt32Enum_ValueOne,
        SharedInt32Enum_ValueTwo
    }


}
