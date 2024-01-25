using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System;

namespace System.Linq.Dynamic
{
#if NETFRAMEWORK
    static class StringExtensions
    {
        static public string Slice(this string str, int startIndex, int length)
            => str.Substring(startIndex, length);
    }
#endif

    internal static class DynamicQueryable
    {
        public static IQueryable Where(this IQueryable source, string predicate, QueryResolver queryResolver)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            LambdaExpression lambda = DynamicExpression.ParseLambda(source.ElementType, typeof(bool), predicate, queryResolver);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Where",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Quote(lambda)));
        }

        public static IQueryable OrderBy(this IQueryable source, string ordering, QueryResolver queryResolver)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (ordering == null)
                throw new ArgumentNullException(nameof(ordering));
            ParameterExpression[] parameters = new ParameterExpression[] {
                Expression.Parameter(source.ElementType, "") };
            ExpressionParser parser = new ExpressionParser(parameters, ordering, queryResolver);
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
            return source.Provider.CreateQuery(queryExpr);
        }

        public static IQueryable Take(this IQueryable source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Take",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Skip",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }
    }

    internal static class DynamicExpression
    {
        static readonly Type[] funcTypes = new Type[] {
            typeof(Func<>),
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>)
        };

        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression, QueryResolver queryResolver)
        {
            return ParseLambda(new ParameterExpression[] { Expression.Parameter(itType, "") }, resultType, expression, queryResolver);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type resultType, string expression, QueryResolver queryResolver)
        {
            ExpressionParser parser = new ExpressionParser(parameters, expression, queryResolver);
            return Lambda(parser.Parse(resultType), parameters);
        }

        public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters)
        {
            int paramCount = parameters == null ? 0 : parameters.Length;
            Type[] typeArgs = new Type[paramCount + 1];
            for (int i = 0; i < paramCount; i++)
                typeArgs[i] = parameters[i].Type;
            typeArgs[paramCount] = body.Type;
            return Expression.Lambda(GetFuncType(typeArgs), body, parameters);
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "Arguments are provided internally by the parser's ParserLambda methods.")]
        public static Type GetFuncType(params Type[] typeArgs)
        {
            if (typeArgs == null || typeArgs.Length < 1 || typeArgs.Length > 5)
                throw new ArgumentException(nameof(typeArgs));
            return funcTypes[typeArgs.Length - 1].MakeGenericType(typeArgs);
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

    internal partial class ExpressionParser
    {
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

        interface ILogicalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        interface IArithmeticSignatures
        {
            void F(int x, int y);
            void F(uint x, uint y);
            void F(long x, long y);
            void F(ulong x, ulong y);
            void F(float x, float y);
            void F(double x, double y);
            void F(decimal x, decimal y);
            void F(int? x, int? y);
            void F(uint? x, uint? y);
            void F(long? x, long? y);
            void F(ulong? x, ulong? y);
            void F(float? x, float? y);
            void F(double? x, double? y);
            void F(decimal? x, decimal? y);
        }

        interface IRelationalSignatures : IArithmeticSignatures
        {
            void F(string x, string y);
            void F(char x, char y);
            void F(DateTime x, DateTime y);
            void F(TimeSpan x, TimeSpan y);
            void F(char? x, char? y);
            void F(DateTime? x, DateTime? y);
            void F(TimeSpan? x, TimeSpan? y);
            void F(DateTimeOffset x, DateTimeOffset y);
            void F(DateTimeOffset? x, DateTimeOffset? y);
        }

        interface IEqualitySignatures : IRelationalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
            void F(Guid x, Guid y);
            void F(Guid? x, Guid? y);
        }

        interface IAddSignatures : IArithmeticSignatures
        {
            void F(DateTime x, TimeSpan y);
            void F(TimeSpan x, TimeSpan y);
            void F(DateTime? x, TimeSpan? y);
            void F(TimeSpan? x, TimeSpan? y);
            void F(DateTimeOffset x, TimeSpan y);
            void F(DateTimeOffset? x, TimeSpan? y);
        }

        interface ISubtractSignatures : IAddSignatures
        {
            void F(DateTime x, DateTime y);
            void F(DateTime? x, DateTime? y);
            void F(DateTimeOffset x, DateTimeOffset y);
            void F(DateTimeOffset? x, DateTimeOffset? y);
        }

        interface INegationSignatures
        {
            void F(int x);
            void F(long x);
            void F(float x);
            void F(double x);
            void F(decimal x);
            void F(int? x);
            void F(long? x);
            void F(float? x);
            void F(double? x);
            void F(decimal? x);
        }

        interface INotSignatures
        {
            void F(bool x);
            void F(bool? x);
        }

        interface IEnumerableSignatures
        {
            void Where(bool predicate);
            void Any();
            void Any(bool predicate);
            void All(bool predicate);
            void Count();
            void Count(bool predicate);
            void Min(object selector);
            void Max(object selector);
            void Sum(int selector);
            void Sum(int? selector);
            void Sum(long selector);
            void Sum(long? selector);
            void Sum(float selector);
            void Sum(float? selector);
            void Sum(double selector);
            void Sum(double? selector);
            void Sum(decimal selector);
            void Sum(decimal? selector);
            void Average(int selector);
            void Average(int? selector);
            void Average(long selector);
            void Average(long? selector);
            void Average(float selector);
            void Average(float? selector);
            void Average(double selector);
            void Average(double? selector);
            void Average(decimal selector);
            void Average(decimal? selector);
        }

        static readonly Type[] s_predefinedTypes = {
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
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Math),
            typeof(Convert),
            typeof(StringComparison),
            typeof(Uri)
        };

        static readonly ConstantExpression TrueLiteral = Expression.Constant(true);
        static readonly ConstantExpression FalseLiteral = Expression.Constant(false);
        static readonly ConstantExpression NullLiteral = Expression.Constant(null);

        const string KeywordIt = "it";
        const string KeywordIif = "iif";

        static readonly Dictionary<string, object> s_keywords = CreateKeywords();

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


        public ExpressionParser(ParameterExpression[] parameters, string expression, QueryResolver queryResolver)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

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
            if (_symbols.ContainsKey(name))
                throw ParseError(Resource.DuplicateIdentifier, name);
            _symbols.Add(name, value);
        }

        public Expression Parse(Type resultType)
        {
            int exprPos = CurrentToken.pos;
            Expression expr = ParseExpression();
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

        // ||, or operator
        Expression ParseLogicalOr()
        {
            Expression left = ParseLogicalAnd();
            while (CurrentToken.id == TokenId.DoubleBar || TokenIdentifierIs("or"))
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseLogicalAnd();
                CheckAndPromoteOperands(typeof(ILogicalSignatures), op, ref left, ref right);
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
                CheckAndPromoteOperands(typeof(ILogicalSignatures), op, ref left, ref right);
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

                CheckAndPromoteOperands(typeof(IArithmeticSignatures), op, ref left, ref right);

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
            while (CurrentToken.id == TokenId.Equal || CurrentToken.id == TokenId.DoubleEqual ||
                CurrentToken.id == TokenId.ExclamationEqual || CurrentToken.id == TokenId.LessGreater ||
                CurrentToken.id == TokenId.GreaterThan || CurrentToken.id == TokenId.GreaterThanEqual ||
                CurrentToken.id == TokenId.LessThan || CurrentToken.id == TokenId.LessThanEqual)
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseAdditive();
                bool isEquality = op.id == TokenId.Equal || op.id == TokenId.DoubleEqual ||
                    op.id == TokenId.ExclamationEqual || op.id == TokenId.LessGreater;
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
                else if (IsEnumType(left.Type) || IsEnumType(right.Type))
                {
                    // convert enum expressions to their underlying values for comparison
                    left = ConvertEnumExpression(left, right);
                    right = ConvertEnumExpression(right, left);

                    CheckAndPromoteOperands(isEquality ? typeof(IEqualitySignatures) : typeof(IRelationalSignatures),
                        op, ref left, ref right);
                }
                else
                {
                    CheckAndPromoteOperands(isEquality ? typeof(IEqualitySignatures) : typeof(IRelationalSignatures),
                        op, ref left, ref right);
                }
                switch (op.id)
                {
                    case TokenId.Equal:
                    case TokenId.DoubleEqual:
                        left = GenerateEqual(left, right);
                        break;
                    case TokenId.ExclamationEqual:
                    case TokenId.LessGreater:
                        left = GenerateNotEqual(left, right);
                        break;
                    case TokenId.GreaterThan:
                        left = GenerateGreaterThan(left, right);
                        break;
                    case TokenId.GreaterThanEqual:
                        left = GenerateGreaterThanEqual(left, right);
                        break;
                    case TokenId.LessThan:
                        left = GenerateLessThan(left, right);
                        break;
                    case TokenId.LessThanEqual:
                        left = GenerateLessThanEqual(left, right);
                        break;
                }
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
            while (CurrentToken.id == TokenId.Plus || CurrentToken.id == TokenId.Minus ||
                CurrentToken.id == TokenId.Amphersand)
            {
                Token op = CurrentToken;
                NextToken();
                Expression right = ParseMultiplicative();
                switch (op.id)
                {
                    case TokenId.Plus:
                        if (left.Type == typeof(string) || right.Type == typeof(string))
                            goto case TokenId.Amphersand;
                        CheckAndPromoteOperands(typeof(IAddSignatures), op, ref left, ref right);
                        left = GenerateAdd(left, right);
                        break;
                    case TokenId.Minus:
                        CheckAndPromoteOperands(typeof(ISubtractSignatures), op, ref left, ref right);
                        left = GenerateSubtract(left, right);
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
                CheckAndPromoteOperands(typeof(IArithmeticSignatures), op, ref left, ref right);
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
                Expression expr = ParseUnary();
                if (op.id == TokenId.Minus)
                {
                    CheckAndPromoteOperand(typeof(INegationSignatures), op, ref expr);
                    expr = Expression.Negate(expr);
                }
                else
                {
                    CheckAndPromoteOperand(typeof(INotSignatures), op, ref expr);
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
            Token integerToken = CurrentToken;
            ValidateToken(TokenId.IntegerLiteral);
#if NET
            ReadOnlySpan<char> text = GetText(integerToken);
#else
            string text = GetString(CurrentToken);
#endif
            if (text[0] != '-')
            {
                ulong value;
                if (!ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
                    throw ParseError(Resource.InvalidIntegerLiteral, text.ToString());
                NextToken();
                if (value <= (ulong)int.MaxValue)
                    return CreateLiteral((int)value, integerToken);
                if (value <= (ulong)uint.MaxValue)
                    return CreateLiteral((uint)value, integerToken);
                if (value <= (ulong)long.MaxValue)
                    return CreateLiteral((long)value, integerToken);
                return CreateLiteral(value, integerToken);
            }
            else
            {
                long value;
                if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                    throw ParseError(Resource.InvalidIntegerLiteral, text.ToString());
                NextToken();
                if (value >= int.MinValue && value <= int.MaxValue)
                    return CreateLiteral((int)value, integerToken);
                return CreateLiteral(value, integerToken);
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
            if (last == 'F' || last == 'f')
            {
                float f;
                if (float.TryParse(text.Slice(0, text.Length - 1), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out f))
                    value = f;
            }
            else if (last == 'M' || last == 'm')
            {
                decimal m;
                if (decimal.TryParse(text.Slice(0, text.Length - 1), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out m))
                    value = m;
            }
            else if (last == 'D' || last == 'd')
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
                if (value is Type)
                    return ParseTypeAccess((Type)value);
                if (value == (object)KeywordIt)
                    return ParseIt();
                if (value == (object)KeywordIif)
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
                MethodBase method;
                switch (FindBestMethod(type.GetConstructors(), args, out method))
                {
                    case 0:
                        if (args.Length == 1)
                            return GenerateConversion(args[0], type, errorPos);
                        throw ParseError(errorPos, Resource.NoMatchingConstructor, GetTypeName(type));
                    case 1:
                        if (!IsMemberAccessAllowed(method, args))
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
                    Type enumerableType = FindGenericType(typeof(IEnumerable<>), type);
                    if (enumerableType != null)
                    {
                        Type elementType = enumerableType.GetGenericArguments()[0];
                        return ParseAggregate(instance, elementType, id, errorPos);
                    }
                }
                Expression[] args = ParseArgumentList();
                MethodBase mb;
                switch (FindMethod(type, id, instance == null, args, out mb))
                {
                    case 0:
                        throw ParseError(errorPos, Resource.NoApplicableMethod,
                            id, GetTypeName(type));
                    case 1:
                        MethodInfo method = (MethodInfo)mb;
                        if (!IsPredefinedType(method.DeclaringType))
                            throw ParseError(errorPos, Resource.MethodsAreInaccessible, GetTypeName(method.DeclaringType));
                        if (method.ReturnType == typeof(void))
                            throw ParseError(errorPos, Resource.MethodIsVoid,
                                id, GetTypeName(method.DeclaringType));
                        if (!IsMemberAccessAllowed(method, args))
                            throw ParseError(errorPos, Resource.MethodsAreInaccessible, GetTypeName(method.DeclaringType));
                        return Expression.Call(instance, (MethodInfo)method, args);
                    default:
                        throw ParseError(errorPos, Resource.AmbiguousMethodInvocation,
                            id, GetTypeName(type));
                }
            }
            else
            {
                MemberInfo member = FindPropertyOrField(type, id, instance == null);
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

        static Type FindGenericType(Type generic, Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == generic)
                    return type;
                if (generic.IsInterface)
                {
                    foreach (Type intfType in type.GetInterfaces())
                    {
                        Type found = FindGenericType(generic, intfType);
                        if (found != null)
                            return found;
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        Expression ParseAggregate(Expression instance, Type elementType, string methodName, int errorPos)
        {
            ParameterExpression outerIt = _it;
            ParameterExpression innerIt = Expression.Parameter(elementType, "");
            _it = innerIt;
            Expression[] args = ParseArgumentList();
            _it = outerIt;
            MethodBase signature;
            if (FindMethod(typeof(IEnumerableSignatures), methodName, false, args, out signature) != 1)
                throw ParseError(errorPos, Resource.NoApplicableAggregate, methodName);
            Type[] typeArgs;
            if (signature.Name == "Min" || signature.Name == "Max")
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
            return Expression.Call(typeof(Enumerable), signature.Name, typeArgs, args);
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

        static bool IsPredefinedType(Type type)
        {
            type = GetNonNullableType(type);
            foreach (Type t in s_predefinedTypes)
                if (t == type)
                    return true;
            return false;
        }

        static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
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

        void CheckAndPromoteOperand(Type signatures, Token op, ref Expression expr)
        {
            Expression[] args = new Expression[] { expr };
            MethodBase method;
            if (FindMethod(signatures, "F", false, args, out method) != 1)
                throw ParseError(op.pos, Resource.IncompatibleOperand,
                    GetString(op), GetTypeName(args[0].Type));
            expr = args[0];
        }

        void CheckAndPromoteOperands(Type signatures, Token op, ref Expression left, ref Expression right)
        {
            Expression[] args = new Expression[] { left, right };
            MethodBase method;
            if (FindMethod(signatures, "F", false, args, out method) != 1)
                throw IncompatibleOperandsError(GetString(op), left, right, op.pos);
            left = args[0];
            right = args[1];
        }

        static Exception IncompatibleOperandsError(string opName, Expression left, Expression right, int pos)
        {
            return ParseError(pos, Resource.IncompatibleOperands,
                opName, GetTypeName(left.Type), GetTypeName(right.Type));
        }

        static MemberInfo FindPropertyOrField(Type type, string memberName, bool staticAccess)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly |
                (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.FindMembers(MemberTypes.Property | MemberTypes.Field,
                    flags, Type.FilterNameIgnoreCase, memberName);
                if (members.Length != 0)
                    return members[0];
            }
            return null;
        }

        int FindMethod(Type type, string methodName, bool staticAccess, Expression[] args, out MethodBase method)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly |
                (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.FindMembers(MemberTypes.Method,
                    flags, Type.FilterNameIgnoreCase, methodName);
                int count = FindBestMethod(members.Cast<MethodBase>(), args, out method);
                if (count != 0)
                    return count;
            }
            method = null;
            return 0;
        }

        int FindIndexer(Type type, Expression[] args, out MethodBase method)
        {
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.GetDefaultMembers();
                if (members.Length != 0)
                {
                    IEnumerable<MethodBase> methods = members.
                        OfType<PropertyInfo>().
                        Select(p => (MethodBase)p.GetGetMethod()).
                        Where(m => m != null);
                    int count = FindBestMethod(methods, args, out method);
                    if (count != 0)
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

        class MethodData
        {
            public MethodBase MethodBase;
            public ParameterInfo[] Parameters;
            public Expression[] Args;
        }

        int FindBestMethod(IEnumerable<MethodBase> methods, Expression[] args, out MethodBase method)
        {
            MethodData[] applicable = methods.
                Select(m => new MethodData
                {
                    MethodBase = m,
                    Parameters = m.GetParameters()
                }).
                Where(m => IsApplicable(m, args)).
                ToArray();
            if (applicable.Length > 1)
            {
                applicable = applicable.
                    Where(m => applicable.All(n => m == n || IsBetterThan(args, m, n))).
                    ToArray();
            }
            if (applicable.Length == 1)
            {
                MethodData md = applicable[0];
                for (int i = 0; i < args.Length; i++)
                    args[i] = md.Args[i];
                method = md.MethodBase;
            }
            else
            {
                method = null;
            }
            return applicable.Length;
        }

        bool IsApplicable(MethodData method, Expression[] args)
        {
            if (method.Parameters.Length != args.Length)
                return false;
            Expression[] promotedArgs = new Expression[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                ParameterInfo pi = method.Parameters[i];
                if (pi.IsOut)
                    return false;
                Expression promoted = PromoteExpression(args[i], pi.ParameterType, false);
                if (promoted == null)
                    return false;
                promotedArgs[i] = promoted;
            }
            method.Args = promotedArgs;
            return true;
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
#if !NETFRAMEWORK
                                if (ce.Value is string str)
                                {
                                    // We parsed as text but wanted it as something else, probaly an enum
                                    value = ParseEnum(str, target);
                                }
                                else
#endif
                                {
                                    // We parsed as text but wanted it as something else, probaly an enum
                                    value = ParseEnum(text.Span, target);
                                }
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
                Enum.TryParse(type, name, ignoreCase: true, out var result);
#else
                object result;
                try
                {
                    result = Enum.Parse(type, name.ToString(), ignoreCase: true);

                }
                catch (Exception)
                {
                    result = null;
                }
#endif
                return result;
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
                case TypeCode.UInt16:
                    switch (tc)
                    {
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

        static bool IsBetterThan(Expression[] args, MethodData m1, MethodData m2)
        {
            bool better = false;
            for (int i = 0; i < args.Length; i++)
            {
                int c = CompareConversions(args[i].Type,
                    m1.Parameters[i].ParameterType,
                    m2.Parameters[i].ParameterType);
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

        static Expression GenerateEqual(Expression left, Expression right)
        {
            return Expression.Equal(left, right);
        }

        static Expression GenerateNotEqual(Expression left, Expression right)
        {
            return Expression.NotEqual(left, right);
        }

        static Expression GenerateGreaterThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThan(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.GreaterThan(left, right);
        }

        static Expression GenerateGreaterThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThanOrEqual(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.GreaterThanOrEqual(left, right);
        }

        static Expression GenerateLessThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThan(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.LessThan(left, right);
        }

        static Expression GenerateLessThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThanOrEqual(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                );
            }
            return Expression.LessThanOrEqual(left, right);
        }

        static Expression GenerateAdd(Expression left, Expression right)
        {
            if (left.Type == typeof(string) && right.Type == typeof(string))
            {
                return GenerateStaticMethodCall("Concat", left, right);
            }
            return Expression.Add(left, right);
        }

        static Expression GenerateSubtract(Expression left, Expression right)
        {
            return Expression.Subtract(left, right);
        }

        static Expression GenerateStringConcat(Expression left, Expression right)
        {
            if (left.Type.IsValueType)
                left = Expression.Convert(left, typeof(object));
            if (right.Type.IsValueType)
                right = Expression.Convert(right, typeof(object));
            return Expression.Call(
                null,
                typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) }),
                new[] { left, right });
        }

        static MethodInfo GetStaticMethod(string methodName, Expression left, Expression right)
        {
            return left.Type.GetMethod(methodName, new[] { left.Type, right.Type });
        }

        static Expression GenerateStaticMethodCall(string methodName, Expression left, Expression right)
        {
            return Expression.Call(null, GetStaticMethod(methodName, left, right), new[] { left, right });
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
                        if (_ch == 'E' || _ch == 'e')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            if (_ch == '+' || _ch == '-')
                                NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (char.IsDigit(_ch));
                        }
                        if (_ch == 'F' || _ch == 'f' || _ch == 'M' || _ch == 'm' || _ch == 'D' || _ch == 'd')
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

        static Dictionary<string, object> CreateKeywords()
        {
            Dictionary<string, object> d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            d.Add("true", TrueLiteral);
            d.Add("false", FalseLiteral);
            d.Add("null", NullLiteral);
            d.Add(KeywordIt, KeywordIt);
            d.Add(KeywordIif, KeywordIif);
            foreach (Type type in s_predefinedTypes)
                d.Add(type.Name, type);
            return d;
        }
    }
}
