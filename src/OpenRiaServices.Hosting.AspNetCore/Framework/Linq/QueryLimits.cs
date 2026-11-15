using System;

namespace OpenRiaServices.Hosting.AspNetCore;

/// <summary>
/// Limits resource consumption when parsing client query expressions.
/// </summary>
#if ASPNET_CORE
public
#else
internal
#endif
sealed class QueryLimits
{
    /// <summary>
    /// Gets the default limits for client query expressions.
    /// </summary>
    internal static QueryLimits Defaults { get; } = new();

    private int _maxExpressionDepth = System.Linq.Dynamic.ExpressionParser.MaxExpressionDepth;
    private int _maxNumericFormatPrecision = 100;
    private int _maxNumericFormatLength = 64;

    /// <summary>
    /// Gets or sets the maximum parser recursion and expression-tree depth. The default is 200.
    /// </summary>
    /// <remarks>Stack safety checks can reject expressions below this limit. Increasing it can compromise stack safety.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public int MaxExpressionDepth
    {
        get => _maxExpressionDepth;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _maxExpressionDepth = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum precision of a standard numeric format in a client query. The default is 100.
    /// </summary>
    /// <remarks>Increasing this limit allows larger string allocations during query execution.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxNumericFormatPrecision
    {
        get => _maxNumericFormatPrecision;
        set
        {
#if NETFRAMEWORK
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
#else
            ArgumentOutOfRangeException.ThrowIfNegative(value);
#endif
            _maxNumericFormatPrecision = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of characters in a numeric format string in a client query. The default is 64.
    /// </summary>
    /// <remarks>Numeric formats must be constants. This limit also bounds custom numeric formats.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public int MaxNumericFormatLength
    {
        get => _maxNumericFormatLength;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _maxNumericFormatLength = value;
        }
    }
}
