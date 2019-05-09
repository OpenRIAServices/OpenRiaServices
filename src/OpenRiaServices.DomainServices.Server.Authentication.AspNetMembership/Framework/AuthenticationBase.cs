using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Web;
using System.Web.Configuration;
using System.Web.Profile;
using System.Web.Security;

namespace OpenRiaServices.DomainServices.Server.Authentication.AspNetMembership
{
    /// <summary>
    /// <see cref="DomainService"/> that encapsulates the authentication domain.
    /// </summary>
    /// <remarks>
    /// The default <c>AuthenticationService</c> on the client will work with the
    /// <c>DomainContext</c> generated for any domain service extending this class.
    /// <para>
    /// <see cref="UpdateUser"/> is designed as an update method, and will be invoked via
    /// <c>SubmitChanges</c> on the client. This has a couple implications. First,
    /// invoking <see cref="UpdateUser"/> via <c>AuthenticationService.SaveUser</c> will submit all 
    /// changes that have occurred in the <c>DomainContext</c> and may invoke other update methods.
    /// Second, invoking other update methods on the <c>DomainContext</c> from the client will submit
    /// all changes and may invoke <see cref="UpdateUser"/>.
    /// </para>
    /// <para>
    /// By default, this domain service relies on the ASP.NET providers for
    /// <c>Membership, Roles, and Profile</c> and will reflect the customizations made in
    /// each.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the user entity</typeparam>
    public abstract class AuthenticationBase<T> : DomainService, IAuthentication<T> where T : IUser, new()
    {
        #region Static fields

        private static readonly string[] DefaultRoles = Array.Empty<string>();
        private static readonly IPrincipal DefaultPrincipal =
            new GenericPrincipal(new GenericIdentity(string.Empty), DefaultRoles.ToArray());

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationBase{T}"/> class.
        /// </summary>
        protected AuthenticationBase() { }

        #endregion

        #region Methods

        #region Private Static Methods

        /// <summary>
        /// Gets the profile settings for the current user from the <see cref="ProfileBase"/>
        /// and sets them into the specified <paramref name="user"/>.
        /// </summary>
        /// <remarks>
        /// The <paramref name="user"/> is updated from the profile using the following algorithm.
        /// <para>
        /// For every property in <paramref name="user"/>:
        ///  if (the property can be set and is in the profile)
        ///   then set the property value using the value in the profile specified by the alias
        /// </para>
        /// </remarks>
        /// <param name="user">The user to update with the profile settings</param>
        /// <exception cref="InvalidDataContractException"> is thrown if a property in 
        /// <paramref name="user"/> that meets the specified conditions does not have a
        /// corresponding profile value.
        /// </exception>
        private static void GetProfile(T user)
        {
            if (string.IsNullOrEmpty(user.Name) || !ProfileManager.Enabled)
            {
                return;
            }

            // We're creating a new profile so this algorithm works with both Login and
            // Logout where the current principal and Profile have not been updated.
            var profile = ProfileBase.Create(user.Name);

            foreach (PropertyInfo property in user.GetType().GetProperties())
            {
                if (!property.CanWrite || !IsInProfile(property) || property.GetIndexParameters().Length > 0)
                {
                    // Skip this property if it is not writable or in the profile or is an indexer property
                    continue;
                }

                try
                {
                    property.SetValue(user, profile.GetPropertyValue(GetProfileAlias(property)), null);
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    // The default ASP.NET providers use SQL. Since these errors are sometimes
                    // hard to interpret, we're wrapping them to provide more context.
                    throw new DomainException(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.ApplicationServices_ProviderError,
                        "Profile", ex.Message),
                        ex);
                }
                catch (SettingsPropertyNotFoundException e)
                {
                    throw new InvalidDataContractException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.ApplicationServices_ProfilePropertyDoesNotExist,
                            GetProfileAlias(property)),
                        e);
                }
            }
        }

        /// <summary>
        /// Writes the profile settings for the current user to the <see cref="ProfileBase"/>
        /// using the specified <paramref name="user"/>.
        /// </summary>
        /// <remarks>
        /// The profile is updated from the <paramref name="user"/> using the following algorithm.
        /// <para>
        /// For every property in <paramref name="user"/>:
        ///  if (the property can be read and is in the profile)
        ///   then use the property value to set the value in the profile specified by the alias
        /// </para>
        /// </remarks>
        /// <param name="user">The user to update the profile settings with</param>
        /// <exception cref="InvalidDataContractException"> is thrown if a property in 
        /// <paramref name="user"/> that meets the specified conditions does not have a
        /// corresponding profile value.
        /// </exception>
        private static void UpdateProfile(T user)
        {
            if (string.IsNullOrEmpty(user.Name) || !ProfileManager.Enabled)
            {
                return;
            }

            // We're using the current Profile since we've verified it matches the current
            // principal and it allows us to leverage the auto-save feature. When testing,
            // however, we'll need to create a new one.
            ProfileBase profile = GetProfileBase(user.Name);

            foreach (PropertyInfo property in user.GetType().GetProperties())
            {
                if (!property.CanRead ||
                    !property.CanWrite ||
                    !IsInProfile(property) ||
                    IsReadOnly(property) ||
                    property.GetIndexParameters().Length > 0)
                {
                    // Skip this property if it is not readable, in the profile, is readonly or is an indexer property
                    continue;
                }

                try
                {
                    profile.SetPropertyValue(GetProfileAlias(property), property.GetValue(user, null));
                }
                catch (SettingsPropertyNotFoundException e)
                {
                    throw new InvalidDataContractException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.ApplicationServices_ProfilePropertyDoesNotExist,
                            GetProfileAlias(property)),
                        e);
                }
                catch (SettingsPropertyIsReadOnlyException e)
                {
                    throw new InvalidDataContractException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.ApplicationServices_ProfilePropertyReadOnly,
                            GetProfileAlias(property)),
                        e);
                }
                catch (SettingsPropertyWrongTypeException e)
                {
                    throw new InvalidDataContractException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.ApplicationServices_ProfilePropertyTypeMismatch,
                            GetProfileAlias(property)),
                        e);
                }
            }

            // Explicit invocation is necessary when auto-save is not enabled
            bool isAutoSaveEnabled = false;
            try
            {
                isAutoSaveEnabled = ProfileManager.AutomaticSaveEnabled;
            }
            catch (HttpException)
            {
                // If the feature is not supported at the current hosting permission level,
                // we can assume it is not enabled.
            }
            if (!isAutoSaveEnabled)
            {
                profile.Save();
            }
        }

        /// <summary>
        /// Checks that the ASP.NET authentication mode is forms and throws if it is
        /// not.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the ASP.NET 
        /// authentication mode is not <see cref="AuthenticationMode.Forms"/>.
        /// </exception>
        private static void CheckAuthenticationMode()
        {
            if (!FormsAuthentication.IsEnabled)
            {
                throw new InvalidOperationException(Resources.ApplicationServices_LoginLogoutOnlyForForms);
            }
        }

        /// <summary>
        /// Gets the <see cref="ProfileUsageAttribute"/> for the specified property.
        /// </summary>
        /// <param name="propertyInfo">The property to get the attribute for</param>
        /// <returns>The attribute or null if one does not exist</returns>
        private static ProfileUsageAttribute GetProfileUsage(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(typeof(ProfileUsageAttribute), false).SingleOrDefault() as ProfileUsageAttribute;
        }

        /// <summary>
        /// Returns a value indicating whether the specified property has a backing member in
        /// the ASP.NET profile.
        /// </summary>
        /// <param name="propertyInfo">The property to make the determination for</param>
        /// <returns>Whether the property is in the profile</returns>
        private static bool IsInProfile(PropertyInfo propertyInfo)
        {
            bool isInProfile = true;

            ProfileUsageAttribute usageAttribute = GetProfileUsage(propertyInfo);
            if (usageAttribute != null)
            {
                isInProfile = !usageAttribute.IsExcluded;
            }

            return isInProfile;
        }

        /// <summary>
        /// Gets the profile alias for the specified property.
        /// </summary>
        /// <remarks>
        /// This is either:
        /// <para>
        /// 1) <see cref="ProfileUsageAttribute.Alias"/> when the property is marked with the attribute.
        /// 2) <see cref="MemberInfo.Name"/> for the specified property.
        /// </para>
        /// </remarks>
        /// <param name="propertyInfo">The property to get the profile alias for</param>
        /// <returns>The profile alias for the specified property</returns>
        private static string GetProfileAlias(PropertyInfo propertyInfo)
        {
            string profileAlias = propertyInfo.Name;

            ProfileUsageAttribute usageAttribute = GetProfileUsage(propertyInfo);
            if (usageAttribute != null)
            {
                if (!string.IsNullOrEmpty(usageAttribute.Alias))
                {
                    profileAlias = usageAttribute.Alias;
                }
            }

            return profileAlias;
        }

        /// <summary>
        /// Returns a value indicating whether the specified property is read-only.
        /// </summary>
        /// <remarks>
        /// This method determines read only state by checking for appropriately configured
        /// <see cref="EditableAttribute"/>s.
        /// </remarks>
        /// <param name="propertyInfo">The property to determine whether it is read-only</param>
        /// <returns><c>true</c> if the property is marked read-only; <c>false</c> otherwise.</returns>
        private static bool IsReadOnly(PropertyInfo propertyInfo)
        {
            EditableAttribute editableAttribute =
                propertyInfo.GetCustomAttributes(typeof(EditableAttribute), false)
                    .Cast<EditableAttribute>().FirstOrDefault();

            return editableAttribute != null && !editableAttribute.AllowEdit;
        }

        /// <summary>
        /// Gets the current principal from <see cref="HttpContext"/> or returns
        /// a default value.
        /// </summary>
        /// <returns>The current principal</returns>
        private static IPrincipal GetPrincipal()
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.User != null)
            {
                return context.User;
            }
            return DefaultPrincipal;
        }

        /// <summary>
        /// Gets the profile for the current identity from <see cref="HttpContext"/> or returns
        /// a profile base for the specified user.
        /// </summary>
        /// <param name="userName">The name for the user to get the profile for</param>
        /// <returns>The current profile</returns>
        private static ProfileBase GetProfileBase(string userName)
        {
            HttpContext context = HttpContext.Current;
            if (context != null && context.Profile != null)
            {
                return context.Profile;
            }
            return ProfileBase.Create(userName);
        }

        /// <summary>
        /// Gets the roles for the specified identity from <see cref="Roles"/> or returns
        /// a default enumerable.
        /// </summary>
        /// <param name="userName">The userName associated with the identity to get the
        /// roles for</param>
        /// <returns>The roles for the specified identity</returns>
        private static IEnumerable<string> GetRoles(string userName)
        {
            if (Roles.Enabled)
            {
                try
                {
                    return Roles.GetRolesForUser(userName);
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    // The default ASP.NET providers use SQL. Since these errors are sometimes
                    // hard to interpret, we're wrapping them to provide more context.
                    throw new DomainException(string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.ApplicationServices_ProviderError,
                        "Role", ex.Message),
                        ex);
                }
            }
            return DefaultRoles;
        }

        #endregion

        /// <summary>
        /// Authenticates and returns the user with the specified name and password.
        /// </summary>
        /// <remarks>
        /// This method will return a single user if the 
        /// authentication was successful. If the user could not be authenticated, <c>null</c>
        /// will be returned.
        /// <para>
        /// By default, this method can be only used for forms authentication and leverages
        /// ASP.NET <see cref="Membership"/> and <see cref="FormsAuthentication"/>.
        /// </para>
        /// </remarks>
        /// <param name="userName">The userName associated with the user to authenticate</param>
        /// <param name="password">The password associated with the user to authenticate</param>
        /// <param name="isPersistent">Whether the authentication should persist between sessions</param>
        /// <param name="customData">Optional implementation-specific data. It is unused by this base class.</param>
        /// <returns>A single user or <c>null</c> if authentication failed</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the ASP.NET 
        /// authentication mode is not <see cref="AuthenticationMode.Forms"/>.
        /// </exception>
        /// <seealso cref="GetUser()"/>
        public T Login(string userName, string password, bool isPersistent, string customData)
        {
            CheckAuthenticationMode();

            if (ValidateUser(userName, password))
            {
                IPrincipal principal = new GenericPrincipal(
                        new GenericIdentity(userName, "Forms"),
                        DefaultRoles.ToArray());

                IssueAuthenticationToken(principal, isPersistent);

                return GetUserCore(principal);
            }
            return default;
        }

        /// <summary>
        /// Logs an authenticated user out.
        /// </summary>
        /// <remarks>
        /// This method will return a single, anonymous user.
        /// <para>
        /// By default, this method can only be used for forms authentication and leverages
        /// ASP.NET <see cref="FormsAuthentication"/>.
        /// </para>
        /// </remarks>
        /// <returns>A single, default user.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the ASP.NET 
        /// authentication mode is not <see cref="AuthenticationMode.Forms"/>.
        /// </exception>
        /// <seealso cref="GetUser()"/>
        public T Logout()
        {
            CheckAuthenticationMode();

            ClearAuthenticationToken();

            return GetUserCore(DefaultPrincipal);
        }

        /// <summary>
        /// Gets the principal and profile for the current user.
        /// </summary>
        /// <remarks>
        /// This method will return an enumerable containing a single user. If the user is not
        /// authenticated, an anonymous user will be returned.
        /// <para>
        /// By default, the user is populated with data from <see cref="HttpContext"/>,
        /// <see cref="Roles"/>, and <see cref="ProfileBase"/>.
        /// </para>
        /// <para>
        /// In updating the user from the profile, this service copies the corresponding
        /// profile value into each property in <typeparamref name="T"/>. This behavior 
        /// can be tailored by marking specified properties with the
        /// <see cref="ProfileUsageAttribute"/>.
        /// </para>
        /// </remarks>
        /// <returns>An enumerable with a single user.</returns>
        /// <seealso cref="ProfileUsageAttribute"/>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This DataOperation.Select operation must be a method.")]
        public T GetUser()
        {
            return GetUserCore(GetPrincipal());
        }

        /// <summary>
        /// Updates the profile for the authenticated user.
        /// </summary>
        /// <remarks>
        /// By default, the user is persisted to the <see cref="ProfileBase"/>.
        /// <para>
        /// In writing the user to the profile, this service copies each property in 
        /// <typeparamref name="T"/> into the corresponding value in the profile. This behavior
        /// can be tailored by marking specified properties with the 
        /// <see cref="ProfileUsageAttribute"/>.
        /// </para>
        /// </remarks>
        /// <param name="user">The updated user</param>
        /// <exception cref="UnauthorizedAccessException"> is thrown if the authenticated 
        /// user does not have the correct permissions to update the profile.
        /// </exception>
        /// <seealso cref="ProfileUsageAttribute"/>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity",
            Justification = "It shouldn't be a security concern that user.Name is a mutable property. Our goal " +
                            "here is establish that the profile we are about to write to is the one associated " +
                            "with the current authenticated principal.")]
        public void UpdateUser(T user)
        {
            // Ensure the user data that will be modified represents the currently
            // authenticated identity 
            if (ServiceContext.User == null
                || ServiceContext.User.Identity == null
                || !string.Equals(ServiceContext.User.Identity.Name, user.Name, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(Resources.ApplicationServices_UnauthorizedUpdate);
            }

            UpdateUserCore(user);
        }

        /// <summary>
        /// Verifies that the supplied user name and password are valid.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="Login"/>. By default, it delegates to
        /// <see cref="Membership.ValidateUser"/>. The base implementation does not need
        /// to be invoked when this method is overridden.
        /// </remarks>
        /// <param name="userName">The name of the user to be validated</param>
        /// <param name="password">The password for the specified user</param>
        /// <returns>A value indicating whether the user is valid</returns>
        protected virtual bool ValidateUser(string userName, string password)
        {
            try
            {
                return Membership.ValidateUser(userName, password);
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                // The default ASP.NET providers use SQL. Since these errors are sometimes
                // hard to interpret, we're wrapping them to provide more context.
                throw new DomainException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.ApplicationServices_ProviderError,
                    "Membership", ex.Message),
                    ex);
            }
        }

        /// <summary>
        /// Issues a token for the authenticated principal.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="Login"/> when the user is authenticated.
        /// By default, the method will issue a token by leveraging the cookie support
        /// in <see cref="FormsAuthentication"/>. The base implementation does not need to
        /// be invoked when this method is overridden.
        /// </remarks>
        /// <param name="principal">The authenticated principal</param>
        /// <param name="isPersistent">Whether the token should persist between sessions</param>
        protected virtual void IssueAuthenticationToken(IPrincipal principal, bool isPersistent)
        {
            FormsAuthentication.SetAuthCookie(principal.Identity.Name, isPersistent);
        }

        /// <summary>
        /// Clears any issued authentication token.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="Logout"/>. By default, the method will
        /// clear tokens by leveraging the cookie support in <see cref="FormsAuthentication"/>.
        /// The base implementation does not need to be invoked when this method is overridden.
        /// </remarks>
        protected virtual void ClearAuthenticationToken()
        {
            FormsAuthentication.SignOut();
        }

        /// <summary>
        /// Gets the user for the specified principal.
        /// </summary>
        /// <remarks>
        /// This method will return a single user. If the user is not authenticated, an
        /// anonymous user will be returned.
        /// </remarks>
        /// <param name="principal">The principal to get the user for</param>
        /// <returns>A single user.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="GetAuthenticatedUser"/>
        /// or <see cref="GetAnonymousUser"/> returns <c>null</c>.
        /// </exception>
        private T GetUserCore(IPrincipal principal)
        {
            T user;

            if (principal.Identity.IsAuthenticated)
            {
                user = GetAuthenticatedUser(principal);
            }
            else
            {
                user = GetAnonymousUser();
            }

            if (user == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.ApplicationServices_GetUserCannotBeNull,
                        GetType()));
            }

            return user;
        }

        /// <summary>
        /// Gets the user for the authenticated principal.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="Login"/> and <see cref="GetUser()"/> for
        /// authenticated users. By default, the user is populated with data from 
        /// <paramref name="principal"/>, <see cref="Roles"/>, and <see cref="ProfileBase"/>.
        /// The base implementation does not need to be invoked when this method is overridden.
        /// </remarks>
        /// <param name="principal">The principal to get the user for</param>
        /// <returns>The user for the authenticated principal. This should never be <c>null</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="CreateUser"/>
        /// returns <c>null</c>.
        /// </exception>
        /// <seealso cref="GetUser()"/>
        protected virtual T GetAuthenticatedUser(IPrincipal principal)
        {
            return GetUserImpl(principal);
        }

        /// <summary>
        /// Gets an anonymous user.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="Logout"/> and <see cref="GetUser()"/> for
        /// anonymous users. By default, the user is populated with data from 
        /// <see cref="Roles"/>, and <see cref="ProfileBase"/>.
        /// The base implementation does not need to be invoked when this method is overridden.
        /// </remarks>
        /// <returns>The anonymous user. This should never be <c>null</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="CreateUser"/>
        /// returns <c>null</c>.
        /// </exception>
        /// <seealso cref="GetUser()"/>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Remaining a method for similarity to GetUser(IIdentity)")]
        protected virtual T GetAnonymousUser()
        {
            return GetUserImpl(DefaultPrincipal);
        }

        /// <summary>
        /// Gets the user for the specified principal.
        /// </summary>
        /// <remarks>
        /// The user is populated with data from <paramref name="principal"/>,
        /// <see cref="Roles"/>, and <see cref="ProfileBase"/>.
        /// </remarks>
        /// <param name="principal">The principal to get the user for</param>
        /// <returns>The user for the specified principal</returns>
        private T GetUserImpl(IPrincipal principal)
        {
            T user = CreateUser();

            if (user == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.ApplicationServices_CreateUserCannotBeNull,
                        GetType()));
            }

            user.Name = principal.Identity.Name;
            user.Roles = GetRoles(user.Name);

            GetProfile(user);

            return user;
        }

        /// <summary>
        /// Creates a new instance of <typeparamref name="T"/> and initializes it
        /// with default values. 
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="GetAuthenticatedUser"/> and
        /// <see cref="GetAnonymousUser"/>. By default, it returns an instance
        /// created with the default constructor. The base implementation does not
        /// need to be invoked when this method is overridden.
        /// </remarks>
        /// <returns>A new instance of <typeparamref name="T"/></returns>
        protected virtual T CreateUser()
        {
            return new T();
        }

        /// <summary>
        /// Updates the user data for the authenticated identity.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <see cref="UpdateUser"/> after the identity of the
        /// current principal has been verified. It is responsible for persisting the 
        /// updated user data. By default, this method will persist the user using
        /// <see cref="ProfileBase"/>. The base implementation does not need to be invoked 
        /// when this method is overridden.
        /// </remarks>
        /// <param name="user">The updated user data</param>
        protected virtual void UpdateUserCore(T user)
        {
            UpdateProfile(user);
        }

        #endregion
    }
}
