using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Util;

namespace Bee.Logging
{
    internal class ConsoleLogImpl : ILogImpl
    {
        #region ILogImpl 成员

        public void ProcessLog(LogLevel level, string name, string message, Exception exception)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = GetColor(level);
            Console.WriteLine(Logger.FormatOut(level, name, message, exception));

            Console.ForegroundColor = color;
        }

        #endregion

        /// <summary>
        /// Get a color for a specific log level
        /// </summary>
        /// <param name="logLevel">Level to get color for</param>
        /// <returns>Level color</returns>
        protected virtual ConsoleColor GetColor(LogLevel logLevel)
        {
            switch (logLevel.Value)
            {
                case 1:
                    return ConsoleColor.DarkGray;
                case 2:
                    return ConsoleColor.Gray;
                case 4:
                    return ConsoleColor.White;
                case 6:
                    return ConsoleColor.Red;
                case 7:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.Blue;
            }
        }
    }
}
