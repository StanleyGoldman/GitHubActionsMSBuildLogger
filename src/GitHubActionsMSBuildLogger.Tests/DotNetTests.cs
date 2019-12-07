using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using RunProcessAsTask;
using Xunit;
using Xunit.Abstractions;

namespace GitHubActionsMSBuildLogger.Tests
{
    public class DotNetTests : BuildTestsBase
    {
        public DotNetTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;
        
        private async Task<ProcessResults> BuildAsync(string project, bool nugetRestore = false)
        {
            var loggerPath = GetLoggerPathOrThrow();
            var slnPath = GetSolutionPathOrThrow(project);

            if (nugetRestore)
            {
                await NugetRestoreAsync(slnPath)
                    .ConfigureAwait(false);
            }

            var processResults = await ProcessEx.RunAsync(new ProcessStartInfo("dotnet", $"--version"))
                .ConfigureAwait(false);

            _output.WriteLine("dotnet version {0}", processResults.StandardOutput.First());

            var processStartInfo =
                new ProcessStartInfo("dotnet", $"build {slnPath} /logger:GitHubActionsLogger,{loggerPath}")
                {
                    Environment =
                    {
                        {"GITHUB_ACTION", Guid.NewGuid().ToString()},
                        {"GITHUB_WORKSPACE", TargetPath}
                    }
                };

            return await ProcessEx.RunAsync(processStartInfo)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task TestCodeAnalysis()
        {
            using var processResults = await BuildAsync("codeanalysis")
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);

                warnings.Should().BeEmpty();
                errors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestRoslynCodeAnalysis()
        {
            using var processResults = await BuildAsync("roslyn-codeanalysis")
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);

                warnings.Should().BeEmpty();
                errors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestRoslynator()
        {
            using var processResults = await BuildAsync("roslynator", true)
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);

                warnings.Should().BeEquivalentTo(
                    @"::warning file=TestConsoleApp1/Program.cs,line=9,col=0::RCS1102 Make class static.", "::warning file=TestConsoleApp1/Program.cs,line=16,col=0::RCS1102 Make class static."
                );
                errors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestSimpleProjectError()
        {
            using var processResults = await BuildAsync("simple-project-error")
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);

                warnings.Should().BeEmpty();
                errors.Should().BeEquivalentTo(
                    "::error file=TestConsoleApp1/Program.cs,line=13,col=0::CS1002 ; expected"
                );
            }
        }

        [Fact]
        public async Task TestSimpleProjectInfo()
        {
            using var processResults = await BuildAsync("simple-project-info")
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);

                warnings.Should().BeEmpty();
                errors.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task TestSimpleProjectWarning()
        {
            using var processResults = await BuildAsync("simple-project-warning")
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);

                warnings.Should().BeEquivalentTo(
                    "::warning file=TestConsoleApp1/Program.cs,line=13,col=0::CS0219 The variable 'hello' is assigned but its value is never used"
                );
                errors.Should().BeEmpty();
            }
        }
    }
}