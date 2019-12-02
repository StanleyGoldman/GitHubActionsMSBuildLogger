using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using RunProcessAsTask;
using Xunit;

namespace GitHubActionsMSBuildLogger.Tests
{
    public class DotNetTests : BuildTestsBase
    {

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


        private async Task<ProcessResults> BuildAsync(string simpleProjectInfo)
        {
            var loggerPath = GetLoggerPathOrSkip();
            var slnPath = GetSolutionPathOrSkip(simpleProjectInfo);

            return await ProcessEx.RunAsync("dotnet", $"build {slnPath} /logger:GitHubActionsLogger,{loggerPath}")
                .ConfigureAwait(false);
        }
    }
}