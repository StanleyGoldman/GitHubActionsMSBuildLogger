using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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

        private async Task<ProcessResults> BuildAsync(string project, bool nugetRestore = false)
        {
            var loggerPath = GetLoggerPathOrThrow();
            var slnPath = GetSolutionPathOrThrow(project);

            if (nugetRestore)
            {
                await NugetRestoreAsync(slnPath)
                    .ConfigureAwait(false);
            }
            
            var version = await GetMsBuildVersionAsync()
                .ConfigureAwait(false);

            _output.WriteLine("msbuild version {0}", version);

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

        private async Task<string> GetMsBuildVersionAsync()
        {
            var processResults = await ProcessEx.RunAsync(new ProcessStartInfo(_msbuildExec, $"-version"))
                .ConfigureAwait(false);

            var regex = new Regex(@"^Microsoft \(R\) Build Engine version (.*?)\s.*$");
            var match = regex.Match(processResults.StandardOutput.First());
            
            return match.Groups[1].Value;
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
            using var processResults = await BuildAsync("roslyn-codeanalysis", true)
                .ConfigureAwait(false);

            var (warnings, errors) = OutputResults(processResults, _output);

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);

                warnings.Should().BeEquivalentTo(
                    @"::warning file=TestConsoleApp1/Program.cs,line=15,col=0::CA1812 Program.MyClass is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it static (Shared in Visual Basic).",
                    @"::warning file=TestConsoleApp1/Program.cs,line=11,col=0::CA1801 Parameter args of method Main is never used. Remove the parameter or use it in the method body.",
                    @"::warning file=TestConsoleApp1/Program.cs,line=17,col=0::CA1823 Unused field '_inner'.",
                    @"::warning file=TestConsoleApp1/Program.cs,line=17,col=0::CA2213 'MyClass' contains field '_inner' that is of IDisposable type 'MyOTherClass', but it is never disposed. Change the Dispose method on 'MyClass' to call Close or Dispose on this field."
                    );
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