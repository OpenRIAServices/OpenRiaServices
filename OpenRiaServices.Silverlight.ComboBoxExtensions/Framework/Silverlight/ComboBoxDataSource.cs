using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using OpenRiaServices.DomainServices.Client;

namespace OpenRiaServices.Silverlight.ComboBoxExtensions
{
    public class ComboBoxDataSource : Control
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static DependencyProperty DataProperty =
            DependencyProperty.Register(
                "Data",
                typeof(IEnumerable),
                typeof(ComboBoxDataSource),
                new PropertyMetadata(null, ComboBoxDataSource.DataPropertyChanged));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static DependencyProperty DomainContextProperty = 
            DependencyProperty.Register(
                "DomainContext",
                typeof(DomainContext),
                typeof(ComboBoxDataSource),
                new PropertyMetadata(null, ComboBoxDataSource.DomainContextPropertyChanged));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static DependencyProperty OperationNameProperty = 
            DependencyProperty.Register(
                "OperationName",
                typeof(string),
                typeof(ComboBoxDataSource),
                new PropertyMetadata(null, ComboBoxDataSource.OperationNamePropertyChanged));

        private static DependencyProperty ParametersProperty =
            DependencyProperty.Register(
                "Parameters",
                typeof(ParameterCollection),
                typeof(ComboBoxDataSource),
                null);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                "IsLoading",
                typeof(bool),
                typeof(ComboBoxDataSource),
                null);

        private static void DataPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ComboBoxDataSource source = (ComboBoxDataSource)sender;
        }

        private static void DomainContextPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ComboBoxDataSource source = (ComboBoxDataSource)sender;
            source.Refresh();
        }

        private static void OperationNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ComboBoxDataSource source = (ComboBoxDataSource)sender;
            source.Refresh();
        }

        private OperationBase _operation;

        public ComboBoxDataSource()
        {
            this.Parameters = new ParameterCollection();
            this.Parameters.CollectionOrPropertyChanged += this.Parameters_CollectionOrPropertyChanged;
        }

        public IEnumerable Data
        {
            get { return (IEnumerable)this.GetValue(ComboBoxDataSource.DataProperty); }
            private set { this.SetValue(ComboBoxDataSource.DataProperty, value); }
        }

        public DomainContext DomainContext
        {
            get { return (DomainContext)this.GetValue(ComboBoxDataSource.DomainContextProperty); }
            set { this.SetValue(ComboBoxDataSource.DomainContextProperty, value); }
        }

        public string OperationName
        {
            get { return (string)this.GetValue(ComboBoxDataSource.OperationNameProperty); }
            set { this.SetValue(ComboBoxDataSource.OperationNameProperty, value); }
        }

        public ParameterCollection Parameters
        {
            get { return (ParameterCollection)this.GetValue(ComboBoxDataSource.ParametersProperty); }
            private set { this.SetValue(ComboBoxDataSource.ParametersProperty, value); }
        }

        public bool IsLoading
        {
            get { return (bool)this.GetValue(ComboBoxDataSource.IsLoadingProperty); }
            private set { this.SetValue(ComboBoxDataSource.IsLoadingProperty, value); }
        }

        private OperationBase Operation
        {
            get { return this._operation; }
            set
            {
                if (this._operation != value)
                {
                    if ((this._operation != null) && !this._operation.IsComplete)
                    {
                        this._operation.Cancel();
                    }
                    this._operation = value;
                    this.IsLoading = (this._operation != null);
                }
            }
        }

        public event AsyncCompletedEventHandler LoadCompleted;

        private void Parameters_CollectionOrPropertyChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EntityQuery"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "InvokeOperation")]
        public void Refresh()
        {
            if ((this.DomainContext != null) & !string.IsNullOrEmpty(this.OperationName))
            {
                Type domainContextType = this.DomainContext.GetType();
                MethodInfo operationInfo = domainContextType.GetMethods().Where(
                    m => (m.Name == this.OperationName) && (m.GetParameters().Count() == this.Parameters.Count)).FirstOrDefault();
                if (operationInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Could not find a method named " + this.OperationName +
                        " with the specified parameters (" + string.Join(",", this.Parameters.Select(p => p.ParameterName)) + ").");
                }
                else
                {
                    if (typeof(EntityQuery).IsAssignableFrom(operationInfo.ReturnType))
                    {
                        // Query
                        if (!DesignerProperties.IsInDesignTool)
                        {
                            EntityQuery query = (EntityQuery)operationInfo.Invoke(this.DomainContext, this.Parameters.Select(p => p.Value).ToArray());
                            this.Operation = this.DomainContext.Load(query, LoadBehavior.KeepCurrent, this.OnLoadCompleted, null);
                        }
                    }
                    else if (typeof(InvokeOperation).IsAssignableFrom(operationInfo.ReturnType))
                    {
                        // Invoke
                        if (!operationInfo.ReturnType.IsGenericType || !typeof(IEnumerable).IsAssignableFrom(operationInfo.ReturnType.GetGenericArguments()[0]))
                        {
                            throw new NotImplementedException("Support non-enumerable InvokeOperation return types is not implemented.");
                        }
                        if (!DesignerProperties.IsInDesignTool)
                        {
                            this.Operation = (InvokeOperation)operationInfo.Invoke(this.DomainContext, this.Parameters.Select(p => p.Value).ToArray());
                            this.Operation.Completed += this.OnInvokeCompleted;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Support for return types other than EntityQuery and InvokeOperation is not implemented.");
                    }
                }
            }
        }

        private void OnLoadCompleted(LoadOperation op)
        {
            this.Operation = null;

            if (!op.HasError && !op.IsCanceled)
            {
                this.Data = op.Entities;
            }
            this.RaiseLoadCompleted(op);
        }

        private void OnInvokeCompleted(object sender, EventArgs e)
        {
            this.Operation = null;

            InvokeOperation op = (InvokeOperation)sender;
            if (!op.HasError && !op.IsCanceled)
            {
                this.Data = (IEnumerable)op.Value;
            }
            this.RaiseLoadCompleted(op);
        }

        private void RaiseLoadCompleted(OperationBase op)
        {
            if (op.HasError)
            {
                op.MarkErrorAsHandled();
            }

            AsyncCompletedEventHandler handler = this.LoadCompleted;
            if (handler != null)
            {
                handler(this, new AsyncCompletedEventArgs(op.Error, op.IsCanceled, op.UserState));
            }
        }
    }
}
