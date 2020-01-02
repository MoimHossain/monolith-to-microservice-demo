using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DevDays2020.Data
{
    public static class SafetyExtensions
    {
        public static void Execute(ILogger logger, Action action)
        {
            Ensure.ArgumentNotNull(logger, nameof(logger));
            Ensure.ArgumentNotNull(action, nameof(action));
            try
            {
                action();
            }
            catch (Exception exception)
            {
                logger.LogError("Exception occured: {0}", exception.Message);
            }
        }

        public static async Task ExecuteAsync(ILogger logger, Func<Task> asyncAction)
        {
            Ensure.ArgumentNotNull(asyncAction, nameof(asyncAction));
            try
            {
                await asyncAction();
            }
            catch (Exception exception)
            {
                if (logger != null) { logger.LogError("Exception occured: {0}", exception.Message); }
            }
        }

        public static async Task ExecuteAsync(
            Func<Task> asyncAction, Func<Exception, Task> asyncErrorAction)
        {
            Ensure.ArgumentNotNull(asyncAction, nameof(asyncAction));
            try
            {
                await asyncAction();
            }
            catch (Exception exception)
            {
                await asyncErrorAction(exception);
            }
        }
    }

    public static class Ensure
    {
        /// <summary>
        /// Ensures that the specified argument is not null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        [DebuggerStepThrough]
        public static void ArgumentNotNull(object argument, string argumentName = "")
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Ensures that the specified argument is not null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        [DebuggerStepThrough]
        public static void ArgumentNotNullOrEmpty(string argument, string argumentName = "")
        {
            if (string.IsNullOrEmpty(argument))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Ensures that the specified argument is not null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        [DebuggerStepThrough]
        public static void ArgumentNotNullOrWhiteSpace(
            string argument, string argumentName = "")
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}

