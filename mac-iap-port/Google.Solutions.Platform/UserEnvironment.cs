using System;

namespace Google.Solutions.Platform
{
    public static class UserEnvironment
    {
        public static string? ExpandEnvironmentStrings(string? source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }
            return Environment.ExpandEnvironmentVariables(source);
        }

        public static bool TryResolveAppPath(string exeName, out string? path)
        {
            path = null;
            return false;
        }
    }
}
