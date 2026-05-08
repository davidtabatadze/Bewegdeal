using System.Text;

namespace Bewegdeal.Tools
{
    /// <summary>
    /// Provides utility methods for generating cache keys based on a base key and additional parameters.
    /// </summary>
    /// <remarks>This class is intended to help standardize cache key formats when storing or retrieving items
    /// from a cache. It is static and cannot be instantiated.</remarks>
    public static class CacheKeyTool
    {
        /// <summary>
        /// Creates a composite string by combining the specified key with one or more parameter values, separated by
        /// colons.
        /// </summary>
        /// <remarks>Null values in the parameters array are ignored and not included in the resulting
        /// string.</remarks>
        /// <param name="key">The base string to use as the key. Cannot be null.</param>
        /// <param name="parameters">An array of parameter values to append to the key. Each non-null value is converted to a string and
        /// appended, separated by colons.</param>
        /// <returns>A string consisting of the key followed by each non-null parameter value, separated by colons. If no
        /// parameters are provided, returns the key.</returns>
        public static string Get(string key, params object[] parameters)
        {

            // start up key build
            var builder = new StringBuilder();
            builder.Append(key);

            // apply valid parameters
            foreach (var parameter in parameters)
            {
                if (parameter != null)
                {
                    builder.Append(':').Append(parameter.ToString());
                }
            }

            // the key is ready
            return builder.ToString();
        }

    }
}
