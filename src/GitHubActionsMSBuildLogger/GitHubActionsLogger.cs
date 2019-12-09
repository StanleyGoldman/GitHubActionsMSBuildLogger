using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using GitHubActionsMSBuildLogger.Extensions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GitHubActionsMSBuildLogger
{
    public class GitHubActionsLogger : Logger
    {
        private Action<string> _debugOutput;
        private Action<string> _output;
        public bool IsGitHubActionRunner => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTION"));
        public string Workspace => Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        
        public override void Initialize(IEventSource eventSource)
        {
            if (eventSource == null)
                throw new ArgumentNullException(nameof(eventSource));

            var debugEnvVariable = Environment.GetEnvironmentVariable("GitHubActionsLogger_Debug");
            var isDebug =!(string.IsNullOrWhiteSpace(debugEnvVariable) || debugEnvVariable == "0" || debugEnvVariable.Equals("false", StringComparison.CurrentCultureIgnoreCase));

            _output = s => Console.WriteLine($"{s}");
            _debugOutput = isDebug
                ? (Action<string>) (s => _output($"GHAL: {s}"))
                : _ => { };

            _output($"GitHubActionsLogger GitHub Actions Detected");
            _output($"GitHubActionsLogger Running v{GetType().GetTypeInfo().Assembly.GetName().Version}");
            _output(string.Empty);

            if (!IsGitHubActionRunner)
            {
                _debugOutput("Not in GitHub Actions; Disabling");
                return;
            }

            eventSource.WarningRaised += (sender, args) => ProcessRecord(args);
            eventSource.ErrorRaised += (sender, args) => ProcessRecord(args);
        }

        public void ProcessRecord(BuildEventArgs recordArgs)
        {
            try
            {
                int lineNumber;

                var buildWarning = recordArgs as BuildWarningEventArgs;
                var buildError = recordArgs as BuildErrorEventArgs;

                if (buildWarning == null && buildError == null)
                {
                    return;
                }

                string buildCode;
                string projectFile;
                string file;
                string message;
                string code;
                string level;

                if (buildWarning != null)
                {
                    level = "warning";

                    buildCode = buildWarning.Code;
                    projectFile = buildWarning.ProjectFile;
                    file = buildWarning.File;
                    code = buildWarning.Code;
                    message = buildWarning.Message;
                    lineNumber = buildWarning.LineNumber;
                }
                else
                {
                    level = "error";

                    buildCode = buildError.Code;
                    projectFile = buildError.ProjectFile;
                    file = buildError.File;
                    code = buildError.Code;
                    message = buildError.Message;
                    lineNumber = buildError.LineNumber;
                }

                if (buildCode.StartsWith("MSB"))
                {
                    if (projectFile == null)
                    {
                        projectFile = file;
                    }
                    else
                    {
                        file = projectFile;
                    }
                }

                var filePath = GetFilePath(projectFile ?? file, file);

                _debugOutput($"{level} - file={filePath},line={lineNumber},col={0} - {code} {message}");

                _output($"::{level} file={filePath},line={lineNumber},col={0}::{code} {message}");
            }
            catch (Exception e)
            {
                _debugOutput($"Error Caught: {e}");
            }
        }


        private string GetFilePath(string projectFile, string file)
        {
            if (projectFile == null) throw new ArgumentNullException(nameof(projectFile));

            if (file == null) throw new ArgumentNullException(nameof(file));

            var filePath = Path.Combine(Path.GetDirectoryName(projectFile), file);
            if (filePath.IsSubPathOf(Workspace)) return GetRelativePath(filePath, Workspace).Replace("\\", "/");

            var dotNugetPosition = filePath.IndexOf(".nuget");
            if (dotNugetPosition != -1) return filePath.Substring(dotNugetPosition).Replace("\\", "/");

            throw new InvalidOperationException($"FilePath `{filePath}` is not a child of `{Workspace}`");
        }

        private static string GetRelativePath(string filespec, string folder)
        {
            //https://stackoverflow.com/a/703292/104877

            var pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) folder += Path.DirectorySeparatorChar;

            var folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }
    }
}