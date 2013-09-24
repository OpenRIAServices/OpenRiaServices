using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web.Ria.ApplicationServices;
using System.Web.UI.SilverlightControls;

namespace System.Web.Ria
{
    /// <summary>
    /// Silverlight control for .NET RIA Services Applications.
    /// </summary>
    /// <remarks>
    /// This control adds properties that will be serialized as <c>InitParameters</c>.
    /// </remarks>
    public class SilverlightApplication : Silverlight
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SilverlightApplication class.
        /// </summary>
        public SilverlightApplication() { }

        #endregion

        #region Properties

        // TODO: We may want to mark these with annotations to facilitate designer use

        /// <summary>
        /// Gets or sets a value indicating whether the user state will be serialized.
        /// </summary>
        public bool EnableUserState
        {
            get
            {
                object enableUserState = this.ViewState["EnableUserState"];
                return (enableUserState == null) || ((bool)enableUserState);
            }

            set
            {
                this.ViewState["EnableUserState"] = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Appends the specified key-value pair to the <c>InitParams</c> builder.
        /// </summary>
        /// <remarks>
        /// The key and the value will be added to the <c>InitParams</c> which limits
        /// them to a strict syntax. This method does not bother to enforce these 
        /// constraints, but rather flows everything through.
        /// </remarks>
        /// <param name="initParamsBuilder">The builder to append to</param>
        /// <param name="key">The parameter key</param>
        /// <param name="value">The parameter value</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="initParamsBuilder"/> is null
        /// or <paramref name="key"/> is null or empty.</exception>
        /// <seealso cref="Silverlight.InitParameters"/>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Params", Justification = "Erroneous match.")]
        protected static void AppendInitParameter(StringBuilder initParamsBuilder, string key, string value)
        {
            if (initParamsBuilder == null)
            {
                throw new ArgumentNullException("initParamsBuilder");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            if (initParamsBuilder.Length > 0)
            {
                initParamsBuilder.Append(",");
            }
            initParamsBuilder.Append(key);
            initParamsBuilder.Append("=");
            initParamsBuilder.Append(value);
        }

        /// <summary>
        /// Overridden to append values to the <c>InitParameters</c>.
        /// </summary>
        /// <remarks>
        /// Injecting here allows us to append our values for serialization to the 
        /// client without modifying <c>InitParameters</c>. This method will be invoked
        /// immediately before writing the parameters to HTML in the base class.
        /// </remarks>
        /// <returns>See <see cref="Silverlight.GetSilverlightParameters"/></returns>
        protected override IDictionary<string, string> GetSilverlightParameters()
        {
            IDictionary<string, string> dictionary = base.GetSilverlightParameters();

            // Create a builder for InitParams
            StringBuilder initParamsBuilder = new StringBuilder();
            if (dictionary.ContainsKey("InitParams"))
            {
                initParamsBuilder.Append(dictionary["InitParams"]);
            }

            // Serialize the user
            if (this.EnableUserState && (UserService.Current != null))
            {
                string serializedUser = HttpUtility.UrlEncode(
                    UserSerializer.SerializeUser(
                    UserService.Current.InternalDomainService,
                    UserService.Current.User));

                if (!string.IsNullOrEmpty(serializedUser))
                {
                    SilverlightApplication.AppendInitParameter(
                        initParamsBuilder,
                        UserSerializer.UserKey,
                        serializedUser);
                }
            }

            // Commit updated InitParams to dictionary
            if (initParamsBuilder.Length > 0)
            {
                dictionary["InitParams"] = initParamsBuilder.ToString();
            }

            return dictionary;
        }

        #endregion
    }
}
