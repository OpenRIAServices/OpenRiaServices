namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public partial class DomainServiceClassWizard
    {
        private InternalTestHook _testHook;

        /// <summary>
        /// public property to expose the TestHook object
        /// </summary>
        public InternalTestHook TestHook
        {
            get
            {
                if (this._testHook == null)
                {
                    this._testHook = new InternalTestHook(this);
                }

                return this._testHook;
            }
        }

        /// <summary>
        /// Test hook class that exposes public and private members of the wizard
        /// </summary>
        public class InternalTestHook
        {
            //Reference to the outer 'parent' wizard
            private readonly DomainServiceClassWizard _wizard;

            public InternalTestHook(DomainServiceClassWizard wizard)
            {
                this._wizard = wizard;
            }

            #region public Properties

            public string GeneratedBusinessLogicCode
            {
                get
                {
                    return this._wizard._businessLogicCode.SourceCode;
                }
            }

            public string GeneratedMetadataCode
            {
                get
                {
                    return this._wizard._metadataCode.SourceCode;
                }
            }

            public BusinessLogicClassDialog BusinessLogicClassDialog
            {
                get
                {
                    return this._wizard._dialog;
                }
            }

            #endregion
        }
    }
}
