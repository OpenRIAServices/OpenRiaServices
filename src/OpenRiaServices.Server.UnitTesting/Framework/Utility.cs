﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OpenRiaServices.Server.UnitTesting
{
    internal static class Utility
    {
        private const int DefaultChangeSetEntryId = 1;

        /// <summary>
        /// Do a blocking wait on a ValueTask that work even when task is not completed and it is backed by a IValueTaskSource
        /// Se https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2012
        /// </summary>
        public static T SafeGetResult<T>(this ValueTask<T> valueTask)
        {
            return valueTask.IsCompleted ? valueTask.GetAwaiter().GetResult() : valueTask.AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Do a blocking wait on a ValueTask that work even when task is not completed and it is backed by a IValueTaskSource
        /// Se https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2012
        /// </summary>
        public static void SafeGetResult(ValueTask valueTask)
        {
            valueTask.AsTask().GetAwaiter().GetResult();
        }

        public static QueryDescription GetQueryDescription(OperationContext context, Expression expression)
        {
            context.OperationName = Utility.GetNameFromLambda(expression);
            IEnumerable<object> parameterValues = Utility.GetParametersFromLambda(expression);

            DomainOperationEntry domainOperationEntry = context.DomainServiceDescription.GetQueryMethod(context.OperationName);
            if (domainOperationEntry == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.NoQueryOperation,
                    context.OperationName,
                    context.DomainServiceDescription.DomainServiceType));
            }

            return new QueryDescription(domainOperationEntry, parameterValues.ToArray());
        }

        public static InvokeDescription GetInvokeDescription(OperationContext context, Expression expression)
        {
            context.OperationName = Utility.GetNameFromLambda(expression);
            IEnumerable<object> parameterValues = Utility.GetParametersFromLambda(expression);

            DomainOperationEntry domainOperationEntry = context.DomainServiceDescription.GetInvokeOperation(context.OperationName);
            if (domainOperationEntry == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.NoInvokeOperation,
                    context.OperationName,
                    context.DomainServiceDescription.DomainServiceType));
            }

            return new InvokeDescription(domainOperationEntry, parameterValues.ToArray());
        }

        public static ChangeSetEntry GetCustomUpdateChangeSetEntry(OperationContext context, Expression expression, object original)
        {
            context.OperationName = Utility.GetNameFromLambda(expression);
            IEnumerable<object> parameterValues = Utility.GetParametersFromLambda(expression);
            object entity = parameterValues.First();

            ChangeSetEntry changeSetEntry = new ChangeSetEntry(Utility.DefaultChangeSetEntryId, entity, original, DomainOperation.Update);

            DomainOperationEntry domainOperationEntry = context.DomainServiceDescription.GetCustomMethod(entity.GetType(), context.OperationName);
            if (domainOperationEntry == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.NoCustomUpdateOperation,
                    context.OperationName,
                    context.DomainServiceDescription.DomainServiceType));
            }
            changeSetEntry.EntityActions = new EntityActionCollection
            {
                { context.OperationName, parameterValues.Skip(1).ToArray() },
            };

            return changeSetEntry;
        }

        public static ChangeSetEntry GetChangeSetEntry(OperationContext context, object entity, object original, DomainOperation operationType)
        {
            Utility.EnsureOperationSupported(context, entity.GetType(), operationType);
            context.OperationName = context.DomainServiceDescription.GetSubmitMethod(entity.GetType(), operationType).Name;
            return new ChangeSetEntry(Utility.DefaultChangeSetEntryId, entity, original, operationType);
        }

        public static ChangeSet CreateChangeSet(ChangeSetEntry entry)
        {
            return new ChangeSet(new[] { entry });
        }

        private static string GetNameFromLambda(Expression expression)
        {
            if (expression is LambdaExpression lambdaExpression)
            {
                if (lambdaExpression.Body is MethodCallExpression callExpression)
                    return RemoveAsyncFromName(callExpression.Method.Name);
                if (lambdaExpression.Body is MemberExpression memberExpression)
                    return RemoveAsyncFromName("Get" + memberExpression.Member.Name);
            }
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Resources.NoOperationName,
                expression));
        }

        private static string RemoveAsyncFromName(string name)
            => name.EndsWith("Async", StringComparison.Ordinal) ? name.Substring(0, name.Length - "Async".Length) : name;

        private static IEnumerable<object> GetParametersFromLambda(Expression expression)
        {
            LambdaExpression lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null)
            {
                MethodCallExpression methodCallExpression = lambdaExpression.Body as MethodCallExpression;
                if (methodCallExpression != null)
                {
                    return methodCallExpression.Arguments.Select(e => Expression.Lambda(e).Compile().DynamicInvoke());
                }

                MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    return Enumerable.Empty<object>();
                }
            }
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                Resources.NoParameterValues,
                expression));
        }

        private static void EnsureOperationSupported(OperationContext context, Type entityType, DomainOperation operationType)
        {
            if (!context.DomainServiceDescription.IsOperationSupported(entityType, operationType))
            {
                throw new DomainServiceTestHostException(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.OperationNotSupported,
                    operationType,
                    entityType,
                    context.DomainServiceDescription.DomainServiceType));
            }
        }
    }
}
