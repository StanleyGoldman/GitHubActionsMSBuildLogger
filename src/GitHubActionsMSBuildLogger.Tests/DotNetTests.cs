using System;
using System.Diagnostics;
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

        private async Task<ProcessResults> BuildAsync(string simpleProjectInfo)
        {
            var loggerPath = GetLoggerPathOrThrow();
            var slnPath = GetSolutionPathOrThrow(simpleProjectInfo);

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
            using var processResults = await BuildAsync("roslynator")
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);

                warnings.Should().BeEmpty();
                errors.Should().BeEquivalentTo(
                    @"::error file=TestConsoleApp1/CSC,line=0,col=0::CS0006 Metadata file '..\packages\Roslynator.Analyzers.1.9.0\analyzers\dotnet\cs\Roslynator.Common.dll' could not be found",
                    @"::error file=TestConsoleApp1/CSC,line=0,col=0::CS0006 Metadata file '..\packages\Roslynator.Analyzers.1.9.0\analyzers\dotnet\cs\Roslynator.Common.Workspaces.dll' could not be found",
                    @"::error file=TestConsoleApp1/CSC,line=0,col=0::CS0006 Metadata file '..\packages\Roslynator.Analyzers.1.9.0\analyzers\dotnet\cs\Roslynator.CSharp.Analyzers.CodeFixes.dll' could not be found",
                    @"::error file=TestConsoleApp1/CSC,line=0,col=0::CS0006 Metadata file '..\packages\Roslynator.Analyzers.1.9.0\analyzers\dotnet\cs\Roslynator.CSharp.Analyzers.dll' could not be found",
                    @"::error file=TestConsoleApp1/CSC,line=0,col=0::CS0006 Metadata file '..\packages\Roslynator.Analyzers.1.9.0\analyzers\dotnet\cs\Roslynator.CSharp.dll' could not be found",
                    @"::error file=TestConsoleApp1/CSC,line=0,col=0::CS0006 Metadata file '..\packages\Roslynator.Analyzers.1.9.0\analyzers\dotnet\cs\Roslynator.CSharp.Workspaces.dll' could not be found"
                );
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