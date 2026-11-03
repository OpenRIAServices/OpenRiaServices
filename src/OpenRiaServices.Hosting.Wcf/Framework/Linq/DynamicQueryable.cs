using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRiaServices.Hosting.AspNetCore;

#nullable disable
#pragma warning disable CA1859 // Change return type of method 'GenerateStringConcat' from 'System.Linq.Expressions.Expression' to 'System.Linq.Expressions.MethodCallExpression' for improved performance

namespace System.Linq.Dynamic
{
#if NETFRAMEWORK
    static class StringExtensions
    {
        static public string Slice(this string str, int startIndex, int length)
            => str.Substring(startIndex, length);
    }
#endif

#nullable enable
    internal static class DynamicQueryable
    {
        public static IQueryable Where(this IQueryable source, string predicate, QueryResolver queryResolver, QueryLimits limits)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);
            LambdaExpression lambda = DynamicExpression.ParseLambda(source.ElementType, typeof(bool), predicate, queryResolver, limits);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Where",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Quote(lambda)));
        }

        public static IQueryable OrderBy(this IQueryable source, string ordering, QueryResolver queryResolver, QueryLimits limits)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(ordering);
            ArgumentNullException.ThrowIfNull(limits);
            ParameterExpression[] parameters = new ParameterExpression[] {
                Expression.Parameter(source.ElementType, "") };
            ExpressionParser parser = new ExpressionParser(parameters, ordering, queryResolver, limits);
            IEnumerable<DynamicOrdering> orderings = parser.ParseOrdering();
            Expression queryExpr = source.Expression;
            string methodAsc = "OrderBy";
            string methodDesc = "OrderByDescending";
            foreach (DynamicOrdering o in orderings)
            {
                queryExpr = Expression.Call(
                    typeof(Queryable), o.Ascending ? methodAsc : methodDesc,
                    new Type[] { source.ElementType, o.Selector.Type },
                    queryExpr, Expression.Quote(DynamicExpression.Lambda(o.Selector, parameters)));
                methodAsc = "ThenBy";
                methodDesc = "ThenByDescending";
            }
            ExpressionParser.ValidateExpression(queryExpr, limits.MaxExpressionDepth, QueryDeserializer.MaxQueryParts + 4);
            return source.Provider.CreateQuery(queryExpr);
        }

        public static IQueryable Take(this IQueryable source, int count)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Take",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            ArgumentNullException.ThrowIfNull(source);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Skip",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }
    }
#nullable disable

    internal static class DynamicExpression
    {
        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, QueryResolver queryResolver, QueryLimits limits)
        {
            return ParseLambda(new ParameterExpression[] { Expression.Parameter(itType, "") }, resultType, expression, queryResolver, limits);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type resultType, string expression, QueryResolver queryResolver, QueryLimits limits)
        {
            ExpressionParser parser = new ExpressionParser(parameters, expression, queryResolver, limits);
            return Lambda(parser.Parse(resultType), parameters);
        }

        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters)
        {
            int paramCount = parameters == null ? 0 : parameters.Length;
            Type[] typeArgs = new Type[paramCount + 1];
            for (int i = 0; i < paramCount; i++)
                typeArgs[i] = parameters[i].Type;
            typeArgs[paramCount] = body.Type;
            return Expression.Lambda(Expression.GetFuncType(typeArgs), body, parameters);
        }
    }

    internal class DynamicOrdering
    {
        public Expression Selector;
        public bool Ascending;
    }

    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "Exception is intended to only be used by the dynamic parser.")]
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Exception is intended to only be used by the dynamic parser.")]
    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Only used by parser")]
    internal class ParseException : Exception
    {
        public ParseException(string message, int position)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.ParseExceptionFormat, message, position))
        {
        }
    }

    // Test placement (paths relative to src):
    // OpenRiaServices.Client\Test\Client.Test\Data\QuerySerializationTests.cs covers both
    // client/server round trips and server parsing through QueryDeserializer, including expression-tree assertions.
    // Prefer that suite for client-query behavior and serialization regressions; check its existing coverage first.
    // OpenRiaServices.Hosting.Wcf\Test\Linq\OperatorResolutionTests.cs is for parser-only binding details
    // (exact operand types, selected overloads, conversions, and syntax the client serializer cannot emit).
    // Use QueryDeserializerTest.cs in the same directory for deserialization/member-visibility rules,
    // and ExpressionSecurityTests.cs for parser limits and rejected unsafe expressions.
    // The Linq tests are also linked into OpenRiaServices.Hosting.AspNetCore.Test.
    internal partial class ExpressionParser
    {
        /// <summary>The default maximum parser recursion and expression-tree depth.</summary>
        internal const int MaxExpressionDepth = 200;
        /// <summary>The maximum number of nodes in a client expression tree.</summary>
        internal const int MaxExpressionNodes = 4096;
        /// <summary>The maximum length of a client expression.</summary>
        internal const int MaxExpressionLength = 32768;

        readonly int _maxExpressionDepth;
        readonly QueryLimits _limits;
        int _expressionDepth;
        int _growingReplaceCalls;
        int _stringExpansionFactor = 1;

        void CheckStringExpansion(MethodInfo method, Expression[] args)
        {
            if (method.DeclaringType != typeof(string) || method.Name != nameof(string.Replace)
                || args.Length != 2 || args[0].Type != typeof(string))
                return;

            // The allowlist requires a constant replacement of at most 100 characters.
            int newLength = ((ConstantExpression)args[1]).Value is string replacement ? replacement.Length : 0;
            int oldLength = args[0] is ConstantExpression { Value: string oldValue } ? Math.Max(1, oldValue.Length) : 1;
            int factor = newLength / oldLength + (newLength % oldLength == 0 ? 0 : 1);
            if (factor <= 1)
                return;

            _stringExpansionFactor *= factor;

            if (++_growingReplaceCalls > 3 || _stringExpansionFactor > 100)
                throw ParseError(Resource.ExpressionTooComplex);
        }

        void EnterExpression()
        {
            if (_expressionDepth >= _maxExpressionDepth)
                throw ParseError(Resource.ExpressionTooComplex);
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (InsufficientExecutionStackException)
            {
                throw ParseError(Resource.ExpressionTooComplex);
            }
            _expressionDepth++;
        }

        /// <summary>Checks tree complexity without recursively walking untrusted expressions.</summary>
        internal static void ValidateExpression(Expression expression, int maxExpressionDepth = MaxExpressionDepth, int depthAllowance = 0)
        {
            new ComplexityValidator((long)maxExpressionDepth + depthAllowance).Validate(expression);
        }

        sealed class ComplexityValidator(long maxExpressionDepth) : ExpressionVisitor
        {
            readonly Queue<(Expression Expression, int Depth)> _pending = new();
            int _depth;
            int _nodes;

            public void Validate(Expression expression)
            {
                Visit(expression);
                while (_pending.Count != 0)
                {
                    var entry = _pending.Dequeue();
                    _depth = entry.Depth;
                    // Dispatch this node only; Visit queues its children instead of recursing.
                    base.Visit(entry.Expression);
                }
            }

            public override Expression Visit(Expression node)
            {
                if (node != null)
                {
                    if (_depth >= maxExpressionDepth || ++_nodes > MaxExpressionNodes)
                        throw new ParseException(Resource.ExpressionTooComplex, 0);
                    _pending.Enqueue((node, _depth + 1));
                }
                return node;
            }
        }

        struct Token(TokenId id, int pos, int length)
        {
            public readonly TokenId id = id;
            public readonly int pos = pos;
            public readonly int length = length;
        }

        enum TokenId
        {
            Unknown,
            End,
            Identifier,
            StringLiteral,
            IntegerLiteral,
            RealLiteral,
            Exclamation,
            Percent,
            Amphersand,
            OpenParen,
            CloseParen,
            Asterisk,
            Plus,
            Comma,
            Minus,
            Dot,
            Slash,
            Colon,
            LessThan,
            Equal,
            GreaterThan,
            Question,
            OpenBracket,
            CloseBracket,
            Bar,
            ExclamationEqual,
            DoubleAmphersand,
            LessThanEqual,
            LessGreater,
            DoubleEqual,
            GreaterThanEqual,
            DoubleBar
        }

#nullable enable
        /// <summary>
        /// A candidate overload for an operator, aggregate, or reflected member.
        /// Parameter types are captured once, so overload resolution does not need reflection.
        /// </summary>
        internal class Signature
        {
            public Signature(params Type[] parameterTypes)
            {
                ParameterTypes = parameterTypes;
            }

            public Type[] ParameterTypes { get; }
        }

        /// <summary>A reflected member signature, also used for separately validated indexer getters.</summary>
        internal class MemberSignature : Signature
        {
            public MemberSignature(MethodBase method, Type[] parameterTypes)
                : base(parameterTypes)
            {
                Method = method;
            }

            public MethodBase Method { get; }
        }

        /// <summary>An invocable method or constructor with its argument-independent permission rule.</summary>
        internal sealed class MethodSignature : MemberSignature
        {
            public MethodSignature(MethodBase method, Type[] parameterTypes, MemberRule rule)
                : base(method, parameterTypes)
            {
                Rule = rule;
            }

            public MemberRule Rule { get; }
        }

        /// <summary>The supported <see cref="Enumerable"/> aggregate methods and their selector/predicate types.</summary>
        sealed class AggregateSignatures(string name, Signature[] signatures)
        {
            public string Name { get; } = name;
            public Signature[] Signatures { get; } = signatures;
        }

        // Each operation has one flat candidate set. Construction groups are local and share
        // signatures, but never establish binding priority. Round-trip compatibility comes first.
        static readonly Signature[] s_logicalSignatures, s_arithmeticSignatures, s_relationalSignatures,
            s_equalitySignatures, s_addSignatures, s_subtractSignatures, s_negationSignatures, s_notSignatures;

        static ExpressionParser()
        {
            s_logicalSignatures = BinarySignatures(typeof(bool));
            s_arithmeticSignatures = BinarySignatures(
                typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal));
            Signature[] comparable = BinarySignatures(
                typeof(string), typeof(char), typeof(DateTime), typeof(TimeSpan), typeof(DateTimeOffset)
#if NET
                , typeof(DateOnly), typeof(TimeOnly)
#endif
                );
            s_relationalSignatures = [.. s_arithmeticSignatures, .. comparable];
            s_equalitySignatures = [.. s_relationalSignatures, .. BinarySignatures(typeof(bool), typeof(Guid))];
            Signature[] durations = PairSignatures(
                (typeof(DateTime), typeof(TimeSpan)), (typeof(TimeSpan), typeof(TimeSpan)),
                (typeof(DateTimeOffset), typeof(TimeSpan)));
            Signature[] differences = PairSignatures(
                (typeof(DateTime), typeof(DateTime)), (typeof(DateTimeOffset), typeof(DateTimeOffset))
#if NET
                , (typeof(TimeOnly), typeof(TimeOnly))
#endif
                );
            s_addSignatures = [.. s_arithmeticSignatures, .. durations];
            s_subtractSignatures = [.. s_addSignatures, .. differences];
            s_negationSignatures = UnarySignatures(typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal));
            s_notSignatures = UnarySignatures(typeof(bool));
        }

        static readonly FrozenDictionary<string, AggregateSignatures> s_aggregates = CreateAggregates();

        /// <summary>Creates (T) and (T?) signatures for each type.</summary>
        static Signature[] UnarySignatures(params Type[] types)
        {
            var signatures = new List<Signature>(types.Length * 2);
            foreach (Type type in types)
            {
                signatures.Add(new Signature(type));
                if (type.IsValueType)
                    signatures.Add(new Signature(MakeNullable(type)));
            }
            return signatures.ToArray();
        }

        /// <summary>Creates (T, T) and, for value types, (T?, T?) signatures for each type.</summary>
        static Signature[] BinarySignatures(params Type[] types)
        {
            var signatures = new List<Signature>(types.Length * 2);
            foreach (Type type in types)
            {
                signatures.Add(new Signature(type, type));
                if (type.IsValueType)
                    signatures.Add(new Signature(MakeNullable(type), MakeNullable(type)));
            }
            return signatures.ToArray();
        }

        /// <summary>Creates (L, R) and (L?, R?) signatures for each pair of value types.</summary>
        static Signature[] PairSignatures(params (Type Left, Type Right)[] pairs)
        {
            var signatures = new List<Signature>(pairs.Length * 2);
            foreach ((Type left, Type right) in pairs)
            {
                signatures.Add(new Signature(left, right));
                signatures.Add(new Signature(MakeNullable(left), MakeNullable(right)));
            }
            return signatures.ToArray();
        }

        static Type MakeNullable(Type type) => typeof(Nullable<>).MakeGenericType(type);

        static FrozenDictionary<string, AggregateSignatures> CreateAggregates()
        {
            Type[][] numericSelectors =
            [
                [typeof(int)], [typeof(int?)], [typeof(long)], [typeof(long?)], [typeof(float)], [typeof(float?)],
                [typeof(double)], [typeof(double?)], [typeof(decimal)], [typeof(decimal?)]
            ];
            Type[] noArguments = Type.EmptyTypes;
            Type[] predicate = [typeof(bool)];
            Type[] selector = [typeof(object)];

            AggregateSignatures[] aggregates =
            [
                Aggregate("Where", predicate),
                Aggregate("Any", noArguments, predicate),
                Aggregate("All", predicate),
                Aggregate("Count", noArguments, predicate),
                Aggregate("Min", selector),
                Aggregate("Max", selector),
                Aggregate("Sum", numericSelectors),
                Aggregate("Average", numericSelectors),
            ];
            return aggregates.ToFrozenDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

            static AggregateSignatures Aggregate(string name, params Type[][] parameterLists)
                => new(name, Array.ConvertAll(parameterLists, parameters => new Signature(parameters)));
        }

        /// <summary>
        /// Property, field and indexer metadata, independent of method invocation policies.
        /// Property and field lookups only include members declared on the type itself.
        /// The cache is keyed only by type, never by (untrusted) member names, so its size is bounded by the types in use.
        /// </summary>
        sealed class DataMembers
        {
            const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
            const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static;

            public DataMembers(Type type)
            {
                Hierarchy = SelfAndBaseTypes(type).ToArray();
                InstanceFieldsAndProperties = GroupFieldsAndProperties(type, InstanceFlags);
                StaticFieldsAndProperties = GroupFieldsAndProperties(type, StaticFlags);
                Indexers = GetIndexers(type);
            }

            /// <summary>The type itself followed by its base classes, or all inherited interfaces for interface types.</summary>
            public Type[] Hierarchy { get; }
            public FrozenDictionary<string, (MemberInfo Member, bool IsIndexer)> InstanceFieldsAndProperties { get; }
            public FrozenDictionary<string, (MemberInfo Member, bool IsIndexer)> StaticFieldsAndProperties { get; }
            /// <summary>Getters of the default members (indexers), including inherited ones.</summary>
            public MemberSignature[] Indexers { get; }

            static FrozenDictionary<string, (MemberInfo Member, bool IsIndexer)> GroupFieldsAndProperties(Type type, BindingFlags flags)
            {
                // The first member wins when names only differ by case, matching the order returned by FindMembers.
                var members = new Dictionary<string, (MemberInfo Member, bool IsIndexer)>(StringComparer.OrdinalIgnoreCase);
                foreach (MemberInfo member in type.FindMembers(MemberTypes.Property | MemberTypes.Field, flags, null, null))
                {
                    if (!members.ContainsKey(member.Name))
                        members.Add(member.Name, (member, member is PropertyInfo property && property.GetIndexParameters().Length != 0));
                }
                return members.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            }

            static MemberSignature[] GetIndexers(Type type)
            {
                var indexers = new List<MemberSignature>();
                foreach (MemberInfo member in type.GetDefaultMembers())
                {
                    if (member is PropertyInfo property && property.GetGetMethod() is MethodInfo getter)
                    {
                        Type[] parameters = Array.ConvertAll(getter.GetParameters(), p => p.ParameterType);
                        if (!parameters.Any(p => p.IsByRef))
                            indexers.Add(new MemberSignature(getter, parameters));
                    }
                }
                return indexers.ToArray();
            }
        }

         static readonly ConditionalWeakTable<Type, DataMembers> s_dataMembers = new();

        static DataMembers GetDataMembers(Type type)
            => s_dataMembers.GetValue(type, static t => new DataMembers(t));

#nullable disable

        static readonly FrozenSet<Type> s_predefinedTypeSet = new Type[] {
            typeof(Object),
            typeof(Boolean),
            typeof(Char),
            typeof(String),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
#if NET
            typeof(DateOnly),
            typeof(TimeOnly),
#endif
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Math),
            typeof(Convert),
            typeof(StringComparison),
            typeof(Uri)
        }.ToFrozenSet();

        static readonly MethodInfo s_stringCompare = typeof(string).GetMethod(nameof(string.Compare), new[] { typeof(string), typeof(string) });
        static readonly MethodInfo s_concatObjects = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(object), typeof(object) });

        static readonly ConstantExpression TrueLiteral = Expression.Constant(true);
        static readonly ConstantExpression FalseLiteral = Expression.Constant(false);
        static readonly ConstantExpression NullLiteral = Expression.Constant(null);

        const string KeywordIt = "it";
        const string KeywordIif = "iif";

        static readonly FrozenDictionary<string, object> s_keywords = CreateKeywords();

        readonly Dictionary<string, object> _symbols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<ConstantExpression, ReadOnlyMemory<char>> _literals = new();
        ParameterExpression _it;
        readonly string _text;
        int _textPos;
        char _ch;
        TokenId _tokenId;
        int _tokenStart;
        readonly QueryResolver _queryResolver;

        Token CurrentToken => new Token(_tokenId, _tokenStart, _textPos - _tokenStart);

        ReadOnlySpan<char> GetText(Token token) =>
            _text.AsSpan(token.pos, token.length);

        string GetString(Token token)
            => _text.Substring(token.pos, token.length);


        public ExpressionParser(ParameterExpression[] parameters, string expression, QueryResolver queryResolver, QueryLimits limits)
        {
            ArgumentNullException.ThrowIfNull(expression);
            ArgumentNullException.ThrowIfNull(limits);

            _limits = limits;
            _maxExpressionDepth = _limits.MaxExpressionDepth;
            if (expression.Length > MaxExpressionLength)
                throw new ParseException(Resource.ExpressionTooComplex, 0);
            this._queryResolver = queryResolver;
            if (parameters != null)
                ProcessParameters(parameters);
            _text = expression;
            SetTextPos(0);
            NextToken();
        }

        void ProcessParameters(ParameterExpression[] parameters)
        {
            foreach (ParameterExpression pe in parameters)
                if (!string.IsNullOrEmpty(pe.Name))
                    AddSymbol(pe.Name, pe);
            if (parameters.Length == 1 && string.IsNullOrEmpty(parameters[0].Name))
                _it = parameters[0];
        }

        void AddSymbol(string name, object value)
        {
            if (!_symbols.TryAdd(name, value))
                throw ParseError(Resource.DuplicateIdentifier, name);
        }

        public Expression Parse(Type resultType)
        {
            int exprPos = CurrentToken.pos;
            Expression expr = ParseExpression();
            ValidateExpression(expr, _maxExpressionDepth);
            if (resultType != null)
                if ((expr = PromoteExpression(expr, resultType, true)) == null)
                    throw ParseError(exprPos, Resource.ExpressionTypeMismatch, GetTypeName(resultType));
            ValidateToken(TokenId.End, Resource.SyntaxError);
            return expr;
        }

        public IEnumerable<DynamicOrdering> ParseOrdering()
        {
            List<DynamicOrdering> orderings = new List<DynamicOrdering>();
            while (true)
            {
                Expression expr = ParseExpression();
                ValidateExpression(expr, _maxExpressionDepth);
                bool ascending = true;
                if (_tokenId == TokenId.Identifier)
                {
                    if (TokenIdentifierIs("asc") || TokenIdentifierIs("ascending"))
                    {
                        NextToken();
                    }
                    else if (TokenIdentifierIs("desc") || TokenIdentifierIs("descending"))
                    {
                        NextToken();
                        ascending = false;
                    }
                }

                orderings.Add(new DynamicOrdering
                {
                    Selector = expr,
                    Ascending = ascending
                });
                if (CurrentToken.id != TokenId.Comma)
                    break;
                NextToken();
            }
            ValidateToken(TokenId.End, Resource.SyntaxError);
            return orderings;
        }

        // ?: operator
        Expression ParseExpression()
        {
            EnterExpression();
            try
            {
                int errorPos = CurrentToken.pos;
                Expression expr = ParseLogicalOr();
                if (CurrentToken.id == TokenId.Question)
                {
                    NextToken();
                    Expression expr1 = ParseExpression();
                    ValidateToken(TokenId.Colon, Resource.ColonExpected);
                    NextToken();
                    Expression expr2 = ParseExpression();
                    expr = GenerateConditional(expr, expr1, expr2, errorPos);
                }
                return expr;
            }
            finally
            {
                _expressionDepth--;
            }
        }

        // ||, or operator
        Expression ParseLogicalOr()
        {
            Expression left = ParseLogicalAnd();
            while (CurrentToken.id == TokenId.DoubleBar || TokenIdentifierIs("or"))
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseLogicalAnd();
                CheckAndPromoteOperands(s_logicalSignatures, op, ref left, ref right);
                left = Expression.OrElse(left, right);
            }
            return left;
        }

        // &&, and operator
        Expression ParseLogicalAnd()
        {
            Expression left = ParseHasOperator();
            while (CurrentToken.id == TokenId.DoubleAmphersand || TokenIdentifierIs("and"))
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseHasOperator();
                CheckAndPromoteOperands(s_logicalSignatures, op, ref left, ref right);
                left = Expression.AndAlso(left, right);
            }
            return left;
        }

        // Is this the correct place for this operator?
        // Either we place it here after conditional operators (called LogicalAnd here) and before comparison
        // * Or we place it after multiplicative but before primary function
        // * Or we treat it as a function and treat it as primary function
        // Either way it will always work with the OpenRiaServices client since it will add parenthesis, so it should not be much of an issue
        Expression ParseHasOperator()
        {
            Expression left = ParseComparison();
            while (TokenIdentifierIs("has"))
            {
                Token op = CurrentToken;
                NextToken();

                Type enumType = left.Type;

                // The right hand side can either be a constant such as EnumName.EnumValue, 1 or an expression
                // we only need to make special arrangement to handle the first case since case 2 and 3 is handled by normal parsing
                Expression right;
                if (TokenIdentifierIs(enumType.Name))
                {
                    // Remove identifier enumType.Name
                    NextToken();

                    //Verify next is dot and then remove it
                    if (CurrentToken.id != TokenId.Dot)
                        throw new ParseException("Expected a dot", CurrentToken.pos);
                    NextToken();

                    // Read the enum field name, parse it and remove it
                    if (CurrentToken.id != TokenId.Identifier)
                        throw new ParseException("Expected an Identifier", CurrentToken.pos);
                    right = Expression.Constant(ParseEnum(GetText(CurrentToken), enumType), enumType);
                    NextToken();
                }
                else // Either numeric value or member access
                {
                    right = ParseComparison();
                }

                left = ConvertEnumExpression(left, right);
                right = ConvertEnumExpression(right, left);

                CheckAndPromoteOperands(s_arithmeticSignatures, op, ref left, ref right);

                // Treat as (left & right) == right which is the same behaviour as calling Enum.HasFlag
                // but it will work with entity framework and probably most other query providers
                left = Expression.Equal(Expression.And(left, right), right);
            }
            return left;
        }

        // =, ==, !=, <>, >, >=, <, <= operators
        Expression ParseComparison()
        {
            Expression left = ParseAdditive();

            while (CurrentToken.id is TokenId.Equal or TokenId.DoubleEqual or
                TokenId.ExclamationEqual or TokenId.LessGreater or
                TokenId.GreaterThan or TokenId.GreaterThanEqual or
                TokenId.LessThan or TokenId.LessThanEqual)
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseAdditive();
                bool isEquality = op.id is TokenId.Equal or TokenId.DoubleEqual or
                    TokenId.ExclamationEqual or TokenId.LessGreater;
                if (isEquality && !left.Type.IsValueType && !right.Type.IsValueType)
                {
                    if (left.Type != right.Type)
                    {
                        if (left.Type.IsAssignableFrom(right.Type))
                        {
                            right = Expression.Convert(right, left.Type);
                        }
                        else if (right.Type.IsAssignableFrom(left.Type))
                        {
                            left = Expression.Convert(left, right.Type);
                        }
                        else
                        {
                            throw IncompatibleOperandsError(GetString(op), left, right, op.pos);
                        }
                    }
                }
                else
                {
                    if (IsEnumType(left.Type) || IsEnumType(right.Type))
                    {
                        // convert enum expressions to their underlying values for comparison
                        left = ConvertEnumExpression(left, right);
                        right = ConvertEnumExpression(right, left);
                    }

                    CheckAndPromoteOperands(isEquality ? s_equalitySignatures : s_relationalSignatures,
                        op, ref left, ref right);
                }
                left = op.id switch
                {
                    TokenId.Equal or TokenId.DoubleEqual => Expression.Equal(left, right),
                    TokenId.ExclamationEqual or TokenId.LessGreater => Expression.NotEqual(left, right),
                    TokenId.GreaterThan => GenerateComparison(ExpressionType.GreaterThan, left, right),
                    TokenId.GreaterThanEqual => GenerateComparison(ExpressionType.GreaterThanOrEqual, left, right),
                    TokenId.LessThan => GenerateComparison(ExpressionType.LessThan, left, right),
                    TokenId.LessThanEqual => GenerateComparison(ExpressionType.LessThanOrEqual, left, right),
                    _ => throw new InvalidOperationException()
                };
            }
            return left;
        }

        /// <summary>
        /// We perform comparisons against enums using the underlying type
        /// because a more complete set of comparisons can be performed.
        /// </summary>
        static Expression ConvertEnumExpression(Expression expr, Expression otherExpr)
        {
            if (!IsEnumType(expr.Type))
            {
                return expr;
            }

            Type underlyingType;
            if (IsNullableType(expr.Type) ||
                (otherExpr.NodeType == ExpressionType.Constant && ((ConstantExpression)otherExpr).Value == null))
            {
                // if the enum expression itself is nullable or is being compared against null
                // we use a nullable type
                underlyingType = typeof(Nullable<>).MakeGenericType(Enum.GetUnderlyingType(GetNonNullableType(expr.Type)));
            }
            else
            {
                underlyingType = Enum.GetUnderlyingType(expr.Type);
            }

            return Expression.Convert(expr, underlyingType);
        }

        // +, -, & operators
        Expression ParseAdditive()
        {
            Expression left = ParseMultiplicative();
            while (CurrentToken.id is TokenId.Plus or TokenId.Minus or
                TokenId.Amphersand)
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseMultiplicative();
                switch (op.id)
                {
                    case TokenId.Plus:
                        if (left.Type == typeof(string) || right.Type == typeof(string))
                            goto case TokenId.Amphersand;
                        CheckAndPromoteOperands(s_addSignatures, op, ref left, ref right);
                        left = Expression.Add(left, right);
                        break;
                    case TokenId.Minus:
                        CheckAndPromoteOperands(s_subtractSignatures, op, ref left, ref right);
                        left = Expression.Subtract(left, right);
                        break;
                    case TokenId.Amphersand:
                        left = GenerateStringConcat(left, right);
                        break;
                }
            }
            return left;
        }

        // *, /, %, mod operators
        Expression ParseMultiplicative()
        {
            Expression left = ParseUnary();
            while (CurrentToken.id == TokenId.Asterisk || CurrentToken.id == TokenId.Slash ||
                CurrentToken.id == TokenId.Percent || TokenIdentifierIs("mod"))
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseUnary();
                CheckAndPromoteOperands(s_arithmeticSignatures, op, ref left, ref right);
                switch (op.id)
                {
                    case TokenId.Asterisk:
                        left = Expression.Multiply(left, right);
                        break;
                    case TokenId.Slash:
                        left = Expression.Divide(left, right);
                        break;
                    case TokenId.Percent:
                    case TokenId.Identifier:
                        left = Expression.Modulo(left, right);
                        break;
                }
            }
            return left;
        }

        // -, !, not unary operators
        Expression ParseUnary()
        {
            if (CurrentToken.id == TokenId.Minus || CurrentToken.id == TokenId.Exclamation ||
                TokenIdentifierIs("not"))
            {
                Token op = CurrentToken;
                NextToken();
                if (op.id == TokenId.Minus && (CurrentToken.id == TokenId.IntegerLiteral ||
                    CurrentToken.id == TokenId.RealLiteral))
                {
                    // Reset token start to also include "-" sign
                    _tokenStart = op.pos;
                    return ParsePrimary();
                }
                Expression expr;
                EnterExpression();
                try
                {
                    expr = ParseUnary();
                }
                finally
                {
                    _expressionDepth--;
                }
                if (op.id == TokenId.Minus)
                {
                    CheckAndPromoteOperand(s_negationSignatures, op, ref expr);
                    expr = Expression.Negate(expr);
                }
                else
                {
                    CheckAndPromoteOperand(s_notSignatures, op, ref expr);
                    expr = Expression.Not(expr);
                }
                return expr;
            }
            return ParsePrimary();
        }

        Expression ParsePrimary()
        {
            Expression expr = ParsePrimaryStart();
            while (true)
            {
                if (CurrentToken.id == TokenId.Dot)
                {
                    NextToken();
                    expr = ParseMemberAccess(null, expr);
                }
                else if (CurrentToken.id == TokenId.OpenBracket)
                {
                    expr = ParseElementAccess(expr);
                }
                else
                {
                    break;
                }
            }
            return expr;
        }

        Expression ParsePrimaryStart()
        {
            switch (CurrentToken.id)
            {
                case TokenId.Identifier:
                    return ParseIdentifier();
                case TokenId.StringLiteral:
                    return ParseStringLiteral();
                case TokenId.IntegerLiteral:
                    return ParseIntegerLiteral();
                case TokenId.RealLiteral:
                    return ParseRealLiteral();
                case TokenId.OpenParen:
                    return ParseParenExpression();
                default:
                    throw ParseError(Resource.ExpressionExpected);
            }
        }

        Expression ParseStringLiteral()
        {
            ValidateToken(TokenId.StringLiteral);
            var text = GetText(CurrentToken);
            char quote = text[0];
            // Unwrap string (remove surrounding quotes) and unwrap backslashes.
            string s = text.Slice(1, text.Length - 2).ToString().Replace("\\\\", "\\");
            if (quote == '\'')
            {
                // Unwrap quotes.
                s = s.Replace("\\\'", "\'");
                if (s.Length != 1)
                    throw ParseError(Resource.InvalidCharacterLiteral);
                NextToken();
                return CreateLiteral(s[0], s);
            }
            else
            {
                // Unwrap quotes.
                s = s.Replace("\\\"", "\"");
            }

            NextToken();
            return CreateLiteral(s, s);
        }

        Expression ParseIntegerLiteral()
        {
            Token token = CurrentToken;
            ValidateToken(TokenId.IntegerLiteral);
#if NET
            ReadOnlySpan<char> text = GetText(token);
#else
            string text = GetString(token);
#endif
            if (text[0] != '-')
            {
                ulong value;
                if (!ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
                    throw ParseError(Resource.InvalidIntegerLiteral, text.ToString());
                NextToken();
                if (value <= (ulong)int.MaxValue)
                    return CreateLiteral((int)value, token);
                if (value <= (ulong)uint.MaxValue)
                    return CreateLiteral((uint)value, token);
                if (value <= (ulong)long.MaxValue)
                    return CreateLiteral((long)value, token);
                return CreateLiteral(value, token);
            }
            else
            {
                long value;
                if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                    throw ParseError(Resource.InvalidIntegerLiteral, text.ToString());
                NextToken();
                if (value >= int.MinValue && value <= int.MaxValue)
                    return CreateLiteral((int)value, token);
                return CreateLiteral(value, token);
            }
        }

        Expression ParseRealLiteral()
        {
            ValidateToken(TokenId.RealLiteral);
            Token token = CurrentToken;
#if NET
            ReadOnlySpan<char> text = GetText(token);
#else
            string text = GetString(token);
#endif
            object value = null;
            char last = text[text.Length - 1];
            if (last is 'F' or 'f')
            {
                float f;
                if (float.TryParse(text.Slice(0, text.Length - 1), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out f))
                    value = f;
            }
            else if (last is 'M' or 'm')
            {
                decimal m;
                if (decimal.TryParse(text.Slice(0, text.Length - 1), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out m))
                    value = m;
            }
            else if (last is 'D' or 'd')
            {
                double d;
                if (double.TryParse(text.Slice(0, text.Length - 1), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out d))
                    value = d;
            }
            else
            {
                double d;
                if (double.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out d))
                    value = d;
            }
            if (value == null)
                throw ParseError(Resource.InvalidRealLiteral, text.ToString());
            NextToken();
            return CreateLiteral(value, token);
        }

        Expression CreateLiteral(object value, string valueAsString)
        {
            ConstantExpression expr = Expression.Constant(value);
            _literals.Add(expr, valueAsString.AsMemory());
            return expr;
        }
        Expression CreateLiteral(object value, Token token)
        {
            ConstantExpression expr = Expression.Constant(value);
            _literals.Add(expr, _text.AsMemory(token.pos, token.length));
            return expr;
        }

        Expression ParseParenExpression()
        {
            ValidateToken(TokenId.OpenParen, Resource.OpenParenExpected);
            NextToken();
            Expression e = ParseExpression();
            ValidateToken(TokenId.CloseParen, Resource.CloseParenOrOperatorExpected);
            NextToken();
            return e;
        }

        Expression ParseIdentifier()
        {
            ValidateToken(TokenId.Identifier);
            object value;
            string text = GetString(CurrentToken);

            if (s_keywords.TryGetValue(text, out value))
            {
                if (value is Type type)
                    return ParseTypeAccess(type);
                if (object.ReferenceEquals(value, KeywordIt))
                    return ParseIt();
                if (object.ReferenceEquals(value, KeywordIif))
                    return ParseIif();
                NextToken();
                return (Expression)value;
            }
            if (_symbols.TryGetValue(text, out value))
            {
                Expression expr = value as Expression;
                if (expr == null)
                {
                    expr = Expression.Constant(value);
                }
                NextToken();
                return expr;
            }
            if (_it != null)
                return ParseMemberAccess(null, _it);
            throw ParseError(Resource.UnknownIdentifier, text);
        }

        Expression ParseIt()
        {
            if (_it == null)
                throw ParseError(Resource.NoItInScope);
            NextToken();
            return _it;
        }

        Expression ParseIif()
        {
            int errorPos = CurrentToken.pos;
            NextToken();
            Expression[] args = ParseArgumentList();
            if (args.Length != 3)
                throw ParseError(errorPos, Resource.IifRequiresThreeArgs);
            return GenerateConditional(args[0], args[1], args[2], errorPos);
        }

        Expression GenerateConditional(Expression test, Expression expr1, Expression expr2, int errorPos)
        {
            if (test.Type != typeof(bool))
                throw ParseError(errorPos, Resource.FirstExprMustBeBool);
            if (expr1.Type != expr2.Type)
            {
                Expression expr1as2 = expr2 != NullLiteral ? PromoteExpression(expr1, expr2.Type, true) : null;
                Expression expr2as1 = expr1 != NullLiteral ? PromoteExpression(expr2, expr1.Type, true) : null;
                if (expr1as2 != null && expr2as1 == null)
                {
                    expr1 = expr1as2;
                }
                else if (expr2as1 != null && expr1as2 == null)
                {
                    expr2 = expr2as1;
                }
                else
                {
                    string type1 = expr1 != NullLiteral ? expr1.Type.Name : "null";
                    string type2 = expr2 != NullLiteral ? expr2.Type.Name : "null";
                    if (expr1as2 != null && expr2as1 != null)
                        throw ParseError(errorPos, Resource.BothTypesConvertToOther, type1, type2);
                    throw ParseError(errorPos, Resource.NeitherTypeConvertsToOther, type1, type2);
                }
            }
            return Expression.Condition(test, expr1, expr2);
        }

        Expression ParseTypeAccess(Type type)
        {
            int errorPos = CurrentToken.pos;
            NextToken();
            if (CurrentToken.id == TokenId.Question)
            {
                if (!type.IsValueType || IsNullableType(type))
                    throw ParseError(errorPos, Resource.TypeHasNoNullableForm, GetTypeName(type));
                type = typeof(Nullable<>).MakeGenericType(type);
                NextToken();
            }
            if (CurrentToken.id == TokenId.OpenParen)
            {
                Expression[] args = ParseArgumentList();
                switch (FindBestMethod(GetInvocationMembers(type).Constructors, args, out MethodSignature constructor))
                {
                    case 0:
                        if (args.Length == 1)
                            return GenerateConversion(args[0], type, errorPos);
                        throw ParseError(errorPos, Resource.NoMatchingConstructor, GetTypeName(type));
                    case 1:
                        MethodBase method = constructor.Method;
                        if (!ApplyMemberRule(constructor.Rule, args, _limits))
                            throw ParseError(errorPos, Resource.MethodsAreInaccessible, GetTypeName(method.DeclaringType));

                        return Expression.New((ConstructorInfo)method, args);
                    default:
                        throw ParseError(errorPos, Resource.AmbiguousConstructorInvocation, GetTypeName(type));
                }
            }
            ValidateToken(TokenId.Dot, Resource.DotOrOpenParenExpected);
            NextToken();
            return ParseMemberAccess(type, null);
        }

        static Expression GenerateConversion(Expression expr, Type type, int errorPos)
        {
            Type exprType = expr.Type;
            if (exprType == type)
                return expr;
            if (exprType.IsValueType && type.IsValueType)
            {
                if ((IsNullableType(exprType) || IsNullableType(type)) &&
                    GetNonNullableType(exprType) == GetNonNullableType(type))
                    return Expression.Convert(expr, type);
                if ((IsNumericType(exprType) || IsEnumType(exprType)) &&
                    (IsNumericType(type) || IsEnumType(type)))
                    return Expression.ConvertChecked(expr, type);
            }
            if (exprType.IsAssignableFrom(type) || type.IsAssignableFrom(exprType) ||
                exprType.IsInterface || type.IsInterface)
                return Expression.Convert(expr, type);
            throw ParseError(errorPos, Resource.CannotConvertValue,
                GetTypeName(exprType), GetTypeName(type));
        }

        Expression ParseMemberAccess(Type type, Expression instance)
        {
            if (instance != null)
                type = instance.Type;
            int errorPos = CurrentToken.pos;
            string id = GetIdentifier();
            NextToken();
            if (CurrentToken.id == TokenId.OpenParen)
            {
                if (instance != null && type != typeof(string))
                {
                    if (FindEnumerableType(type) is { } enumerableType)
                    {
                        Type elementType = enumerableType.GetGenericArguments()[0];
                        return ParseAggregate(instance, elementType, id, errorPos);
                    }
                }
                Expression[] args = ParseArgumentList();
                switch (FindMethod(type, id, instance == null, args, out MethodSignature signature))
                {
                    case 0:
                        throw ParseError(errorPos, Resource.NoApplicableMethod,
                            id, GetTypeName(type));
                    case 1:
                        MethodInfo method = (MethodInfo)signature.Method;
                        if (method.ReturnType == typeof(void))
                            throw ParseError(errorPos, Resource.MethodIsVoid, id, GetTypeName(method.DeclaringType));
                        if (!ApplyMemberRule(signature.Rule, args, _limits))
                            throw ParseError(errorPos, Resource.MethodsAreInaccessible, GetTypeName(method.DeclaringType));
                        CheckStringExpansion(method, args);
                        return Expression.Call(instance, (MethodInfo)method, args);
                    default:
                        throw ParseError(errorPos, Resource.AmbiguousMethodInvocation,
                            id, GetTypeName(type));
                }
            }
            else
            {
                MemberInfo member = FindPropertyOrField(type, id, instance == null, out bool isIndexer);
                // Prevent "getting" an indexer, such as it.Item == ''
                if (isIndexer)
                    throw ParseError(errorPos, Resource.UnknownPropertyOrField, id, GetTypeName(type));
                if (member == null)
                {
                    if (this._queryResolver != null)
                    {
                        MemberExpression mex = _queryResolver.ResolveMember(type, id, instance);
                        if (mex != null)
                        {
                            return mex;
                        }
                    }
                    throw ParseError(errorPos, Resource.UnknownPropertyOrField,
                        id, GetTypeName(type));
                }
                return member is PropertyInfo ?
                    Expression.Property(instance, (PropertyInfo)member) :
                    Expression.Field(instance, (FieldInfo)member);
            }
        }

#nullable enable
        private static readonly ConcurrentDictionary<Type, Type?> s_enumerableTypeLookup = new();

        /// <summary>
        /// See if <<paramref name="type"/> is <see cref="IEnumerable{T}"/> for any T and if so returns the actual <see cref="IEnumerable{T}"/> type.
        /// </summary>
        /// <returns>The matching constructed <see cref="IEnumerable{T}"/> type, or <see langword="null"/> if the type is not an <see cref="IEnumerable{T}"/> .</returns>
        private static Type? FindEnumerableType(Type type)
        {
            return s_enumerableTypeLookup.GetOrAdd(type, static x => FindEnumerable(x));

            static Type? FindEnumerable(Type? type)
            {
                while (type != null && type != typeof(object))
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        return type;
                    foreach (Type intfType in type.GetInterfaces())
                    {
                        Type? found = FindEnumerable(intfType);
                        if (found != null)
                            return found;
                    }
                    type = type.BaseType;
                }
                return null;
            }
        }
#nullable disable

        Expression ParseAggregate(Expression instance, Type elementType, string methodName, int errorPos)
        {
            ParameterExpression outerIt = _it;
            ParameterExpression innerIt = Expression.Parameter(elementType, "");
            _it = innerIt;
            Expression[] args = ParseArgumentList();
            _it = outerIt;
            if (!s_aggregates.TryGetValue(methodName, out AggregateSignatures aggregate)
                || FindBestMethod(aggregate.Signatures, args, out _) != 1)
                throw ParseError(errorPos, Resource.NoApplicableAggregate, methodName);
            Type[] typeArgs;
            if (aggregate.Name is "Min" or "Max")
            {
                typeArgs = new Type[] { elementType, args[0].Type };
            }
            else
            {
                typeArgs = new Type[] { elementType };
            }
            if (args.Length == 0)
            {
                args = new Expression[] { instance };
            }
            else
            {
                args = new Expression[] { instance, DynamicExpression.Lambda(args[0], innerIt) };
            }
            return Expression.Call(typeof(Enumerable), aggregate.Name, typeArgs, args);
        }

        Expression[] ParseArgumentList()
        {
            ValidateToken(TokenId.OpenParen, Resource.OpenParenExpected);
            NextToken();
            Expression[] args = CurrentToken.id != TokenId.CloseParen ? ParseArguments() : Array.Empty<Expression>();
            ValidateToken(TokenId.CloseParen, Resource.CloseParenOrCommaExpected);
            NextToken();
            return args;
        }

        Expression[] ParseArguments()
        {
            List<Expression> argList = new List<Expression>();
            while (true)
            {
                argList.Add(ParseExpression());
                if (CurrentToken.id != TokenId.Comma)
                    break;
                NextToken();
            }
            return argList.ToArray();
        }

        Expression ParseElementAccess(Expression expr)
        {
            int errorPos = CurrentToken.pos;
            ValidateToken(TokenId.OpenBracket, Resource.OpenParenExpected);
            NextToken();
            Expression[] args = ParseArguments();
            ValidateToken(TokenId.CloseBracket, Resource.CloseBracketOrCommaExpected);
            NextToken();
            if (expr.Type.IsArray)
            {
                if (expr.Type.GetArrayRank() != 1 || args.Length != 1)
                    throw ParseError(errorPos, Resource.CannotIndexMultiDimArray);
                Expression index = PromoteExpression(args[0], typeof(int), true);
                if (index == null)
                    throw ParseError(errorPos, Resource.InvalidIndex);
                return Expression.ArrayIndex(expr, index);
            }
            else
            {
                MethodBase mb;
                switch (FindIndexer(expr.Type, args, out mb))
                {
                    case 0:
                        throw ParseError(errorPos, Resource.NoApplicableIndexer,
                            GetTypeName(expr.Type));
                    case 1:
                        return Expression.Call(expr, (MethodInfo)mb, args);
                    default:
                        throw ParseError(errorPos, Resource.AmbiguousIndexerInvocation,
                            GetTypeName(expr.Type));
                }
            }
        }

        static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static Type GetNonNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        internal static string GetTypeName(Type type)
        {
            Type baseType = GetNonNullableType(type);
            string s = baseType.Name;
            if (type != baseType)
                s += '?';
            return s;
        }

        static bool IsNumericType(Type type)
        {
            return GetNumericTypeKind(type) != 0;
        }

        static bool IsSignedIntegralType(Type type)
        {
            return GetNumericTypeKind(type) == 2;
        }

        static bool IsUnsignedIntegralType(Type type)
        {
            return GetNumericTypeKind(type) == 3;
        }

        static int GetNumericTypeKind(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
                return 0;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return 1;
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return 2;
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return 3;
                default:
                    return 0;
            }
        }

        static bool IsEnumType(Type type)
        {
            return GetNonNullableType(type).IsEnum;
        }

        void CheckAndPromoteOperand(Signature[] signatures, Token op, ref Expression expr)
        {
            Expression[] args = new Expression[] { expr };
            if (FindOperator(signatures, args) != 1)
                throw ParseError(op.pos, Resource.IncompatibleOperand,
                    GetString(op), GetTypeName(args[0].Type));
            expr = args[0];
        }

        void CheckAndPromoteOperands(Signature[] signatures, Token op, ref Expression left, ref Expression right)
        {
            Expression[] args = new Expression[] { left, right };
            // Older clients erase null types. Preserve their date-difference interpretation;
            // typed nulls from newer clients distinguish date differences from duration subtraction.
            Type leftType = GetNonNullableType(left.Type);
            if (signatures == s_subtractSignatures && right == NullLiteral
                && (leftType == typeof(DateTime) || leftType == typeof(DateTimeOffset)))
                args[1] = Expression.Constant(null, MakeNullable(leftType));
            if (FindOperator(signatures, args) != 1)
                throw IncompatibleOperandsError(GetString(op), left, right, op.pos);
            left = args[0];
            right = args[1];
        }

        /// <summary>
        /// Resolves an operator against <paramref name="signatures"/>, promoting <paramref name="args"/> when a single best match is found.
        /// </summary>
        /// <returns>0 for no match, 1 for success, or more than 1 for ambiguity.</returns>
        int FindOperator(Signature[] signatures, Expression[] args)
        {
            return FindBestMethod(signatures, args, out _, preferOrdinaryConversions: true);
        }

        static Exception IncompatibleOperandsError(string opName, Expression left, Expression right, int pos)
        {
            return ParseError(pos, Resource.IncompatibleOperands,
                opName, GetTypeName(left.Type), GetTypeName(right.Type));
        }

        static MemberInfo FindPropertyOrField(Type type, string memberName, bool staticAccess, out bool isIndexer)
        {
            foreach (Type t in GetDataMembers(type).Hierarchy)
            {
                DataMembers members = GetDataMembers(t);
                var fieldsAndProperties = staticAccess ? members.StaticFieldsAndProperties : members.InstanceFieldsAndProperties;
                if (fieldsAndProperties.TryGetValue(memberName, out var member))
                {
                    isIndexer = member.IsIndexer;
                    return member.Member;
                }
            }
            isIndexer = false;
            return null;
        }

        int FindMethod(Type type, string methodName, bool staticAccess, Expression[] args, out MethodSignature method)
        {
            if (!staticAccess && args.Length == 0 && IsNullableType(type)
                && string.Equals(methodName, nameof(ToString), StringComparison.Ordinal)
                && FindMethod(Nullable.GetUnderlyingType(type), methodName, staticAccess, [], out _) == 1)
            {
                method = new MethodSignature(type.GetMethod(nameof(ToString), Type.EmptyTypes), Type.EmptyTypes, MemberRule.Allowed);
                return 1;
            }

            InvocationMembers members = GetInvocationMembers(type);
            var methods = staticAccess ? members.StaticMethods : members.InstanceMethods;
            if (methods.TryGetValue(methodName, out MethodSignature[] candidates))
                return FindBestMethod(candidates, args, out method);
            method = null;
            return 0;
        }

        int FindIndexer(Type type, Expression[] args, out MethodBase method)
        {
            foreach (Type t in GetDataMembers(type).Hierarchy)
            {
                int count = FindBestMethod(GetDataMembers(t).Indexers, args, out MemberSignature best);
                if (count != 0)
                {
                    method = best?.Method;
                    return count;
                }
            }
            method = null;
            return 0;
        }

        static IEnumerable<Type> SelfAndBaseTypes(Type type)
        {
            if (type.IsInterface)
            {
                List<Type> types = new List<Type>();
                AddInterface(types, type);
                return types;
            }
            return SelfAndBaseClasses(type);
        }

        static IEnumerable<Type> SelfAndBaseClasses(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        static void AddInterface(List<Type> types, Type type)
        {
            if (!types.Contains(type))
            {
                types.Add(type);
                foreach (Type t in type.GetInterfaces())
                    AddInterface(types, t);
            }
        }

        /// <summary>
        /// Selects the best applicable candidate, following the C# "better function member" rules.
        /// When a single best candidate is found, <paramref name="args"/> are promoted to its parameter types.
        /// Operator binding enables <paramref name="preferOrdinaryConversions"/> to first keep candidates needing
        /// the fewest protocol-specific literal re-parses, then compare conversions normally. This preserves
        /// query round trips without type-family tiers; method and constructor binding leave it disabled.
        /// </summary>
        /// <returns>The number of best candidates: 0 for no match, 1 for success and more than 1 if ambiguous.</returns>
        int FindBestMethod<TSignature>(TSignature[] candidates, Expression[] args, out TSignature best, bool preferOrdinaryConversions = false)
            where TSignature : Signature
        {
            // Determine applicability without building any expressions; only the winner is promoted.
            Span<int> applicable = candidates.Length <= 64 ? stackalloc int[candidates.Length] : new int[candidates.Length];
            int applicableCount = 0;
            int minimumRecoveries = int.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (IsApplicable(candidates[i], args))
                {
                    if (preferOrdinaryConversions)
                    {
                        int recoveries = 0;
                        for (int j = 0; j < args.Length; j++)
                        {
                            if (args[j] != NullLiteral && !IsOrdinaryOperatorConversion(args[j], candidates[i].ParameterTypes[j]))
                                recoveries++;
                        }
                        if (recoveries > minimumRecoveries)
                            continue;
                        if (recoveries < minimumRecoveries)
                        {
                            applicableCount = 0;
                            minimumRecoveries = recoveries;
                        }
                    }
                    applicable[applicableCount++] = i;
                }
            }

            int count = applicableCount;
            int bestIndex = applicableCount == 1 ? applicable[0] : -1;
            if (applicableCount > 1)
            {
                count = 0;
                for (int i = 0; i < applicableCount; i++)
                {
                    Type[] candidate = candidates[applicable[i]].ParameterTypes;
                    bool isBest = true;
                    for (int j = 0; j < applicableCount && isBest; j++)
                    {
                        if (i != j && !IsBetterThan(args, candidate, candidates[applicable[j]].ParameterTypes))
                            isBest = false;
                    }
                    if (isBest)
                    {
                        count++;
                        bestIndex = applicable[i];
                    }
                }
            }

            if (count != 1)
            {
                best = null;
                return applicableCount;
            }

            best = candidates[bestIndex];
            for (int i = 0; i < args.Length; i++)
                args[i] = PromoteExpression(args[i], best.ParameterTypes[i], false);
            return 1;
        }

        bool IsOrdinaryOperatorConversion(Expression expression, Type targetType)
        {
            if (IsCompatibleWith(expression.Type, targetType))
                return true;

            // C# allows non-negative int constants to convert to uint or ulong, and non-negative
            // long constants to ulong, even though the corresponding type conversions are not implicit.
            // Treat these as ordinary conversions so uint arithmetic retains its overflow behavior and
            // ulong comparisons remain unambiguous. Other literal re-parses (notably int to char)
            // remain recoveries, so numeric character comparisons still use numeric promotion.
            if (expression is ConstantExpression constant && _literals.ContainsKey(constant))
            {
                Type target = GetNonNullableType(targetType);
                if (constant.Value is int intValue && intValue >= 0)
                    return target == typeof(uint) || target == typeof(ulong);
                if (constant.Value is long longValue && longValue >= 0)
                    return target == typeof(ulong);
            }
            return false;
        }

        bool IsApplicable(Signature candidate, Expression[] args)
        {
            Type[] parameterTypes = candidate.ParameterTypes;
            if (parameterTypes.Length != args.Length)
                return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (!CanPromote(args[i], parameterTypes[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="PromoteExpression"/> would succeed (with exact = <see langword="false"/>),
        /// without creating any expression for the common non-literal case.
        /// </summary>
        bool CanPromote(Expression expr, Type type)
        {
            if (expr.Type == type)
                return true;
            // Literals can be re-parsed as other types (e.g. number to enum or decimal), so reuse the full logic.
            if (expr is ConstantExpression ce && (ce == NullLiteral || _literals.ContainsKey(ce)))
                return PromoteExpression(expr, type, false) != null;
            return IsCompatibleWith(expr.Type, type);
        }

        Expression PromoteExpression(Expression expr, Type type, bool exact)
        {
            if (expr.Type == type)
                return expr;
            if (expr is ConstantExpression)
            {
                ConstantExpression ce = (ConstantExpression)expr;
                if (ce == NullLiteral)
                {
                    if (!type.IsValueType || IsNullableType(type))
                        return Expression.Constant(null, type);
                }
                else
                {
                    if (_literals.TryGetValue(ce, out ReadOnlyMemory<char> text))
                    {
                        Type target = GetNonNullableType(type);
                        object value = null;
                        switch (Type.GetTypeCode(ce.Type))
                        {
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                                if (target.IsEnum)
                                {
                                    // promoting from a number to an enum
                                    value = Enum.ToObject(target, ce.Value);
                                }
                                else if (target == typeof(char))
                                {
                                    // promote from a number to a char
                                    try
                                    {
                                        value = Convert.ToChar(ce.Value, CultureInfo.InvariantCulture);
                                    }
                                    catch (OverflowException)
                                    {
                                        // The value was to large to convert to char/byte so fail
                                        return null;
                                    }
                                }
                                else
                                {
                                    value = ParseNumber(text.Span, target);
                                }
                                break;
                            case TypeCode.Double:
                                // Wanted decimal but it got parsed as double, needs to reparse to avoid loosing precision
                                if (target == typeof(decimal))
                                    value = ParseNumber(text.Span, target);
                                break;
                            case TypeCode.String:
                                // We parsed as text but wanted it as something else, probaly an enum
                                value = ParseEnum(text.Span, target);
                                break;
                        }
                        if (value != null)
                            return Expression.Constant(value, type);
                    }
                }
            }
            if (IsCompatibleWith(expr.Type, type))
            {
                if (type.IsValueType || exact)
                    return Expression.Convert(expr, type);
                return expr;
            }
            return null;
        }

#if NETFRAMEWORK
        static object ParseNumber(ReadOnlySpan<char> span, Type type)
        {
            string text = span.ToString();
#else
        static object ParseNumber(ReadOnlySpan<char> text, Type type)
        {
#endif
            switch (Type.GetTypeCode(GetNonNullableType(type)))
            {
                case TypeCode.SByte:
                    sbyte sb;
                    if (sbyte.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out sb))
                        return sb;
                    break;
                case TypeCode.Byte:
                    byte b;
                    if (byte.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out b))
                        return b;
                    break;
                case TypeCode.Int16:
                    short s;
                    if (short.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out s))
                        return s;
                    break;
                case TypeCode.UInt16:
                    ushort us;
                    if (ushort.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out us))
                        return us;
                    break;
                case TypeCode.Int32:
                    int i;
                    if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out i))
                        return i;
                    break;
                case TypeCode.UInt32:
                    uint ui;
                    if (uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ui))
                        return ui;
                    break;
                case TypeCode.Int64:
                    long l;
                    if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out l))
                        return l;
                    break;
                case TypeCode.UInt64:
                    ulong ul;
                    if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ul))
                        return ul;
                    break;
                case TypeCode.Single:
                    float f;
                    if (float.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out f))
                        return f;
                    break;
                case TypeCode.Double:
                    double d;
                    if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out d))
                        return d;
                    break;
                case TypeCode.Decimal:
                    decimal e;
                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out e))
                        return e;
                    break;
            }
            return null;
        }

        static object ParseEnum(ReadOnlySpan<char> name, Type type)
        {
            if (type.IsEnum)
            {
#if NET
                if (Enum.TryParse(type, name, ignoreCase: true, out var result))
                    return result;
#else
                MemberInfo[] memberInfos = type.FindMembers(MemberTypes.Field,
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static,
                    Type.FilterNameIgnoreCase, name.ToString());
                if (memberInfos.Length != 0)
                    return ((FieldInfo)memberInfos[0]).GetValue(null);
#endif
            }
            return null;
        }

        static bool IsCompatibleWith(Type source, Type target)
        {
            if (source == target)
                return true;
            if (!target.IsValueType)
                return target.IsAssignableFrom(source);
            Type st = GetNonNullableType(source);
            Type tt = GetNonNullableType(target);
            if (st != source && tt == target)
                return false;
            TypeCode sc = st.IsEnum ? TypeCode.Object : Type.GetTypeCode(st);
            TypeCode tc = tt.IsEnum ? TypeCode.Object : Type.GetTypeCode(tt);
            switch (sc)
            {
                case TypeCode.SByte:
                    switch (tc)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Byte:
                    switch (tc)
                    {
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int16:
                    switch (tc)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    switch (tc)
                    {
                        case TypeCode.Char when sc == TypeCode.Char:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int32:
                    switch (tc)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt32:
                    switch (tc)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int64:
                    switch (tc)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt64:
                    switch (tc)
                    {
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Single:
                    switch (tc)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                    }
                    break;
                default:
                    if (st == tt)
                        return true;
                    break;
            }
            return false;
        }

        static bool IsBetterThan(Expression[] args, Type[] parameters1, Type[] parameters2)
        {
            bool better = false;
            for (int i = 0; i < args.Length; i++)
            {
                int c = CompareConversions(args[i].Type,
                    parameters1[i],
                    parameters2[i]);
                if (c < 0)
                    return false;
                if (c > 0)
                    better = true;
            }
            return better;
        }

        // Return 1 if s -> t1 is a better conversion than s -> t2
        // Return -1 if s -> t2 is a better conversion than s -> t1
        // Return 0 if neither conversion is better
        static int CompareConversions(Type s, Type t1, Type t2)
        {
            if (t1 == t2)
                return 0;
            if (s == t1)
                return 1;
            if (s == t2)
                return -1;
            bool t1t2 = IsCompatibleWith(t1, t2);
            bool t2t1 = IsCompatibleWith(t2, t1);
            if (t1t2 && !t2t1)
                return 1;
            if (t2t1 && !t1t2)
                return -1;
            if (IsSignedIntegralType(t1) && IsUnsignedIntegralType(t2))
                return 1;
            if (IsSignedIntegralType(t2) && IsUnsignedIntegralType(t1))
                return -1;
            return 0;
        }

        /// <summary>
        /// Creates a relational comparison. Strings are compared using <see cref="string.Compare(string, string)"/>
        /// since they do not define relational operators.
        /// </summary>
        static Expression GenerateComparison(ExpressionType comparison, Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                left = Expression.Call(s_stringCompare, left, right);
                right = Expression.Constant(0);
            }
            return Expression.MakeBinary(comparison, left, right);
        }

        static Expression GenerateStringConcat(Expression left, Expression right)
        {
            if (left.Type.IsValueType)
                left = Expression.Convert(left, typeof(object));
            if (right.Type.IsValueType)
                right = Expression.Convert(right, typeof(object));
            return Expression.Call(s_concatObjects, left, right);
        }

        void SetTextPos(int pos)
        {
            _textPos = pos;
            _ch = _textPos < _text.Length ? _text[_textPos] : '\0';
        }

        void NextChar()
        {
            if (_textPos < _text.Length)
                _textPos++;
            _ch = _textPos < _text.Length ? _text[_textPos] : '\0';
        }

        void NextToken()
        {
            while (char.IsWhiteSpace(_ch))
                NextChar();
            TokenId t;
            _tokenStart = _textPos;
            switch (_ch)
            {
                case '!':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.ExclamationEqual;
                    }
                    else
                    {
                        t = TokenId.Exclamation;
                    }
                    break;
                case '%':
                    NextChar();
                    t = TokenId.Percent;
                    break;
                case '&':
                    NextChar();
                    if (_ch == '&')
                    {
                        NextChar();
                        t = TokenId.DoubleAmphersand;
                    }
                    else
                    {
                        t = TokenId.Amphersand;
                    }
                    break;
                case '(':
                    NextChar();
                    t = TokenId.OpenParen;
                    break;
                case ')':
                    NextChar();
                    t = TokenId.CloseParen;
                    break;
                case '*':
                    NextChar();
                    t = TokenId.Asterisk;
                    break;
                case '+':
                    NextChar();
                    t = TokenId.Plus;
                    break;
                case ',':
                    NextChar();
                    t = TokenId.Comma;
                    break;
                case '-':
                    NextChar();
                    t = TokenId.Minus;
                    break;
                case '.':
                    NextChar();
                    t = TokenId.Dot;
                    break;
                case '/':
                    NextChar();
                    t = TokenId.Slash;
                    break;
                case ':':
                    NextChar();
                    t = TokenId.Colon;
                    break;
                case '<':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.LessThanEqual;
                    }
                    else if (_ch == '>')
                    {
                        NextChar();
                        t = TokenId.LessGreater;
                    }
                    else
                    {
                        t = TokenId.LessThan;
                    }
                    break;
                case '=':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.DoubleEqual;
                    }
                    else
                    {
                        t = TokenId.Equal;
                    }
                    break;
                case '>':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.GreaterThanEqual;
                    }
                    else
                    {
                        t = TokenId.GreaterThan;
                    }
                    break;
                case '?':
                    NextChar();
                    t = TokenId.Question;
                    break;
                case '[':
                    NextChar();
                    t = TokenId.OpenBracket;
                    break;
                case ']':
                    NextChar();
                    t = TokenId.CloseBracket;
                    break;
                case '|':
                    NextChar();
                    if (_ch == '|')
                    {
                        NextChar();
                        t = TokenId.DoubleBar;
                    }
                    else
                    {
                        t = TokenId.Bar;
                    }
                    break;
                case '"':
                case '\'':
                    char quote = _ch;
                    do
                    {
                        NextChar();
                        while (_textPos < _text.Length && _ch != quote)
                        {
                            if (_ch == '\\')
                            {
                                NextChar();
                            }

                            NextChar();
                        }

                        if (_textPos == _text.Length)
                            throw ParseError(_textPos, Resource.UnterminatedStringLiteral);
                        NextChar();
                    } while (_ch == quote);
                    t = TokenId.StringLiteral;
                    break;
                default:
                    if (IsIdentifierStart(_ch) || _ch == '@' || _ch == '_')
                    {
                        do
                        {
                            NextChar();
                        } while (IsIdentifierPart(_ch) || _ch == '_');
                        t = TokenId.Identifier;
                        break;
                    }
                    if (char.IsDigit(_ch))
                    {
                        t = TokenId.IntegerLiteral;
                        do
                        {
                            NextChar();
                        } while (char.IsDigit(_ch));
                        if (_ch == '.')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (char.IsDigit(_ch));
                        }
                        if (_ch is 'E' or 'e')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            if (_ch is '+' or '-')
                                NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (char.IsDigit(_ch));
                        }
                        if (_ch is 'F' or 'f' or 'M' or 'm' or 'D' or 'd')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                        }
                        break;
                    }
                    if (_textPos == _text.Length)
                    {
                        t = TokenId.End;
                        break;
                    }
                    throw ParseError(_textPos, Resource.InvalidCharacter, _ch);
            }

            _tokenId = t;
        }

        static bool IsIdentifierStart(char ch)
        {
            const int mask =
                1 << (int)UnicodeCategory.UppercaseLetter |
                1 << (int)UnicodeCategory.LowercaseLetter |
                1 << (int)UnicodeCategory.TitlecaseLetter |
                1 << (int)UnicodeCategory.ModifierLetter |
                1 << (int)UnicodeCategory.OtherLetter |
                1 << (int)UnicodeCategory.LetterNumber;
            return (1 << (int)char.GetUnicodeCategory(ch) & mask) != 0;
        }

        static bool IsIdentifierPart(char ch)
        {
            const int mask =
                1 << (int)UnicodeCategory.UppercaseLetter |
                1 << (int)UnicodeCategory.LowercaseLetter |
                1 << (int)UnicodeCategory.TitlecaseLetter |
                1 << (int)UnicodeCategory.ModifierLetter |
                1 << (int)UnicodeCategory.OtherLetter |
                1 << (int)UnicodeCategory.LetterNumber |
                1 << (int)UnicodeCategory.DecimalDigitNumber |
                1 << (int)UnicodeCategory.ConnectorPunctuation |
                1 << (int)UnicodeCategory.NonSpacingMark |
                1 << (int)UnicodeCategory.SpacingCombiningMark |
                1 << (int)UnicodeCategory.Format;
            return (1 << (int)char.GetUnicodeCategory(ch) & mask) != 0;
        }

        bool TokenIdentifierIs(string id)
        {
            Token token = this.CurrentToken;
            return _tokenId == TokenId.Identifier
                && token.length == id.Length
                && GetText(token).Equals(id.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        string GetIdentifier()
        {
            ValidateToken(TokenId.Identifier, Resource.IdentifierExpected);
            var id = GetText(CurrentToken);
            if (id.Length > 1 && id[0] == '@')
                id = id.Slice(1);
            return id.ToString();
        }

        void ValidateDigit()
        {
            if (!char.IsDigit(_ch))
                throw ParseError(_textPos, Resource.DigitExpected);
        }

        void ValidateToken(TokenId t, string errorMessage)
        {
            if (_tokenId != t)
                throw ParseError(errorMessage);
        }

        void ValidateToken(TokenId t)
        {
            if (_tokenId != t)
                throw ParseError(Resource.SyntaxError);
        }

        Exception ParseError(string format, params object[] args)
        {
            return ParseError(CurrentToken.pos, format, args);
        }

        static Exception ParseError(int pos, string format, params object[] args)
        {
            return new ParseException(string.Format(CultureInfo.CurrentCulture, format, args), pos);
        }

        static FrozenDictionary<string, object> CreateKeywords()
        {
            Dictionary<string, object> d = new(capacity: 5 + s_predefinedTypeSet.Count, StringComparer.OrdinalIgnoreCase)
            {
                { "true", TrueLiteral },
                { "false", FalseLiteral },
                { "null", NullLiteral },
                { KeywordIt, KeywordIt },
                { KeywordIif, KeywordIif }
            };
            foreach (Type type in s_predefinedTypeSet)
                d.Add(type.Name, type);

            return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        }
    }
}
