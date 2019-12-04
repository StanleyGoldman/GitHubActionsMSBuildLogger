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
    public class MsBuildTests : BuildTestsBase
    {
        public MsBuildTests(ITestOutputHelper output)
        {
            _output = output;
            _msbuildExec = Environment.GetEnvironmentVariable("GitHubActionsMSBuildLogger_MsBuildPath");
            if (string.IsNullOrEmpty(_msbuildExec)) _msbuildExec = "msbuild";
        }

        private readonly ITestOutputHelper _output;
        private readonly string _msbuildExec;

        private async Task<ProcessResults> BuildAsync(string project)
        {
            var loggerPath = GetLoggerPathOrThrow();
            var slnPath = GetSolutionPathOrThrow(project);

            var processStartInfo =
                new ProcessStartInfo(_msbuildExec, $"/logger:GitHubActionsLogger,{loggerPath} {slnPath}")
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

                warnings.Should().BeEquivalentTo(
                    "::warning file=TestConsoleApp1,line=0,col=0:: Post-build Code Analysis (FxCopCmd.exe) has been deprecated in favor of FxCop analyzers, which run during build. Refer to https://aka.ms/fxcopanalyzers to migrate to FxCop analyzers.",
                    "::warning file=TestConsoleApp1/Program.cs,line=20,col=0::CA2213 Microsoft.Usage : 'Program.MyClass' contains field 'Program.MyClass._inner' that is of IDisposable type: 'Program.MyOTherClass'. Change the Dispose method on 'Program.MyClass' to call Dispose or Close on this field."
                );
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