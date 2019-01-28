using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Windows;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// View model for the Business Logic Class wizard.
    /// </summary>
    /// <remarks>
    /// This model has no knowledge of any UI but is a pure model.  The UI is expected
    /// to data-bind to appropriate properties and/or set any it wants to set.
    /// </remarks>
    public class BusinessLogicViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly string _projectDirectory;
        private string _className;
        private readonly string _language;
        private readonly string _rootNamespace;
        private readonly string _assemblyName;
        private List<ContextViewModel> _contextViewModels;
        private ContextViewModel _currentContextViewModel;
        private bool _generateMetadataClasses;
        private ClientBuildManager _clientBuildManager;
        private IBusinessLogicModel _businessLogicModel;
        private readonly List<Type> _contextTypes;
        private readonly IVsHelp _help;

        /// <summary>
        /// Creates a new instance of the <see cref="BusinessLogicViewModel"/> class.
        /// </summary>
        /// <param name="projectDirectory">The full path of the project directory.  It cannot be null or empty.  This is used as the "appVirtualDir"
        /// when creating a <see cref="ClientBuildManager"/>.</param>
        /// <param name="className">The name of the class to generate.  It cannot be null.</param>
        /// <param name="language">The language.  Currently must be "C#" or "VB".</param>
        /// <param name="rootNamespace">The root namespace of the project.  This should be null for C# and non-null for VB.  Codegen will suppress this namespace.</param>
        /// <param name="assemblyName">The name of the assembly containing the code we will generate.</param>
        /// <param name="contextTypes">The list of types to consider.</param>
        /// <param name="help">The help object used to display help on pressing F1.</param>
        public BusinessLogicViewModel(string projectDirectory, string className, string language, string rootNamespace, string assemblyName, IEnumerable<Type> contextTypes, IVsHelp help)
        {
            if (string.IsNullOrEmpty(projectDirectory))
            {
                throw new ArgumentNullException("projectDirectory");
            }

            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }

            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException("language");
            }

            if (contextTypes == null)
            {
                throw new ArgumentNullException("contextTypes");
            }

            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentNullException("assemblyName");
            }

            this._projectDirectory = projectDirectory;
            this._className = className;
            this._language = language;
            this._rootNamespace = rootNamespace;
            this._assemblyName = assemblyName;
            this._contextTypes = new List<Type>(contextTypes);
            this._help = help;
        }

        /// <summary>
        /// Gets the full path to the project directory to use as the
        /// "appVirtualDir" in creating a <see cref="ClientBuildManager"/>.
        /// </summary>
        public string ProjectDirectory
        {
            get
            {
                return this._projectDirectory;
            }
        }

        /// <summary>
        /// Gets or sets the name of the class
        /// </summary>
        public string ClassName 
        { 
            get 
            { 
                return this._className;
            }
            set 
            {
                // If the class name is invalid, we "report" the exception,
                // allowing different clients to show it as they wish to the user.
                this.ValidateClassName(value);

                if (this._className != value)
                {
                    this._className = value;
                    this.RaisePropertyChanged("ClassName");
                }
            }
        }

        /// <summary>
        /// Gets the language for which code will be generated.   It cannot be null.
        /// </summary>
        public string Language
        {
            get
            {
                return this._language;
            }
        }

        /// <summary>
        /// Gets the root namespace.  It can be null.
        /// </summary>
        public string RootNamespace
        {
            get
            {
                return this._rootNamespace;
            }
        }

        /// <summary>
        /// Gets the assembly name in which the domain service is being generated.
        /// </summary>
        public string AssemblyName
        {
            get
            {
                return this._assemblyName;
            }
        }

        /// <summary>
        /// Gets the candidate contexts from which the user can pick
        /// </summary>
        public IList<ContextViewModel> ContextViewModels
        {
            get
            {
                if (this._contextViewModels == null)
                {
                    this._contextViewModels = new List<ContextViewModel>();
                    IEnumerable<IContextData> contextDataItems = ((IContextData[])(this.BusinessLogicModel.GetContextDataItems())).OrderBy(c => c.Name);
                    foreach (IContextData contextData in contextDataItems)
                    {
                        this._contextViewModels.Add(new ContextViewModel(this.BusinessLogicModel, contextData));
                    }
                }
                return this._contextViewModels;
            }
        }

        /// <summary>
        /// Gets a boolean indicating if the DbContext warning should be shown.
        /// </summary>
        public bool ShowDbContextWarning { get; set; }

        /// <summary>
        /// Gets the current context.  We track currency in the view model
        /// </summary>
        public ContextViewModel CurrentContextViewModel
        {
            get
            {
                if (this._currentContextViewModel == null)
                {
                    // The first time we are asked, we select one.
                    // If we have only one, it's obvious.
                    // If we have more than one, pick the first one after the default empty one
                    IList<ContextViewModel> viewModels = this.ContextViewModels;
                    if (viewModels.Count == 1)
                    {
                        this.CurrentContextViewModel = viewModels[0];
                    }
                    else if (viewModels.Count > 1)
                    {
                        this.CurrentContextViewModel = viewModels[1];
                    }
                }
                return this._currentContextViewModel;
            }

            set
            {
                if (value != this._currentContextViewModel)
                {
                    // Stop listening to prior context
                    if (this._currentContextViewModel != null)
                    {
                        this._currentContextViewModel.PropertyChanged -= this.CurrentContextPropertyChanged;
                    }

                    this._currentContextViewModel = value;

                    // Start listening to new context
                    if (this._currentContextViewModel != null)
                    {
                        this._currentContextViewModel.PropertyChanged += this.CurrentContextPropertyChanged;
                    }

                    this.RaisePropertyChanged("CurrentContextViewModel");

                    // Also raise property changed events on calculated properties that depend on current context
                    this.RaisePropertyChanged("IsMetadataClassGenerationRequested");
                    this.RaisePropertyChanged("IsMetadataClassGenerationAllowed");
                }
            }
        }

        public void DisplayHelp()
        {
            if (this._help != null)
            {
                this._help.DisplayTopicFromF1Keyword("DomainServiceWizard.UI");
            }
        }

        /// <summary>
        /// Gets or sets the value indicating whether we will generate buddy classes
        /// </summary>
        public bool IsMetadataClassGenerationRequested
        {
            get
            {
                // Regardless of state, it is logically not allowed if it is not supported by the current context
                bool isAllowed = this._generateMetadataClasses && this.IsMetadataClassGenerationAllowed;
                return isAllowed;
            }

            set
            {
                // Disallow setting this to true if this is not allowed for the current context
                bool isAllowed = this.CurrentContextViewModel == null ? false : this.IsMetadataClassGenerationAllowed;
                if (!isAllowed)
                {
                    value = false;
                }
                if (this._generateMetadataClasses != value)
                {
                    this._generateMetadataClasses = value;
                    this.RaisePropertyChanged("IsMetadataClassGenerationRequested");
                    this.RaisePropertyChanged("IsMetadataClassGenerationAllowed");
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c>only if it is legal to generate the metadata class
        /// </summary>
        /// <returns></returns>
        public bool IsMetadataClassGenerationAllowed
        {
            get
            {
                ContextViewModel context = this.CurrentContextViewModel;
                if (context == null)
                {
                    return false;
                }

                if (!this.BusinessLogicModel.IsMetadataGenerationRequired(context.ContextData))
                {
                    return false;
                }

                IEnumerable<EntityViewModel> includedEntities = context.Entities.Where(e => e.IsIncluded);                
                // Must have a context, and must have at least 1 entity
                if (!includedEntities.Any())
                {
                    return false;
                }

                // Special check -- any entity outside the assembly we are building
                // disqualifies metadata generation because we rely on partial types
                foreach (EntityViewModel entity in includedEntities)
                {
                    string entityAssemblyName = entity.EntityData.AssemblyName;
                    if (!string.Equals(this.AssemblyName, entityAssemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the exception handler.
        /// </summary>
        /// <remarks>
        /// Exceptions raised by this class will first be directed to this handler,
        /// which may throw or ignore.
        /// </remarks>
        public Action<Exception> ExceptionHandler { get; set; }

        /// <summary>
        /// Gets the <see cref="ClientBuildManager"/> the creates a separate AppDomain
        /// and can be used to instantiate <see cref="BusinessLogicModel"/>.
        /// </summary>
        /// <value>This object is <see cref="IDisposable"/>, and we will dispose it
        /// when the current instance is disposed.
        /// </value>
        private ClientBuildManager ClientBuildManager
        {
            get
            {
                if (this._clientBuildManager == null)
                {
                    // Note: this is IDisposable and we control its lifespan and dispose in Dispose() method
                    this._clientBuildManager = new ClientBuildManager(/* appVDir */ "/", this.ProjectDirectory, /*targetDir*/ null, /*clientBuildManagerParameter*/ null);
                }
                return this._clientBuildManager;
            }
        }

        /// <summary>
        /// Gets the <see cref="BusinessLogicModel"/> instance in a separate AppDomain
        /// with which this view model will communicate.
        /// </summary>
         /// <value>This object is <see cref="IDisposable"/>, and we will dispose it
        /// when the current instance is disposed.
        /// </value>
        private IBusinessLogicModel BusinessLogicModel
        {
            get
            {
                if (this._businessLogicModel == null)
                {
                    // Note: this is IDisposable and we control its lifespan and dispose in Dispose() method
                    try
                    {
                        this._businessLogicModel =
                            (IBusinessLogicModel) ClientBuildManager.CreateObject(typeof (BusinessLogicModel), false);
                    }
                    catch (HttpException)
                    {
                        MessageBox.Show(
                            "Visual Studio returned an HttpException. This may indicate that you are missing the httpRuntime is missing from the web.config. For more details, go to http://bit.ly/1jeC1LT");
                        throw;
                    }
                    HashSet<string> assemblyNames = new HashSet<string>();
                    HashSet<string> referenceAssemblyNames = new HashSet<string>();
                    List<string> candidateTypeNames = new List<string>();

                    foreach (Type t in this._contextTypes)
                    {
                        if (CodeGenUtilities.IsValidGenericTypeParam(t))
                        {
                            assemblyNames.Add(t.Assembly.Location);
                            candidateTypeNames.Add(t.AssemblyQualifiedName);

                            // We also need to add to the list of assemblies all those referenced
                            // by the assembly containing this type.  It is a requirement of the
                            // CLR that any referenced types must already be loaded when attempting
                            // to load the candidate types.
                            Action<string> logger = (s => System.Diagnostics.Debug.WriteLine(s));
                            IEnumerable<Assembly> referencedAssemblies = AssemblyUtilities.GetReferencedAssemblies(t.Assembly, logger);
                            foreach (Assembly a in referencedAssemblies)
                            {
                                // Eliminate duplicates.
                                if (!assemblyNames.Contains(a.Location))
                                {
                                    referenceAssemblyNames.Add(a.Location);
                                }
                            }
                        }
                    }

                    IBusinessLogicData data = new BusinessLogicData()
                    {
                        Language = this.Language,
                        AssemblyPaths = assemblyNames.ToArray(),
                        ReferenceAssemblyPaths = referenceAssemblyNames.ToArray(),
                        ContextTypeNames = candidateTypeNames.ToArray(),
                        LinqToSqlPath = LinqToSqlContext.LinqToSqlDomainServiceAssemblyPath
                    };

                    this._businessLogicModel.Initialize(data);
                }
                return this._businessLogicModel;
            }
        }

        /// <summary>
        /// Generates the business logic class code and required references.
        /// </summary>
        /// <param name="namespaceName">The namespace into which to generate the class</param>
        /// <returns>The generated code and references</returns>
        public IGeneratedCode GenerateBusinessLogicClass(string namespaceName)
        {
            ContextViewModel contextViewModel = this.CurrentContextViewModel;
            if (contextViewModel != null)
            {
                return this.BusinessLogicModel.GenerateBusinessLogicClass(contextViewModel.ContextData, this.ClassName, namespaceName, this.RootNamespace);
            }
            else
            {
                return new GeneratedCode();
            }
        }

        /// <summary>
        /// Generates the associated metadata class and required references
        /// </summary>
        /// <param name="optionalSuffix">If not blank, an optional suffix for namespace and class (for testing)</param>
        /// <returns>The generated code and references</returns>
        public IGeneratedCode GenerateMetadataClasses(string optionalSuffix)
        {
            ContextViewModel contextViewModel = this.CurrentContextViewModel;
            if (contextViewModel != null)
            {
                return this.BusinessLogicModel.GenerateMetadataClasses(contextViewModel.ContextData, this.RootNamespace, optionalSuffix);
            }
            else
            {
                return new GeneratedCode(string.Empty, new string[0]);
            }
        }

        /// <summary>
        /// Logical equivalent of 'throw'.  If an <see cref="ExceptionHandler"/> is
        /// registered, it is invoked with the exception, otherwise the exception is thrown
        /// </summary>
        /// <param name="exception">The exception to throw or report</param>
        private void ReportException(Exception exception)
        {
            Action<Exception> handler = this.ExceptionHandler;
            if (handler != null)
            {
                handler(exception);
            }
            else
            {
                throw exception;
            }
        }

        /// <summary>
        /// Tests whether the given class name is a legal identifier for the current language.
        /// </summary>
        /// <remarks>
        /// This test will invoke <see cref="ReportException"/> with an <see cref="ArgumentException"/>
        /// if the class name is not a legal identifier.
        /// </remarks>
        /// <param name="className">String to test for legality</param>
        private void ValidateClassName(string className)
        {
            using (CodeGenContext codeGenContext = new CodeGenContext(this.Language, this.RootNamespace))
            {
                if (string.IsNullOrEmpty(className) || !codeGenContext.IsValidIdentifier(className))
                {
                    this.ReportException(new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.BusinessLogicClass_Error_Invalid_ClassName, className)));
                }
            }
        }

        /// <summary>
        /// Listens to property change notifications for the current business logic context
        /// and re-raises calculated properties to allow data bound fields to react
        /// </summary>
        /// <param name="sender">The business logic context</param>
        /// <param name="propertyChangedEventArgs">The event arguments.</param>
        private void CurrentContextPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            string propertyName = propertyChangedEventArgs.PropertyName;

            // BusinessLogicContext raising this property re-raises our own property change
            // for the same property.  Their notification is based on local changes, and our
            // notification triggers data bound fields to ask questions that only this view model knows
            if (string.Equals(propertyName, "IsMetadataClassGenerationAllowed", StringComparison.OrdinalIgnoreCase))
            {
                this.RaisePropertyChanged(propertyName);
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Raises a property changed event for the given property name
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Override of IDisposable.Dispose to handle implementation details of dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (this._businessLogicModel  != null)
            {
                try
                {
                    this._businessLogicModel.Dispose();
                }
                catch (System.Runtime.Remoting.RemotingException)
                {
                    // This condition is unlikely but possible if the 2nd AppDomain
                    // is torn down independently.  There is nothing we can do here,
                    // as it is not a user error.  We catch and ignore solely to
                    // prevent the wizard from showing a fatal error the user does
                    // not understand.
                }
                this._businessLogicModel = null;
            }

            IDisposable disposable = this._clientBuildManager as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            this._clientBuildManager = null;
        }
        #endregion
    }
}
