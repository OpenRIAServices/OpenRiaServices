using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.Server.Authentication.AspNetMembership.Test
{
    public class MockUserBase : UserBase
    {
        public bool UserBoolean { get; set; }
        public double UserDouble { get; set; }
        public int UserInt32 { get; set; }
        public string UserString { get; set; }

        // Ensures the profile provider can handle calculated properties. While using [ProfileUsage(IsExcluded = true)]
        // would work on the server, it won't for shared properties and should be easier in the primary scenario.
        public bool UserInt32IsGreaterThan10
        {
            get { return UserInt32 > 10; }
        }

        // The profile provider will return the value of AliasedString for UserStringAliased
        [ProfileUsage(Alias = "AliasedString")]
        public string UserStringAliased { get; set; }
        [ProfileUsage(IsExcluded = true)]
        public string AliasedString { get; set; }

        [Editable(false)]
        public string UserStringReadOnly { get; set; }
        [Editable(true)]
        public string UserStringNotReadOnly { get; set; }

        // These attributes will be removed by the derived class
        [ProfileUsage(Alias = "AliasedString")]
        public virtual string VirtualNotAliased { get; set; }
        [ProfileUsage(IsExcluded = true)]
        public virtual string VirtualInProfile { get; set; }
        [Editable(false)]
        public virtual string VirtualReadOnly { get; set; }
        [Editable(true)]
        public virtual string VirtualNotReadOnly { get; set; }
    }

    public class MockUser : MockUserBase
    {
        public static MockUser CreateDefaultUser()
        {
            return new MockUser()
            {
                Name = string.Empty,
                Roles = Array.Empty<string>(),
            };
        }

        public static MockUser CreateInitializedUser()
        {
            return new MockUser()
            {
                Name = "MockName",
                Roles = new string[] { "MockRole1", "MockRole2", @"Domain\MockRole3" },

                UserBoolean = true,
                UserDouble = 1.2,
                UserInt32 = 3,
                UserString = "MockString",

                UserStringAliased = "UserStringAliased",
                AliasedString = "AliasedString",

                UserStringReadOnly = "ReadOnly",
                UserStringNotReadOnly = "NotReadOnly",

                VirtualNotAliased = "NotAliased",
                VirtualInProfile = "InProfile",
                VirtualReadOnly = "ReadOnly",
                VirtualNotReadOnly = "NotReadOnly",
            };
        }

        public override string VirtualNotAliased { get; set; }
        public override string VirtualInProfile { get; set; }
        public override string VirtualReadOnly { get; set; }
        public override string VirtualNotReadOnly { get; set; }

        //Indexer property. Should be ignored.
        public int this[int index]
        {
            get { return index + 1; }
            set { }
        }
    }
}
