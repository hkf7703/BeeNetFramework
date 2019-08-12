using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Bee.Caching;

namespace Bee.Util
{
    /// <summary>
    /// The Util to get the content of the resource.
    /// </summary>
    public static class ResourceUtil
    {
        /// <summary>
        /// Gets the stream of the resource in the assembly.
        /// </summary>
        /// <param name="asm">the assembly.</param>
        /// <param name="filePath">the path of the resource.</param>
        /// <param name="addPrefix">the flag to indicate to add the prefix of the assembly name or not.</param>
        /// <returns>the stream of the resource.</returns>
        public static Stream GetStream(Assembly asm, string filePath, bool addPrefix)
        {
            string name = addPrefix ? string.Format("{0}.{1}", asm.GetName().Name, filePath) : filePath;
            return asm.GetManifestResourceStream(name);
        }

        /// <summary>
        /// Gets the content of the resource in the assembly.
        /// </summary>
        /// <param name="asm">the assembly.</param>
        /// <param name="filePath">the path of the resource.</param>
        /// <returns>the content of the resource.</returns>
        public static string ReadToEnd(Assembly asm, string filePath)
        {
            return ReadToEnd(asm, filePath, true);
        }

        /// <summary>
        /// Get the content of the resource in the assembly via type.
        /// </summary>
        /// <param name="t">the type.</param>
        /// <param name="filePath">the </param>
        /// <returns>the content of the resource.</returns>
        public static string ReadToEnd(Type t, string filePath)
        {
            return ReadToEnd(t.Assembly, filePath);
        }

        /// <summary>
        /// Gets the content of the resource in the assembly.
        /// </summary>
        /// <param name="asm">the assembly.</param>
        /// <param name="filePath">the path of the resource.</param>
        /// <param name="addPrefix">the flag to indicate to add the prefix of the assembly name or not.</param>
        /// <returns>the content of the resource.</returns>
        public static string ReadToEnd(Assembly asm, string filePath, bool addPrefix)
        {
            using (StreamReader reader = new StreamReader(GetStream(asm, filePath, addPrefix)))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Gets the content of the resource by using cache strategy.
        /// the expired time of the cache is infinite.
        /// </summary>
        /// <param name="asm">the assembly.</param>
        /// <param name="filePath">the path of the resource.</param>
        /// <param name="addPrefix">the flag to indicate to add the prefix of the assembly name or not.</param>
        /// <returns>the content of the resource.</returns>
        public static string ReadToEndFromCache(Assembly asm, string filePath, bool addPrefix)
        {
            return CacheManager.Instance.GetEntity<string, string>("ResourceCache", filePath,
                TimeSpan.MaxValue, 
                (item) => 
                {
                    return ReadToEnd(asm, filePath, addPrefix);
                }
                );
        }
    }
}
