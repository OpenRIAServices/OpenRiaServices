using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Client.Test;
using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Server.Authentication;

namespace OpenRiaServices.DomainServices.Server.Test.Authentication
{
    /// <summary>
    /// Tests <see cref="AuthenticationCodeProcessor"/> codegen with a barrage of negative tests.
    /// </summary>
    [TestClass]
    public class AuthenticationCodeProcessorTest
    {
        [TestMethod]
        public void UserWithExplicitNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithExplicitName.Auth>());
        }

        [TestMethod]
        public void UserWithGetNameThrows()
        {
            // TODO: Figure out what check is required.
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithGetName.Auth>());
        }

        [TestMethod]
        public void UserWithSetNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithSetName.Auth>());
        }

        [TestMethod]
        public void UserWithIntNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithIntName.Auth>());
        }

        [TestMethod]
        public void UserWithExcludedNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithExcludedName.Auth>());
        }

        [TestMethod]
        public void UserWithNonDataMemberNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithNonDataMemberName.Auth>());
        }

        [TestMethod]
        public void UserWithIgnoredDataMemeberNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithIgnoredDataMemeberName.Auth>());
        }

        [TestMethod]
        public void UserWithNonKeyNameThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithNonKeyName.Auth>());
        }

        [TestMethod]
        public void UserWithExplicitRolesThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithExplicitRoles.Auth>());
        }

        [TestMethod]
        public void UserWithGetRolesThrows()
        {
            // TODO: Figure out what check is required.
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithGetRoles.Auth>());
        }

        [TestMethod]
        public void UserWithSetRolesThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithSetRoles.Auth>());
        }

        [TestMethod]
        public void UserWithIntRolesThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithIntRoles.Auth>());
        }

        [TestMethod]
        public void UserWithExcludedRolesThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithExcludedRoles.Auth>());
        }

        [TestMethod]
        public void UserWithNonDataMemberRolesThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithNonDataMemberRoles.Auth>());
        }

        [TestMethod]
        public void UserWithIgnoredDataMemeberRolesThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<UserWithIgnoredDataMemeberRoles.Auth>());
        }

        [TestMethod]
        public void ServiceWithExplicitLoginThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithExplicitLogin>());
        }

        [TestMethod]
        public void ServiceWithNonUserLoginThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithNonUserLogin>());
        }

        [TestMethod]
        public void ServiceWithNoParameterLoginThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithNoParameterLogin>());
        }

        [TestMethod]
        public void ServiceWithWrongParameterLoginThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithWrongParameterLogin>());
        }

        [TestMethod]
        public void ServiceWithExplicitLogoutThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithExplicitLogout>());
        }

        [TestMethod]
        public void ServiceWithNonUserLogoutThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithNonUserLogout>());
        }

        [TestMethod]
        public void ServiceWithParameterLogoutThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithParameterLogout>());
        }

        [TestMethod]
        public void ServiceWithExplicitGetUserThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithExplicitGetUser>());
        }

        [TestMethod]
        public void ServiceWithNonUserGetUserThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithNonUserGetUser>());
        }

        [TestMethod]
        public void ServiceWithParameterGetUserThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithParameterGetUser>());
        }

        [TestMethod]
        public void ServiceWithExplicitUpdateUserThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithExplicitUpdateUser>());
        }

        [TestMethod]
        public void ServiceWithNonUserUpdateUserThrows()
        {
            ExceptionHelper.ExpectException<InvalidOperationException>(() =>
                Generate<ServiceWithNonUserUpdateUser>());
        }

        private static void Generate<T>()
        {
            new AuthenticationCodeProcessor(new CSharpCodeProvider()).ProcessGeneratedCode(
                DomainServiceDescription.GetDescription(typeof(T)),
                null /* unused in negative tests */,
                null /* unused in negative tests */);
        }

        public class AcpDomainService<T> : DomainService, IAuthentication<T> where T : IUser
        {
            #region IAuthentication<T> Members

            public T Login(string userName, string password, bool isPersistent, string customData)
            {
                throw new NotImplementedException();
            }

            public T Logout()
            {
                throw new NotImplementedException();
            }

            public T GetUser()
            {
                throw new NotImplementedException();
            }

            public void UpdateUser(T user)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        public class AcpUser : IUser
        {
            [Key]
            public string Name { get; set; }
            public IEnumerable<string> Roles { get; set; }
        }

        public class AcpUser2 : IUser
        {
            [Key]
            public string Name { get; set; }
            public IEnumerable<string> Roles { get; set; }
        }

        #region Nested User Types

        // Cannot explicitly implement Name
        public class UserWithExplicitName : IUser
        {
            [Key]
            public int Key { get; set; }

            string IUser.Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithExplicitName> { }
        }

        // Cannot explicitly implement Name and redefine it without a setter
        public class UserWithGetName : IUser
        {
            [Key]
            public string Name { get { return null; } }

            string IUser.Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithGetName> { }
        }

        // Cannot explicitly implement Name and redefine it without a getter
        public class UserWithSetName : IUser
        {
            [Key]
            public int Key { get; set; }

            public string Name { set { } }

            string IUser.Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithSetName> { }
        }

        // Cannot explicitly implement Name and redefine it as another type
        public class UserWithIntName : IUser
        {
            [Key]
            public int Name { get; set; }

            string IUser.Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithIntName> { }
        }

        // Cannot implement IUser but exclude Name
        public class UserWithExcludedName : IUser
        {
            [Key]
            [Exclude]
            public string Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithExcludedName> { }
        }

        // Cannot implement IUser but not include Name as a DataMember (in a DataContract)
        [DataContract]
        public class UserWithNonDataMemberName : IUser
        {
            [Key]
            public string Name { get; set; }
            [DataMember]
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithNonDataMemberName> { }
        }

        // Cannot implement IUser but ignore Name as a DataMember
        public class UserWithIgnoredDataMemeberName : IUser
        {
            [Key]
            [IgnoreDataMember]
            public string Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithIgnoredDataMemeberName> { }
        }

        // Cannot implement IUser but not make Name a key
        public class UserWithNonKeyName : IUser
        {
            [Key]
            public int Key { get; set; }

            public string Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithNonKeyName> { }
        }

        // Cannot explicitly implement Roles
        public class UserWithExplicitRoles : IUser
        {
            [Key]
            public string Name { get; set; }
            IEnumerable<string> IUser.Roles { get; set; }

            public class Auth : AcpDomainService<UserWithExplicitRoles> { }
        }

        // Cannot explicitly implement Roles and redefine it without a setter
        public class UserWithGetRoles : IUser
        {
            [Key]
            public string Name { get; set; }
            public IEnumerable<string> Roles { get { return null; } }

            IEnumerable<string> IUser.Roles { get; set; }

            public class Auth : AcpDomainService<UserWithGetRoles> { }
        }

        // Cannot explicitly implement Roles and redefine it without a getter
        public class UserWithSetRoles : IUser
        {
            [Key]
            public string Name { get; set; }
            public IEnumerable<string> Roles { set { } }

            IEnumerable<string> IUser.Roles { get; set; }

            public class Auth : AcpDomainService<UserWithSetRoles> { }
        }

        // Cannot explicitly implement Roles and redefine it as another type
        public class UserWithIntRoles : IUser
        {
            [Key]
            public string Name { get; set; }
            public int Roles { get; set; }

            IEnumerable<string> IUser.Roles { get; set; }

            public class Auth : AcpDomainService<UserWithIntRoles> { }
        }

        // Cannot implement IUser but exclude Roles
        public class UserWithExcludedRoles : IUser
        {
            [Key]
            public string Name { get; set; }
            [Exclude]
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithExcludedRoles> { }
        }

        // Cannot implement IUser but not include Roles as a DataMember (in a DataContract)
        [DataContract]
        public class UserWithNonDataMemberRoles : IUser
        {
            [Key]
            [DataMember]
            public string Name { get; set; }
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithNonDataMemberRoles> { }
        }

        // Cannot implement IUser but ignore Roles as a DataMember
        public class UserWithIgnoredDataMemeberRoles : IUser
        {
            [Key]
            public string Name { get; set; }
            [IgnoreDataMember]
            public IEnumerable<string> Roles { get; set; }

            public class Auth : AcpDomainService<UserWithIgnoredDataMemeberRoles> { }
        }

        #endregion

        #region Nested Service Types

        // Cannot explicitly implement Login
        public class ServiceWithExplicitLogin : DomainService, IAuthentication<AcpUser>
        {
            AcpUser IAuthentication<AcpUser>.Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot return another type from Login
        public class ServiceWithNonUserLogin : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser2 Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }

            AcpUser IAuthentication<AcpUser>.Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot implement Login without parameters
        public class ServiceWithNoParameterLogin : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser Login() { throw new NotImplementedException(); }

            AcpUser IAuthentication<AcpUser>.Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot implement Login with incorrect parameters
        public class ServiceWithWrongParameterLogin : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser Login(string customData, bool isPersistent, string password, string userName) { throw new NotImplementedException(); }

            AcpUser IAuthentication<AcpUser>.Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot explicitly implement Logout
        public class ServiceWithExplicitLogout : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            AcpUser IAuthentication<AcpUser>.Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot return another type from Logout
        public class ServiceWithNonUserLogout : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser2 Logout() { throw new NotImplementedException(); }

            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            AcpUser IAuthentication<AcpUser>.Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot implement Logout with parameters
        public class ServiceWithParameterLogout : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser Logout(string invalidParameter) { throw new NotImplementedException(); }

            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            AcpUser IAuthentication<AcpUser>.Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot explicitly implement GetUser
        public class ServiceWithExplicitGetUser : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            AcpUser IAuthentication<AcpUser>.GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot return another type from GetUser
        public class ServiceWithNonUserGetUser : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser2 GetUser() { throw new NotImplementedException(); }

            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            AcpUser IAuthentication<AcpUser>.GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot implement GetUser with parameters
        public class ServiceWithParameterGetUser : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser GetUser(string invalidParameter) { throw new NotImplementedException(); }

            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            AcpUser IAuthentication<AcpUser>.GetUser() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot explicitly implement UpdateUser
        public class ServiceWithExplicitUpdateUser : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            void IAuthentication<AcpUser>.UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        // Cannot take another type for UpdateUser
        public class ServiceWithNonUserUpdateUser : DomainService, IAuthentication<AcpUser>
        {
            public AcpUser2 GetUser2() { throw new NotImplementedException(); }
            public void UpdateUser(AcpUser2 user) { throw new NotImplementedException(); }

            public AcpUser Login(string userName, string password, bool isPersistent, string customData) { throw new NotImplementedException(); }
            public AcpUser Logout() { throw new NotImplementedException(); }
            public AcpUser GetUser() { throw new NotImplementedException(); }
            void IAuthentication<AcpUser>.UpdateUser(AcpUser user) { throw new NotImplementedException(); }
        }

        #endregion
    }
}
