using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace GitHubActionsMSBuildLogger.Tests
{
    public abstract class BuildTestsBase : IDisposable
    {
        protected readonly string TargetPath;

        protected BuildTestsBase()
        {
            TargetPath = Path.Combine(Path.GetTempPath(), "GitHubActionsMSBuildLogger.Tests",
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(TargetPath);
        }

        public void Dispose()
        {
            Directory.Delete(TargetPath, true);
        }

        protected string GetLoggerPathOrThrow()
        {
            var variable = "GitHubActionsMSBuildLogger_LoggerPath";
            var loggerPath = Environment.GetEnvironmentVariable(variable);
            string.IsNullOrWhiteSpace(loggerPath)
                .Should()
                .BeFalse($"environment variable '{variable}' should be set");

            var exists = File.Exists(loggerPath);
            exists.Should().BeTrue($"logger file '{loggerPath}' should exist");

            var fileInfo = new FileInfo(loggerPath);
            var targetLoggerPath = Path.Combine(TargetPath, fileInfo.Name);

            File.Copy(loggerPath, targetLoggerPath);

            return targetLoggerPath;
        }

        protected string GetSolutionPathOrThrow(string project)
        {
            var variable = "GitHubActionsMSBuildLogger_TestResourcePath";
            var testResourcePath = Environment.GetEnvironmentVariable(variable);

            string.IsNullOrWhiteSpace(testResourcePath)
                .Should()
                .BeFalse($"environment variable '{variable}' should be set");

            var solutionPath = Path.Join(testResourcePath, project);
            var exists = Directory.Exists(solutionPath);
            exists.Should().BeTrue($"solution file '{solutionPath}' should exist");

            CopyFiles(solutionPath, TargetPath);

            return Path.Combine(TargetPath, "TestConsoleApp1.sln");
        }

        private void CopyFiles(string sourcePath, string destinationPath)
        {
            //https://stackoverflow.com/a/3822913/104877

            //Now Create all of the directories
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (var newPath in Directory.GetFiles(sourcePath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }
    }
}