using System.Diagnostics;
using System.Reflection;

namespace SRML.Utils
{
    public static class ReflectionUtils
    {
        internal static readonly Assembly ourAssembly = Assembly.GetExecutingAssembly();

        public static Assembly GetRelevantAssembly()
        {
            var trace = new StackTrace();

            var frames = trace.GetFrames();
            try // TODO: Clean this up, choose a better solution (check for non mod or srml dlls)
            {
                foreach (var frame in frames) {
                    var theirAssembly = frame.GetMethod().DeclaringType.Assembly;
                    if (theirAssembly != ourAssembly) return theirAssembly;
                }
            }
            catch { /* todo */ }

            return ourAssembly;
        }
    }
}