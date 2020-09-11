extern alias SSmDsClient;
extern alias SSmDsWeb;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using OpenRiaServices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using OpenRiaServices.Client.Test.Utilities;

namespace OpenRiaServices.Client.ApplicationServices.Test
{
    public class AuthenticationDomainClient : DomainClient
    {
        private SemaphoreSlim _callbackDelay = new SemaphoreSlim(0);

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

        public const string ValidUserName = "ValidUser";
        public const string InvalidUserName = "InvalidUser";

        public Exception Error { get; set; }

        public IEnumerable<ValidationResultInfo> SubmitValidationErrors { get; set; }
        public IEnumerable<string> SubmitConflictMembers { get; set; }
        public bool Submitted { get; set; }

        public bool CancellationRequested { get; set; }

        public override bool SupportsCancellation => true;

        public void RequestCallback()
        {
            _callbackDelay.Release();
        }

        protected override async Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
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

            cancellationToken.Register(() => this.CancellationRequested = true);
            await _callbackDelay.WaitAsync(cancellationToken);

            // Maybe assert expected type
            if (this.Error != null)
            {
                throw this.Error;
            }
            QueryCompletedResult results;
            if (user == null)
            {
                results = new QueryCompletedResult(Array.Empty<MockUser>(), Array.Empty<Entity>(), 0, Array.Empty<ValidationResult>());
            }
            else
            {
                results = new QueryCompletedResult(new MockUser[] { user }, Array.Empty<Entity>(), 1, Array.Empty<ValidationResult>());
            }
            return results;
        }


        protected override async Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
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

     
            cancellationToken.Register(() => this.CancellationRequested = true);
            await _callbackDelay.WaitAsync(cancellationToken);

            // Maybe assert expected type
            if (this.Error != null)
            {
                throw this.Error;
            }

            this.Submitted = true;

            MockUser userToSubmit = (MockUser)submitOperations[0].Entity;
            MockUser userToReturn = new MockUser() { Name = userToSubmit.Name, Type = UserType.Saved };

            List<ChangeSetEntry> submitOperationsToReturn = new List<ChangeSetEntry>();
            submitOperationsToReturn.Add(new ChangeSetEntry(userToReturn, submitOperations[0].Id, submitOperations[0].Operation));

            var operation = submitOperationsToReturn.First();
            operation.Entity = userToReturn;

            operation.ConflictMembers = this.SubmitConflictMembers;
            operation.ValidationErrors = this.SubmitValidationErrors;

            return new SubmitCompletedResult(changeSet, submitOperationsToReturn);
        }

        protected override Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
