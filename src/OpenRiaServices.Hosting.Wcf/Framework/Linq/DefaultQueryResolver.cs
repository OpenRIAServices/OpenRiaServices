using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using OpenRiaServices.Server;

namespace System.Linq.Dynamic
{
    /// <summary>
    /// Default query resolver
    /// </summary>
    internal class DefaultQueryResolver : QueryResolver
    {
        /// <summary>
        /// Called to attempt to resolve unresolved member references during query deserialization.
        /// </summary>
        /// <param name="type">The Type the member is expected on.</param>
        /// <param name="member">The member name.</param>
        /// <param name="instance">The instance to form the MemberExpression on.</param>
        /// <returns>A MemberExpression if the member can be resolved, null otherwise.</returns>
        public override MemberExpression ResolveMember(Type type, string member, Expression instance)
        {
            MemberExpression mex = null;
            IDictionary<PropertyDescriptor, IncludeAttribute[]> entityIncludeMap = MetaType.GetMetaType(type).ProjectionMemberMap;

            if (entityIncludeMap.Any())
            {
                // Do a reverse lookup in the include map for the type, looking
                // for the source of the member (if present)
                PropertyDescriptor pd = null;
                IncludeAttribute projection = null;
                foreach (var entry in entityIncludeMap)
                {
                    projection = entry.Value.SingleOrDefault(p => p.IsProjection && p.MemberName == member);
                    if (projection != null)
                    {
                        pd = entry.Key;
                        break;
                    }
                }

                if (projection == null)
                {
                    return null;
                }

                // Found the source projection. Form the corresponding MemberExpression
                // for the projection path
                string[] pathMembers = projection.Path.Split('.');
                Type currType = type;
                MemberInfo memberInfo = currType.GetMember(pd.Name).Single();
                mex = Expression.MakeMemberAccess(instance, memberInfo);
                foreach (string pathMember in pathMembers)
                {
                    currType = mex.Type;
                    memberInfo = currType.GetMember(pathMember).Single();
                    mex = Expression.MakeMemberAccess(mex, memberInfo);
                }
            }

            return mex;
        }
    }
}