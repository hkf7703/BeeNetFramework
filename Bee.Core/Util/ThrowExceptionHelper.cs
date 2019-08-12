using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Bee.Core;

namespace Bee.Util
{
    /// <summary>
    /// Util to throw exception.
    /// </summary>
    public static class ThrowExceptionUtil
    {
        /// <summary>
        /// Throws <typeparamref name="ArgumentException"/> when the condition is false.
        /// </summary>
        /// <param name="condition">the condition to indicate throw the exception or not.</param>
        /// <param name="parameterName">the parameter name.</param>
        /// <param name="message">the message of the exception.</param>
        public static void ArgumentConditionTrue(bool condition, string parameterName, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throw <typeparamref name="ArgumentException"/> when the parameter is null or empty.
        /// </summary>
        /// <param name="parameterValue">the parameter value.</param>
        /// <param name="parameterName">the parameter name.</param>
        public static void ArgumentNotNullOrEmpty(string parameterValue, string parameterName)
        {
            ArgumentConditionTrue(!string.IsNullOrEmpty(parameterValue), parameterName, "The parameter is not valid");
        }

        /// <summary>
        /// Throw <typeparamref name="ArgumentException"/> when the parameter is null.
        /// </summary>
        /// <param name="parameterValue">the parameter value.</param>
        /// <param name="parameterName">the parameter name.</param>
        public static void ArgumentNotNull(object parameterValue, string parameterName)
        {
            if (parameterValue == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Throw <typeparamref name="ArgumentException"/> or <typeparamref name="ArgumentNullException"/>when the parameter is null or empty.
        /// </summary>
        /// <typeparam name="T">the type.</typeparam>
        /// <param name="collection">the instance of the collection.</param>
        /// <param name="parameterName">the parameter name.</param>
        public static void ArgumentNotNullOrEmpty<T>(ICollection<T> collection, string parameterName)
        {
            ArgumentNotNullOrEmpty<T>(collection, parameterName, 
                "Collection '{0}' cannot be empty.".FormatWith(parameterName));
        }

        /// <summary>
        /// Throw <typeparamref name="ArgumentException"/> or <typeparamref name="ArgumentNullException"/> when the parameter is null or empty.
        /// </summary>
        /// <typeparam name="T">the type.</typeparam>
        /// <param name="collection">the instance of the collection.</param>
        /// <param name="parameterName">the parameter name.</param>
        /// <param name="message">the message of the exception.</param>
        public static void ArgumentNotNullOrEmpty<T>(ICollection<T> collection, string parameterName, string message)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (collection.Count == 0)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throw <typeparamref name="ArgumentException"/> or <typeparamref name="ArgumentNullException"/> when the parameter is null or empty.
        /// </summary>
        /// <param name="collection">the instance of the collection.</param>
        /// <param name="parameterName">the parameter name.</param>
        /// <param name="message">the message of the exception.</param>
        public static void ArgumentNotNullOrEmpty(System.Collections.ICollection collection, string parameterName, string message)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (collection.Count == 0)
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void ThrowMessageException(string message)
        {
            throw new CoreException(message);
        }

    }
}
