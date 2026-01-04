using System.IO;
using System.Threading.Tasks;

namespace PseudoCLI
{
    internal static class ProcessPumping
    {
        public static async Task PumpAsync(StreamReader reader, TextWriter writer)
        {
            char[] buf = new char[4096];
            int n;
            while ((n = await reader.ReadAsync(buf, 0, buf.Length)) > 0)
                await writer.WriteAsync(buf, 0, n);
        }
    }
}
