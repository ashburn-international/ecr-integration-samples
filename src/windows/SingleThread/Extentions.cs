using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleThread
{
    public static class Extentions
    {
        public static Array ToIntArray(this byte[] bytes)
        {
            return (System.Array)bytes.Select(i => (int)i).ToArray();
        }

        public static Array ToByteArray(this byte[] bytes)
        {
            return (System.Array)bytes;
        }

        public static byte[] ToBytes(this Array array)
        {
            if (array == null) return Array.Empty<byte>();

            var bytes = new byte[array.Length];
            Array.Copy(array, bytes, array.Length);
            return bytes;
        }
    }
}
