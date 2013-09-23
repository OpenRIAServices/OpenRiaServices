namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    internal partial class DomainServiceClassWizard
    {
        private InternalTestHook _testHook;

        /// <summary>
        /// Internal property to expose the TestHook object
        /// </summary>
        internal InternalTestHook TestHook
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
        /// Test hook class that exposes internal and private members of the wizard
        /// </summary>
        internal class InternalTestHook
        {
            //Reference to the outer 'parent' wizard
            private DomainServiceClassWizard _wizard;

            internal InternalTestHook(DomainServiceClassWizard wizard)
            {
                this._wizard = wizard;
            }

            #region Internal Properties

            internal string GeneratedBusinessLogicCode
            {
                get
                {
                    return this._wizard._businessLogicCode.SourceCode;
                }
            }

            internal string GeneratedMetadataCode
            {
                get
                {
                    return this._wizard._metadataCode.SourceCode;
                }
            }

            internal BusinessLogicClassDialog BusinessLogicClassDialog
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
