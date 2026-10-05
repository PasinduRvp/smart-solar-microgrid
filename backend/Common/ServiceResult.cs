/*
 * ---------------------------------------------------------------------------
 * File        : ServiceResult.cs
 * Project     : Smart Solar Microgrid Trading System — SE4040 Assignment 1
 * Author      : NIMSARA R V P P (IT 23215306)
 * Created     : 2026-09-19
 * Description : Outcome of a service operation: either a value, or a failure
 *               with a category and a message safe to show the user.
 *
 * Code smell  : Avoids using exceptions for ordinary control flow. A wrong
 *               password, or a booking inside the 12 hour window, is an
 *               expected outcome rather than an exceptional one. Throwing for
 *               those would be slower, harder to follow, and would hide the
 *               fact that the failure is part of the design. Exceptions stay
 *               reserved for genuine faults, which GlobalExceptionMiddleware
 *               handles.
 * SOLID       : Single Responsibility — carries an outcome and nothing else.
 * Design note : The factory methods live on the NON generic ServiceResult class
 *               rather than on ServiceResult<TValue>. Two reasons: analyser
 *               rule CA1000 advises against static members on generic types,
 *               and more usefully it lets the compiler infer the type argument,
 *               so callers write ServiceResult.Success(dto) instead of
 *               ServiceResult<SomeLongDtoName>.Success(dto).
 * ---------------------------------------------------------------------------
 */

namespace SolarMicrogrid.Api.Common;

/// <summary>
/// Creates <see cref="ServiceResult{TValue}"/> instances.
/// </summary>
public static class ServiceResult
{
    /// <summary>Creates a successful result carrying the given value.</summary>
    /// <typeparam name="TValue">Type of the produced value, normally inferred.</typeparam>
    public static ServiceResult<TValue> Success<TValue>(TValue value) =>
        new(isSuccess: true, value, ServiceErrorType.None, error: null);

    /// <summary>Creates a failed result of the given category.</summary>
    /// <typeparam name="TValue">Type the operation would have produced.</typeparam>
    /// <param name="errorType">Category of failure, which decides the status code.</param>
    /// <param name="error">Message written to be shown to the end user.</param>
    public static ServiceResult<TValue> Failure<TValue>(ServiceErrorType errorType, string error) =>
        new(isSuccess: false, value: default, errorType, error);
}

/// <summary>
/// The result of a service operation that returns a value.
/// </summary>
/// <typeparam name="TValue">Type produced when the operation succeeds.</typeparam>
public sealed class ServiceResult<TValue>
{
    /// <summary>
    /// Internal so results can only be built through the <see cref="ServiceResult"/>
    /// factory methods, which keeps the success and failure shapes consistent.
    /// </summary>
    internal ServiceResult(bool isSuccess, TValue? value, ServiceErrorType errorType, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorType = errorType;
        Error = error;
    }

    /// <summary>True when the operation completed and <see cref="Value"/> is populated.</summary>
    public bool IsSuccess { get; }

    /// <summary>The produced value. Null when the operation failed.</summary>
    public TValue? Value { get; }

    /// <summary>Category of failure, used by the controller to pick a status code.</summary>
    public ServiceErrorType ErrorType { get; }

    /// <summary>
    /// Message describing the failure. Written to be shown directly to the end
    /// user, so it must never contain internal detail such as a stack trace.
    /// </summary>
    public string? Error { get; }
}
