using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// A collection of simple helper methods.
    /// </summary>
    public static class Helpers
    {

        /// <summary>
        /// Checks if 2 byte arrays are equal.
        /// This is currently only used to compare checksums.
        /// </summary>
        /// <param name="a1">The first array.</param>
        /// <param name="a2">The second array.</param>
        /// <returns>True if they are equal.</returns>
        public static unsafe bool ByteArrayEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;
            // We use an unsafe block here, because
            // array lookups are expensive.
            int cnt = a1.Length;
            int i = 0;
            fixed (byte* a12 = a1)
            {
                fixed (byte* a22 = a2)
                {
                    byte* a1p = a12;
                    byte* a2p = a22;
                    while (i < cnt)
                    {
                        if (*a1p != *a2p)
                            return false;
                        a1p++;
                        a2p++;
                        i++;
                    }
                    return true;
                }
            }
        }

    }
}
