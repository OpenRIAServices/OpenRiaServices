extern alias SSmDsClient;
extern alias SSmDsWeb;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using OpenRiaServices.DomainServices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices.Client.Test.Utilities;

namespace OpenRiaServices.DomainServices.Client.ApplicationServices.Test
{
    public class AuthenticationDomainClient : DomainClient
    {
        public enum UserType { None, LoggedIn, LoggedOut, Loaded, Saved }

        public class MockUser : Entity, IPrincipal, IIdentity
        {
            public MockUser()
            {
                this.Name = string.Empty;
            }

            public UserType Type { get; set; }

            public int MutableProperty { get; set; }

            public string AuthenticationType { get; set; }
            public bool IsAuthenticated
            {
                get { return !string.IsNullOrEmpty(this.Name); }
            }
            [Key]
            public string Name { get; set; }

            public IIdentity Identity { get { return this; } }
            public IEnumerable<string> Roles { get; set; }
            public bool IsInRole(string role)
            {
                if (this.Roles == null)
                {
                    return false;
                }
                return this.Roles.Contains(role);
            }

            public override object GetIdentity() { return this.Name; }

            public void Modify(DomainContext context)
            {
                EntitySet<MockUser> users = context.EntityContainer.GetEntitySet<MockUser>();
                if (!users.Contains(this))
                {
                    users.Attach(this);
                }
                this.RaiseDataMemberChanging("MutableProperty");
                this.MutableProperty++;
                this.RaiseDataMemberChanged("MutableProperty");
            }
        }

        internal class AdcAsyncResult : Client.Test.AsyncResultBase
        {
            private readonly MockUser _user;

            public AdcAsyncResult(MockUser user, AsyncCallback asyncCallback, object asyncState)
                : base(asyncCallback, asyncState)
            {
                this._user = user;
            }

            public MockUser User
            {
                get { return this._user; }
            }
        }

        internal class AdcSubmitAsyncResult : AdcAsyncResult
        {
            private readonly EntityChangeSet _changeSet;
            private readonly IEnumerable<ChangeSetEntry> _submitOperations;

            private bool _submitted;

            public AdcSubmitAsyncResult(EntityChangeSet changeSet, IEnumerable<ChangeSetEntry> submitOperations, MockUser user, AsyncCallback asyncCallback, object asyncState)
                : base(user, asyncCallback, asyncState)
            {
                this._changeSet = changeSet;
                this._submitOperations = submitOperations;
            }

            public EntityChangeSet ChangeSet
            {
                get { return this._changeSet; }
            }

            public IEnumerable<ChangeSetEntry> SubmitOperations
            {
                get { return this._submitOperations; }
            }

            public bool Submitted
            {
                get { return this._submitted; }
            }

            public void Submit(IEnumerable<string> conflictMembers, IEnumerable<ValidationResultInfo> errors, IEnumerable<ValidationResultInfo> validationErrors)
            {
                this._submitted = true;
                this._submitOperations.First().ConflictMembers = conflictMembers;
                this._submitOperations.First().ValidationErrors = validationErrors;
            }
        }

        public const string ValidUserName = "ValidUser";
        public const string InvalidUserName = "InvalidUser";

        public Exception Error { get; set; }

        private AdcAsyncResult Result { get; set; }

        public IEnumerable<ValidationResultInfo> SubmitErrors { get; set; }
        public IEnumerable<ValidationResultInfo> SubmitValidationErrors { get; set; }
        public IEnumerable<string> SubmitConflictMembers { get; set; }
        public bool Submitted { get; set; }

        public bool CancellationRequested { get; set; }

        public override bool SupportsCancellation
        {
            get
            {
                return true;
            }
        }

        private Timer Timer { get; set; }

        public void RequestCallback()
        {
            this.Timer = null;
            if (this.Result != null)
            {
                this.Result.Complete();
            }
        }

        public void RequestCallback(int delay)
        {
            // We have to post back to the UI thread to avoid an intermittent hang
            this.Timer = new Timer(state => this.RequestCallback(), null, delay, Timeout.Infinite);
            //SynchronizationContext context = SynchronizationContext.Current;
            //new Timer(state => context.Post(state2 => this.RequestCallback(), null), null, delay, Timeout.Infinite);
        }

        protected override Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
        {
            MockUser user = null;

            if (query.QueryName == "Login")
            {
                Assert.IsNotNull(query.Parameters,
                    "Parameters should not be null.");
                Assert.IsTrue(query.Parameters.ContainsKey("UserName"),
                    "Parameters should contain UserName.");
                Assert.IsTrue(query.Parameters.ContainsKey("Password"),
                    "Parameters should contain Password.");
                Assert.IsTrue(query.Parameters.ContainsKey("IsPersistent"),
                    "Parameters should contain IsPersistent.");

                if (AuthenticationDomainClient.ValidUserName == (string)query.Parameters["UserName"])
                {
                    user = new MockUser() { Type = UserType.LoggedIn, Name = "LoggedIn" };
                }
            }
            else if (query.QueryName == "Logout")
            {
                Assert.IsTrue((query.Parameters == null) || (query.Parameters.Count == 0),
                    "Logout operation is not expecting any parameters.");

                user = new MockUser() { Type = UserType.LoggedOut };
            }
            else if (query.QueryName == "GetUser")
            {
                Assert.AreEqual("GetUser", query.QueryName,
                    "Operation should be GetUser.");
                Assert.IsTrue((query.Parameters == null) || (query.Parameters.Count == 0),
                    "GetUser operation is not expecting any parameters.");
                Assert.IsNull(query.Query,
                    "GetUser operation is not expecting a query.");

                user = new MockUser() { Type = UserType.Loaded };
            }
            else
            {
                Assert.Fail("Only Login, Logout, and GetUser methods are supported for queries.");
            }


            AsyncCallback completeCallback = (asyncResult) =>
            {
                var adcAsyncResult = (AdcAsyncResult)asyncResult;
                var tcs = (TaskCompletionSource<QueryCompletedResult>)adcAsyncResult.AsyncState;

                // Maybe assert expected type
                if (this.Error != null)
                {
                    tcs.SetException(this.Error);
                }
                else if (this.CancellationRequested)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    QueryCompletedResult results;
                    if (user == null)
                    {
                        results = new QueryCompletedResult(Array.Empty<MockUser>(), Array.Empty<Entity>(), 0, Array.Empty<ValidationResult>());
                    }
                    else
                    {
                        results = new QueryCompletedResult(new MockUser[] { user }, Array.Empty<Entity>(), 1, Array.Empty<ValidationResult>());
                    }

                    tcs.SetResult(results);
                }
            };

            var taskCompletionSource = new TaskCompletionSource<QueryCompletedResult>();
            this.Result = new AdcAsyncResult(user, completeCallback, taskCompletionSource);

            cancellationToken.Register(res =>
            {
                this.CancellationRequested = true;
            }, this.Result);

            return taskCompletionSource.Task;
        }

        protected override Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            Assert.AreEqual(0, changeSet.AddedEntities.Count,
                "Change set should not contained added entities.");
            Assert.IsFalse(changeSet.IsEmpty,
                "Change set should not be empty.");
            Assert.AreEqual(1, changeSet.ModifiedEntities.Count,
                "Change set should contain a single modified entity.");
            Assert.AreEqual(0, changeSet.RemovedEntities.Count,
                "Change set should not contained removed entities.");

            ChangeSetEntry[] submitOperations = changeSet.GetChangeSetEntries().ToArray();
            Assert.AreEqual(1, submitOperations.Length,
                "A single submit operation is expected.");

            MockUser userToSubmit = (MockUser)submitOperations[0].Entity;
            MockUser userToReturn = new MockUser() { Name = userToSubmit.Name, Type = UserType.Saved };

            List<ChangeSetEntry> submitOperationsToReturn = new List<ChangeSetEntry>();
            submitOperationsToReturn.Add(new ChangeSetEntry(userToReturn, submitOperations[0].Id, submitOperations[0].Operation));


            AsyncCallback completeCallback = (asyncResult) =>
            {
                var tcs = (TaskCompletionSource<SubmitCompletedResult>)asyncResult.AsyncState;

                // Maybe assert expected type
                if (this.Error != null)
                {
                    tcs.SetException(this.Error);
                }
                else if (this.CancellationRequested)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    this.Submitted = true;

                    AdcSubmitAsyncResult result = asyncResult as AdcSubmitAsyncResult;
                    result.SubmitOperations.First().Entity = result.User;
                    result.Submit(this.SubmitConflictMembers, this.SubmitErrors, this.SubmitValidationErrors);
                    SubmitCompletedResult results = new SubmitCompletedResult(result.ChangeSet, result.SubmitOperations);
                    tcs.SetResult(results);
                }
            };

            var taskCompletionSource = new TaskCompletionSource<SubmitCompletedResult>();
            this.Result = new AdcSubmitAsyncResult(changeSet, submitOperationsToReturn, userToReturn, completeCallback, taskCompletionSource);

            cancellationToken.Register(res =>
            {
                this.CancellationRequested = true;
            }, this.Result);

            return taskCompletionSource.Task;
        }

        protected override Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
