#if UNITY_STANDALONE_LINUX

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SFB {
    // Native Linux implementation of IStandaloneFileBrowser for Unity.
    // Intent: Binds Unity file panel calls to native Linux desktop file pickers.
    // Execution; Fallback: Attempts to execute zenity first; 
    // if unavailable, gracefully falls back to kdialog.
    // Uses an ASCII record separator (<c>(char)28</c>) for Zenity multi-selection to safely handle spaces and special characters in filenames.
    // Normalizes directory paths with trailing slashes for Zenity compatibility.

    public class StandaloneFileBrowserLinux : IStandaloneFileBrowser {
        private const char MultiSelectSeparator = (char)28;

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
            var zenityArgs = new List<string> { "--file-selection" };
            AddOption(zenityArgs, "--title", title);
            AddInitialDirectory(zenityArgs, directory);
            AddZenityFilters(zenityArgs, extensions);
            if (multiselect) {
                zenityArgs.Add("--multiple");
                AddOption(zenityArgs, "--separator", MultiSelectSeparator.ToString());
            }

            var kdialogArgs = new List<string> { "--title", title, "--getopenfilename" };
            if (multiselect)
                kdialogArgs.AddRange(new[] { "--multiple", "--separate-output" });
            kdialogArgs.Add(string.IsNullOrEmpty(directory) ? Environment.CurrentDirectory : directory);
            kdialogArgs.Add(GetKdialogFilter(extensions));

            return ShowDialog(zenityArgs, kdialogArgs, multiselect);
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect) {
            var zenityArgs = new List<string> { "--file-selection", "--directory" };
            AddOption(zenityArgs, "--title", title);
            AddInitialDirectory(zenityArgs, directory);
            if (multiselect) {
                zenityArgs.Add("--multiple");
                AddOption(zenityArgs, "--separator", MultiSelectSeparator.ToString());
            }

            var kdialogArgs = new List<string> { "--title", title, "--getexistingdirectory",
                string.IsNullOrEmpty(directory) ? Environment.CurrentDirectory : directory };

            return ShowDialog(zenityArgs, kdialogArgs, multiselect);
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions) {
            var initialPath = string.IsNullOrEmpty(directory)
                ? defaultName
                : Path.Combine(directory, defaultName ?? string.Empty);

            var zenityArgs = new List<string> { "--file-selection", "--save", "--confirm-overwrite" };
            AddOption(zenityArgs, "--title", title);
            if (!string.IsNullOrEmpty(initialPath))
                AddOption(zenityArgs, "--filename", initialPath);
            AddZenityFilters(zenityArgs, extensions);

            var kdialogArgs = new List<string> { "--title", title, "--getsavefilename",
                string.IsNullOrEmpty(initialPath) ? Environment.CurrentDirectory : initialPath,
                GetKdialogFilter(extensions) };

            var paths = ShowDialog(zenityArgs, kdialogArgs, false);
            return paths.Length == 0 ? string.Empty : AddDefaultExtension(paths[0], extensions);
        }

        private static string[] ShowDialog(List<string> zenityArgs, List<string> kdialogArgs, bool multiselect) {
            string output;
            if (TryRunDialog("zenity", zenityArgs, out output))
                return SplitPaths(output, MultiSelectSeparator);

            if (TryRunDialog("kdialog", kdialogArgs, out output))
                return SplitPaths(output, multiselect ? '\n' : '\0');

            UnityEngine.Debug.LogError("No Linux file dialog was found. Install zenity or kdialog to open files and folders.");
            return new string[0];
        }

        private static bool TryRunDialog(string executable, List<string> arguments, out string output) {
            try {
                using (var process = new Process()) {
                    process.StartInfo = new ProcessStartInfo {
                        FileName = executable,
                        Arguments = BuildArguments(arguments),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    process.Start();
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return true;
                }
            }
            catch (Win32Exception) {
                output = string.Empty;
                return false;
            }
        }

        private static void AddInitialDirectory(List<string> arguments, string directory) {
            if (string.IsNullOrEmpty(directory))
                return;

            var initialDirectory = directory.EndsWith("/", StringComparison.Ordinal)
                ? directory
                : directory + "/";
            AddOption(arguments, "--filename", initialDirectory);
        }

        private static void AddZenityFilters(List<string> arguments, ExtensionFilter[] extensions) {
            if (extensions == null)
                return;

            foreach (var filter in extensions) {
                var patterns = GetPatterns(filter.Extensions);
                if (patterns.Length > 0) {
                    AddOption(arguments, "--file-filter",
                        string.IsNullOrEmpty(filter.Name) ? patterns : filter.Name + " | " + patterns);
                }
            }
        }

        private static string GetKdialogFilter(ExtensionFilter[] extensions) {
            if (extensions == null)
                return string.Empty;

            return string.Join("\n", extensions
                .Where(filter => filter.Extensions != null && filter.Extensions.Length > 0)
                .Select(filter => GetPatterns(filter.Extensions) + "|" + filter.Name));
        }

        private static string GetPatterns(string[] extensions) {
            if (extensions == null)
                return string.Empty;

            return string.Join(" ", extensions.Select(extension => "*." + extension));
        }

        private static string AddDefaultExtension(string path, ExtensionFilter[] extensions) {
            if (extensions == null || extensions.Length == 0 || extensions[0].Extensions == null
                || extensions[0].Extensions.Length == 0 || Path.HasExtension(path))
                return path;

            return path + "." + extensions[0].Extensions[0];
        }

        private static void AddOption(List<string> arguments, string option, string value) {
            arguments.Add(option);
            arguments.Add(value ?? string.Empty);
        }

        private static string BuildArguments(List<string> arguments) {
            return string.Join(" ", arguments.Select(QuoteArgument));
        }

        private static string QuoteArgument(string argument) {
            return "\"" + argument.Replace("\"", "\\\"") + "\"";
        }

        private static string[] SplitPaths(string output, char separator) {
            if (string.IsNullOrEmpty(output))
                return new string[0];

            return output.TrimEnd('\r', '\n')
                .Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

#endif