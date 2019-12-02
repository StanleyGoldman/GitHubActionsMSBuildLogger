using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using RunProcessAsTask;
using Xunit;

namespace GitHubActionsMSBuildLogger.Tests
{
    public class MsBuildTests : BuildTestsBase
    {
        private readonly string _msbuildExec;

        public MsBuildTests()
        {
            _msbuildExec = Environment.GetEnvironmentVariable("GitHubActionsMSBuildLogger_MsBuildPath");
            if (string.IsNullOrEmpty(_msbuildExec)) _msbuildExec = "msbuild";
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
            var loggerPath = GetLoggerPathOrThrow();
            var slnPath = GetSolutionPathOrThrow(project);

            return await ProcessEx
                .RunAsync(_msbuildExec, $"/logger:GitHubActionsLogger,{loggerPath} {slnPath}")
                .ConfigureAwait(false);
        }
    }
}