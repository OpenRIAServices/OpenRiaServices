using System;
using System.Configuration;
using System.Web.Profile;

namespace OpenRiaServices.Server.Authentication.AspNetMembership.Test
{
    public class MockProfileProvider : ProfileProvider
    {
        #region NotImplemented

        public override int DeleteInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            throw new NotImplementedException();
        }

        public override int DeleteProfiles(string[] usernames)
        {
            throw new NotImplementedException();
        }

        public override int DeleteProfiles(ProfileInfoCollection profiles)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection FindInactiveProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection FindProfilesByUserName(ProfileAuthenticationOption authenticationOption, string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection GetAllInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override ProfileInfoCollection GetAllProfiles(ProfileAuthenticationOption authenticationOption, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfInactiveProfiles(ProfileAuthenticationOption authenticationOption, DateTime userInactiveSinceDate)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065", Justification = "Not Implemented.")]
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        public Exception Error { get; set; }

        public MockUser User { get; set; }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            if (Error != null)
            {
                throw Error;
            }
            var spvc = new SettingsPropertyValueCollection();
            if (User == null)
            {
                return spvc;
            }
            foreach (SettingsProperty prop in collection)
            {
                var propValue = new SettingsPropertyValue(prop);
                propValue.PropertyValue = typeof(MockUser).GetProperty(prop.Name).GetValue(User, null);
                spvc.Add(propValue);
            }

            return spvc;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            if (Error != null)
            {
                throw Error;
            }
            if (User == null)
            {
                return;
            }
            foreach (SettingsPropertyValue propValue in collection)
            {
                typeof(MockUser).GetProperty(propValue.Name).SetValue(User, propValue.PropertyValue, null);
            }
        }
    }
}
