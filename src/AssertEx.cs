using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OwlCore.Storage.CommonTests;

/// <summary>
/// Extended assertion helpers for common test scenarios.
/// </summary>
public static class AssertEx
{
    /// <summary>
    /// Tests whether the code specified by <paramref name="action"/> throws an exception of type <typeparamref name="T"/>
    /// or any type derived from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected exception type, or a base type of the expected exception.</typeparam>
    /// <param name="action">The async action to execute.</param>
    /// <param name="message">The message to include in the exception when the assertion fails.</param>
    /// <returns>The exception that was thrown.</returns>
    /// <exception cref="AssertFailedException">Thrown if no exception or an incompatible exception type is thrown.</exception>
    public static async Task<T> ThrowsExceptionAsync<T>(Func<Task> action, string? message = null) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T ex)
        {
            // Success - the exception is of the expected type or derived from it
            return ex;
        }
        catch (Exception ex)
        {
            var msg = message != null
                ? $"{message} Expected exception type: <{typeof(T).FullName}>. Actual exception type: <{ex.GetType().FullName}>."
                : $"Assert.ThrowsException failed. Expected exception type: <{typeof(T).FullName}>. Actual exception type: <{ex.GetType().FullName}>.";
            throw new AssertFailedException(msg, ex);
        }

        var noExMsg = message != null
            ? $"{message} Expected exception type: <{typeof(T).FullName}> but no exception was thrown."
            : $"Assert.ThrowsException failed. Expected exception type: <{typeof(T).FullName}> but no exception was thrown.";
        throw new AssertFailedException(noExMsg);
    }

    /// <summary>
    /// Tests whether the code specified by <paramref name="action"/> throws an exception of type <typeparamref name="T"/>
    /// or any type derived from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected exception type, or a base type of the expected exception.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="message">The message to include in the exception when the assertion fails.</param>
    /// <returns>The exception that was thrown.</returns>
    /// <exception cref="AssertFailedException">Thrown if no exception or an incompatible exception type is thrown.</exception>
    public static T ThrowsException<T>(Action action, string? message = null) where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            // Success - the exception is of the expected type or derived from it
            return ex;
        }
        catch (Exception ex)
        {
            var msg = message != null
                ? $"{message} Expected exception type: <{typeof(T).FullName}>. Actual exception type: <{ex.GetType().FullName}>."
                : $"Assert.ThrowsException failed. Expected exception type: <{typeof(T).FullName}>. Actual exception type: <{ex.GetType().FullName}>.";
            throw new AssertFailedException(msg, ex);
        }

        var noExMsg = message != null
            ? $"{message} Expected exception type: <{typeof(T).FullName}> but no exception was thrown."
            : $"Assert.ThrowsException failed. Expected exception type: <{typeof(T).FullName}> but no exception was thrown.";
        throw new AssertFailedException(noExMsg);
    }
}
