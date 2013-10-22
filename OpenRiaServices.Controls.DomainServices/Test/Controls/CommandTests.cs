using System;
using OpenRiaServices.DomainServices.Client.Test;
using System.Windows.Input;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the <see cref="DomainDataSourceCommand"/> members and the <see cref="DomainDataSource.LoadCommand"/>,
    /// <see cref="DomainDataSource.RejectChangesCommand"/>, and <see cref="DomainDataSource.SubmitChangesCommand"/>.
    /// </summary>
    [TestClass]
    public class CommandTests : DomainDataSourceTestBase
    {
        [TestMethod]
        [Description("Tests the DomainDataSourceCommand.")]
        public void DomainDataSourceCommand()
        {
            bool canExecute = false;
            bool executed = false;

            DomainDataSource dds = new DomainDataSource();
            DomainDataSourceCommand command = new DomainDataSourceCommand(dds, "Mock", () => canExecute, () => executed = true);

            // Test CanExecute
            Assert.AreEqual(canExecute, command.CanExecute(null),
                "Command should not be executable.");

            canExecute = true;

            Assert.AreEqual(canExecute, command.CanExecute(null),
                "Command should be executable.");

            // Test Execute
            Assert.IsFalse(executed,
                "Command should not have been executed.");

            command.Execute(null);

            Assert.IsTrue(executed,
                "Command should have been executed.");

            // Test Execute exception
            canExecute = false;
            executed = false;

            ExceptionHelper.ExpectException<InvalidOperationException>(() => command.Execute(null));
            Assert.IsFalse(executed,
                "Command should not have been executed after failure.");
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the LoadCommand.")]
        public void LoadCommand()
        {
            int canExecuteChangedExpected = 0;
            ICommand loadCommand = this._dds.LoadCommand;
            loadCommand.CanExecuteChanged += (sender, e) => 
            {
                canExecuteChangedExpected--;
                if (canExecuteChangedExpected < 0)
                {
                    Assert.Fail("Too many CanExecuteChanged events occurred");
                }
            };

            this.EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                Assert.AreEqual(this._dds.CanLoad, loadCommand.CanExecute(null),
                    "CanExecute should return CanLoad.");
                loadCommand.Execute(null);
            });

            this.AssertLoadedData();

            this.EnqueueCallback(() =>
            {
                // CanLoad will change to false when we modify data
                canExecuteChangedExpected = 1;

                ((City)this._dds.DataView[0]).ZoneID = 100;

                Assert.AreEqual(this._dds.CanLoad, loadCommand.CanExecute(null),
                    "CanExecute should still match CanLoad.");
                Assert.AreEqual(0, canExecuteChangedExpected,
                    "CanExecuteChanged should have changed when CanLoad changed.");

                // Test multiple event handlers
                EventHandler handler = (sender, e) => canExecuteChangedExpected--;
                loadCommand.CanExecuteChanged += handler;

                canExecuteChangedExpected = 2;

                this._dds.RejectChanges();

                Assert.AreEqual(this._dds.CanLoad, loadCommand.CanExecute(null),
                    "CanExecute should match CanLoad when using multiple handlers.");
                Assert.AreEqual(0, canExecuteChangedExpected,
                    "CanExecuteChanged should have changed when CanLoad changed when using multiple handlers.");

                // Test handler unsubscription
                loadCommand.CanExecuteChanged -= handler;

                canExecuteChangedExpected = 1;

                ((City)this._dds.DataView[0]).ZoneID = 100;

                Assert.AreEqual(this._dds.CanLoad, loadCommand.CanExecute(null),
                    "CanExecute should match CanLoad after removing the second handler.");
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the RejectChangesCommand.")]
        public void RejectChangesCommand()
        {
            int canExecuteChangedExpected = 0;
            ICommand rejectChangesCommand = this._dds.RejectChangesCommand;
            rejectChangesCommand.CanExecuteChanged += (sender, e) =>
            {
                canExecuteChangedExpected--;
                if (canExecuteChangedExpected < 0)
                {
                    Assert.Fail("Too many CanExecuteChanged events occurred");
                }
            };
            Assert.AreEqual(this._dds.CanRejectChanges, rejectChangesCommand.CanExecute(null),
                "CanExecute should return CanRejectChanges.");

            this.LoadCities(0, false);

            this.EnqueueCallback(() =>
            {
                // CanRejectChanges will change to true when we modify data
                canExecuteChangedExpected = 1;

                ((City)this._dds.DataView[0]).ZoneID = 100;

                Assert.AreEqual(this._dds.CanRejectChanges, rejectChangesCommand.CanExecute(null),
                    "CanExecute should still match CanRejectChanges.");
                Assert.AreEqual(0, canExecuteChangedExpected,
                    "CanExecuteChanged should have changed when CanRejectChanges changed.");

                // Test multiple event handlers
                EventHandler handler = (sender, e) => canExecuteChangedExpected--;
                rejectChangesCommand.CanExecuteChanged += handler;

                canExecuteChangedExpected = 2;

                this._dds.RejectChanges();

                Assert.AreEqual(this._dds.CanRejectChanges, rejectChangesCommand.CanExecute(null),
                    "CanExecute should match CanRejectChanges when using multiple handlers.");
                Assert.AreEqual(0, canExecuteChangedExpected,
                    "CanExecuteChanged should have changed when CanRejectChanges changed when using multiple handlers.");

                // Test handler unsubscription
                rejectChangesCommand.CanExecuteChanged -= handler;

                canExecuteChangedExpected = 1;

                ((City)this._dds.DataView[0]).ZoneID = 100;

                Assert.AreEqual(this._dds.CanRejectChanges, rejectChangesCommand.CanExecute(null),
                    "CanExecute should match CanRejectChanges after removing the second handler.");
            });

            this.EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests the SubmitChangesCommand.")]
        public void SubmitChangesCommand()
        {
            int canExecuteChangedExpected = 0;
            ICommand submitChangesCommand = this._dds.SubmitChangesCommand;
            submitChangesCommand.CanExecuteChanged += (sender, e) =>
            {
                canExecuteChangedExpected--;
                if (canExecuteChangedExpected < 0)
                {
                    Assert.Fail("Too many CanExecuteChanged events occurred");
                }
            };
            Assert.AreEqual(this._dds.CanSubmitChanges, submitChangesCommand.CanExecute(null),
                "CanExecute should return CanSubmitChanges.");

            this.LoadCities(0, false);

            this.EnqueueCallback(() =>
            {
                // CanSubmitChanges will change to true when we modify data
                canExecuteChangedExpected = 1;

                ((City)this._dds.DataView[0]).ZoneID = 100;

                Assert.AreEqual(this._dds.CanSubmitChanges, submitChangesCommand.CanExecute(null),
                    "CanExecute should still match CanSubmitChanges.");
                Assert.AreEqual(0, canExecuteChangedExpected,
                    "CanExecuteChanged should have changed when CanSubmitChanges changed.");

                // Test multiple event handlers
                EventHandler handler = (sender, e) => canExecuteChangedExpected--;
                submitChangesCommand.CanExecuteChanged += handler;

                canExecuteChangedExpected = 2;

                // CanSubmitChanges will change to false when we discard modifications
                this._dds.RejectChanges();

                Assert.AreEqual(this._dds.CanSubmitChanges, submitChangesCommand.CanExecute(null),
                    "CanExecute should match CanSubmitChanges when using multiple handlers.");
                Assert.AreEqual(0, canExecuteChangedExpected,
                    "CanExecuteChanged should have changed when CanSubmitChanges changed when using multiple handlers.");

                // Test handler unsubscription
                submitChangesCommand.CanExecuteChanged -= handler;

                canExecuteChangedExpected = 1;

                ((City)this._dds.DataView[0]).ZoneID = 100;

                Assert.AreEqual(this._dds.CanSubmitChanges, submitChangesCommand.CanExecute(null),
                    "CanExecute should match CanSubmitChanges after removing the second handler.");
            });

            this.EnqueueTestComplete();
        }
    }
}
