using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using RunProcessAsTask;
using Xunit;

namespace GitHubActionsMSBuildLogger.Tests
{
    public abstract class BuildTestsBase : IDisposable
    {
        private readonly string _targetPath;

        protected BuildTestsBase()
        {
            _targetPath = Path.Combine(Path.GetTempPath(), "GitHubActionsMSBuildLogger.Tests",
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(_targetPath);
        }

        public void Dispose()
        {
            Directory.Delete(_targetPath, true);
        }

        protected string GetLoggerPathOrSkip()
        {
            var variable = "GitHubActionsMSBuildLogger_LoggerPath";
            var loggerPath = Environment.GetEnvironmentVariable(variable);
            Skip.If(string.IsNullOrWhiteSpace(loggerPath), $"Environment Variable '{variable}' is not set");

            var exists = File.Exists(loggerPath);
            Skip.IfNot(exists, $"Solution '{loggerPath}' does not exist");

            var fileInfo = new FileInfo(loggerPath);
            var targetLoggerPath = Path.Combine(_targetPath, fileInfo.Name);

            File.Copy(loggerPath, targetLoggerPath);

            return targetLoggerPath;
        }

        protected string GetSolutionPathOrSkip(string project)
        {
            var variable = "GitHubActionsMSBuildLogger_TestResourcePath";
            var testResourcePath = Environment.GetEnvironmentVariable(variable);
            Skip.If(string.IsNullOrWhiteSpace(testResourcePath), $"Environment Variable '{variable}' is not set");

            var solutionPath = Path.Join(testResourcePath, project);
            var exists = Directory.Exists(solutionPath);
            Skip.IfNot(exists, $"Solution '{solutionPath}' does not exist");

            CopyFiles(solutionPath, _targetPath);

            return Path.Combine(_targetPath, "TestConsoleApp1.sln");
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

    public class MsBuildTests : BuildTestsBase
    {
        private readonly string _msbuildExec;

        public MsBuildTests()
        {
            _msbuildExec = Environment.GetEnvironmentVariable("GitHubActionsMSBuildLogger_MsBuildPath");
            if (string.IsNullOrEmpty(_msbuildExec)) _msbuildExec = "msbuild";
        }

        [SkippableFact]
        public async Task TestSimpleProjectInfo()
        {
            using var processResults = await BuildAsync("simple-project-info")
                .ConfigureAwait(false);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);
                processResults.StandardOutput.Should().Contain("Hello World");
            }
        }

        [SkippableFact]
        public async Task TestSimpleProjectWarning()
        {
            using var processResults = await BuildAsync("simple-project-warning")
                .ConfigureAwait(false);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);
                processResults.StandardOutput.Should().Contain("Hello World");
            }
        }

        [SkippableFact]
        public async Task TestSimpleProjectError()
        {
            using var processResults = await BuildAsync("simple-project-error")
                .ConfigureAwait(false);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);
                processResults.StandardOutput.Should().Contain("Hello World");
            }
        }

        [SkippableFact]
        public async Task TestRoslynator()
        {
            using var processResults = await BuildAsync("roslynator")
                .ConfigureAwait(false);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);
                processResults.StandardOutput.Should().Contain("Hello World");
            }
        }

        [SkippableFact]
        public async Task TestCodeAnalysis()
        {
            using var processResults = await BuildAsync("codeanalysis")
                .ConfigureAwait(false);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);
                processResults.StandardOutput.Should().Contain("Hello World");
            }
        }

        private async Task<ProcessResults> BuildAsync(string project)
        {
            var loggerPath = GetLoggerPathOrSkip();
            var slnPath = GetSolutionPathOrSkip(project);

            return await ProcessEx
                .RunAsync(_msbuildExec, $"/logger:GitHubActionsLogger,{loggerPath} {slnPath}")
                .ConfigureAwait(false);
        }
    }
}