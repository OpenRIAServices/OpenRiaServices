using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    // FrameworkElement has Resources too
    using Res = OpenRiaServices.VisualStudio.DomainServices.Tools.Resources;

    /// <summary>
    /// Interaction logic for BusinessLogicClassDialog.xaml
    /// </summary>
    public partial class BusinessLogicClassDialog : Window
    {
        public BusinessLogicClassDialog()
        {
            this.InitializeComponent();
        }

        internal BusinessLogicViewModel Model
        {
            get
            {
                return this.DataContext as BusinessLogicViewModel;
            }

            set
            {
                if (this.DataContext != value)
                {
                    this.DataContext = value;

                    // This setter is called to unregister as well as to register,
                    // but the unregister must stop all use of the model because
                    // it is being disposed.
                    if (this.DataContext != null)
                    {
                        this.InitializePresentation();
                        this.InitializeFromModel();
                    }
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ContextComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = this.ContextComboBox.SelectedIndex;
            if (index >= 0 && index < this.Model.ContextViewModels.Count)
            {
                this.Model.CurrentContextViewModel = this.Model.ContextViewModels[index];
            }
        }

        /// <summary>
        /// Initializes the localized strings in the UI
        /// </summary>
        private void InitializePresentation()
        {
            GridView gridView = this.EntityListView.View as GridView;
            if (gridView != null && gridView.Columns != null && gridView.Columns.Count == 2)
            {
                // Do final width computations after layout has computed string widths
                this.EntityListView.Loaded += new RoutedEventHandler(this.EntityListView_Loaded);
            }

            // This tooltip requires formatting
            this.GenerateMetadataCheckbox.ToolTip = String.Format(CultureInfo.CurrentCulture, Res.BusinessLogicClass_Metadata_Tooltip, this.Model.ClassName);
        }

        /// <summary>
        /// Called after the <see cref="ListView"/> has been loaded.
        /// </summary>
        /// <remarks>
        /// This handler exists solely to compute the column widths of
        /// the grid to optimize for localized string widths
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments</param>
        void EntityListView_Loaded(object sender, RoutedEventArgs e)
        {
            GridView gridView = this.EntityListView.View as GridView;

            double enableEditingColumnWidth = gridView.Columns[1].ActualWidth;
            double totalGridWidth = this.EntityListView.ActualWidth;

            // Enable editing column is at most 1/2 of total width but shrinks to fit shorter string
            enableEditingColumnWidth = Math.Min(enableEditingColumnWidth, (totalGridWidth / 2.0));

            // Entities column is all remaining space to allow entity names as much room as possible
            gridView.Columns[0].Width = totalGridWidth - enableEditingColumnWidth;
            gridView.Columns[1].Width = enableEditingColumnWidth;
        }

        /// <summary>
        /// Initializes the UI controls to select the first available context
        /// </summary>
        private void InitializeFromModel()
        {
            // Set initial index of combo
            int contextsCount = this.Model.ContextViewModels.Count;
            if (contextsCount > 0)
            {
                // 0th element is "empty business logic class", so choose element 1 if we have any real contexts
                int initialIndex = contextsCount > 1 ? 1 : 0;
                this.Model.CurrentContextViewModel = this.Model.ContextViewModels[initialIndex];
                this.ContextComboBox.SelectedIndex = initialIndex;
            }
        }

        /// <summary>
        /// Click handler for the checkbox controls
        /// </summary>
        /// <remarks>
        /// This handler will propagate the current checkbox's state to all selected
        /// items in the list view, giving us multi-select capabilities for checkboxes
        /// </remarks>
        /// <param name="sender">The checkbox</param>
        /// <param name="e">The events</param>
        private void CheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            IList selectedItems = this.EntityListView.SelectedItems;

            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                // Handle entity selection
                if (selectedItems.Count == 0 || !selectedItems.Contains(checkBox.DataContext))
                {
                    this.EntityListView.SelectedItem = checkBox.DataContext;
                }

                bool isChecked = checkBox.IsChecked.HasValue ? checkBox.IsChecked.Value : false;
                foreach (object item in selectedItems)
                {
                    EntityViewModel entity = item as EntityViewModel;
                    if (entity != null)
                    {
                        switch (checkBox.Name)
                        {
                            case "IncludeEntityCheckbox":
                                entity.IsIncluded = isChecked;
                                if (!isChecked)
                                {
                                    goto case "EnableEditingCheckbox";
                                }

                                break;
                            case "EnableEditingCheckbox":
                                entity.IsEditable = isChecked;
                                break;
                            default:
                                Debug.Fail("Unexpected CheckBox.Tag: " + checkBox.Tag);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// "Enable class metadata" Checkbox EnabledChanged event handler.
        /// This checkbox should be unchecked whenever it gets disabled.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CheckBox_ClassMetadata_EnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;

            // Uncheck box when it gets disabled.
            if (cb != null)
            {
                if (cb.IsEnabled)
                {
                    BusinessLogicViewModel model = cb.DataContext as BusinessLogicViewModel;

                    if (model != null)
                    {
                        cb.IsChecked = model.IsMetadataClassGenerationAllowed;
                    }
                }
                else
                {
                    cb.IsChecked = false;
                }
            }
        }

        private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Model.DisplayHelp();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Res.BusinessLogicClass_AvailableContexts_DbContextUrl);
        }
    }
}
