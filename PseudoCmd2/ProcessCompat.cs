using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PseudoCLI
{
    internal static class ProcessCompat
    {
        public static Task WaitForExitAsyncCompat(Process process)
        {
            if (process.HasExited)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<object>();

            EventHandler handler = null;
            handler = (s, e) =>
            {
                process.Exited -= handler;
                tcs.TrySetResult(null);
            };

            process.Exited += handler;

            if (process.HasExited)
            {
                process.Exited -= handler;
                tcs.TrySetResult(null);
            }

            return tcs.Task;
        }
    }
}
