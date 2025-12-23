namespace ComputeryLib.Utilities;

using System.Diagnostics.CodeAnalysis;

public static class ArgumentUtility {
    public static bool TryGetArgument(string argumentName, [NotNullWhen(true)] out string? argumentValue) {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] != argumentName) continue;
            argumentValue = args[i + 1];
            return true;
        }
        argumentValue = null;
        return false;
    }
}