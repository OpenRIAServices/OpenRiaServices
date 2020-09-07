using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EnvDTE;
using OpenRiaServices.VisualStudio.Installer.Helpers;

namespace OpenRiaServices.VisualStudio.Installer.Dialog
{
    /// <summary>
    /// Interaction logic for LinkRiaDialogWindow.xaml
    /// </summary>
    public partial class LinkRiaDialogWindow : VsDialogWindow
    {
        private readonly Project _project;
        private readonly DTE _dte;
        private RIAProjectLinker _linker;
        public LinkRiaDialogWindow(Project project)
            : this(project,
            ServiceLocator.GetInstance<DTE>())
        {

        }

        private LinkRiaDialogWindow(Project project,
            DTE dte)
        {

            _project = project;
            _dte = dte;
            _linker = new RIAProjectLinker(_project, _dte);
            InitializeComponent();
        }

        private void VsDialogWindow_Initialized(object sender, System.EventArgs e)
        {
            Dispatcher.VerifyAccess();
            var noneComboBoxItem = new ComboBoxItem { DataContext = null, Content = "<No Project Set>" };
            //load our combobox items
            this.Projects.Items.Add(noneComboBoxItem);

            //add all the other projects too
            foreach (var i in _dte.Solution.GetSupportedChildProjects().OrderBy(p => p?.Name).Where(p => p != _project))
            {
                this.Projects.Items.Add(new ComboBoxItem { DataContext = i, Content = i.Name });
            }
            // if this is null
            var selectedItem = this.Projects.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.DataContext == _linker.LinkedProject);
            this.Projects.SelectedItem = selectedItem ?? noneComboBoxItem;
            this.OpenRiaGenerateApplicationContext.IsChecked = this._linker.OpenRiaGenerateApplicationContext;
            this.OpenRiaClientUseFullTypeNames.IsChecked = this._linker.OpenRiaClientUseFullTypeNames;
            this.DisableFastUpToDateCheckBox.IsChecked = this._linker.DisableFastUpToDateCheck;
            this.SharedFilesMode.SelectedValue = this._linker.OpenRiaSharedFilesMode;
            this.Projects.SelectionChanged += new SelectionChangedEventHandler(this.Projects_SelectionChanged);


        }
        private void Projects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.VerifyAccess();

            var itemAsComboBoxItem = this.Projects.SelectedItem as ComboBoxItem;
            if (itemAsComboBoxItem == null)
            {
                _linker.LinkedProject = null;
            }
            else
            {
                var selectedProject = itemAsComboBoxItem.DataContext as Project;
                _linker.LinkedProject = selectedProject;
            }

            this._linker.DisableFastUpToDateCheck = this.DisableFastUpToDateCheckBox.IsChecked;
            this._linker.OpenRiaClientUseFullTypeNames = this.OpenRiaClientUseFullTypeNames.IsChecked;
            this._linker.OpenRiaGenerateApplicationContext = this.OpenRiaGenerateApplicationContext.IsChecked;
            this._linker.OpenRiaSharedFilesMode = (OpenRiaSharedFilesMode?)this.SharedFilesMode.SelectedValue;

            _project.Save();

            //close window

            this.Close();
        }


    }
}
