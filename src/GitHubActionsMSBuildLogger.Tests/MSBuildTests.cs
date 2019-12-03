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

            var buildOutput = string.Join(Environment.NewLine, processResults.StandardOutput);
            var errorOutput = string.Join(Environment.NewLine, processResults.StandardError);

            _output.WriteLine($"STDOUT:{Environment.NewLine}{buildOutput}");
            _output.WriteLine($"STDERR:{Environment.NewLine}{errorOutput}");

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);
            }
        }

        [Fact]
        public async Task TestRoslynator()
        {
            using var processResults = await BuildAsync("roslynator")
                .ConfigureAwait(false);

            var buildOutput = string.Join(Environment.NewLine, processResults.StandardOutput);
            var errorOutput = string.Join(Environment.NewLine, processResults.StandardError);

            _output.WriteLine($"STDOUT:{Environment.NewLine}{buildOutput}");
            _output.WriteLine($"STDERR:{Environment.NewLine}{errorOutput}");

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);
            }
        }

        [Fact]
        public async Task TestSimpleProjectError()
        {
            using var processResults = await BuildAsync("simple-project-error")
                .ConfigureAwait(false);

            var buildOutput = string.Join(Environment.NewLine, processResults.StandardOutput);
            var errorOutput = string.Join(Environment.NewLine, processResults.StandardError);

            _output.WriteLine($"STDOUT:{Environment.NewLine}{buildOutput}");
            _output.WriteLine($"STDERR:{Environment.NewLine}{errorOutput}");

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(1);
                processResults.StandardOutput.Should()
                    .Contain("::error file=TestConsoleApp1/Program.cs,line=13,col=0::CS1002 ; expected");
            }
        }


        [Fact]
        public async Task TestSimpleProjectInfo()
        {
            using var processResults = await BuildAsync("simple-project-info")
                .ConfigureAwait(false);

            var buildOutput = string.Join(Environment.NewLine, processResults.StandardOutput);
            var errorOutput = string.Join(Environment.NewLine, processResults.StandardError);

            _output.WriteLine($"STDOUT:{Environment.NewLine}{buildOutput}");
            _output.WriteLine($"STDERR:{Environment.NewLine}{errorOutput}");

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);
            }
        }

        [Fact]
        public async Task TestSimpleProjectWarning()
        {
            using var processResults = await BuildAsync("simple-project-warning")
                .ConfigureAwait(false);

            var buildOutput = string.Join(Environment.NewLine, processResults.StandardOutput);
            var errorOutput = string.Join(Environment.NewLine, processResults.StandardError);

            _output.WriteLine($"STDOUT:{Environment.NewLine}{buildOutput}");
            _output.WriteLine($"STDERR:{Environment.NewLine}{errorOutput}");

            using (new AssertionScope())
            {
                processResults.ExitCode.Should().Be(0);
                processResults.StandardOutput.Should()
                    .Contain(
                        "::warning file=TestConsoleApp1/Program.cs,line=13,col=0::CS0219 The variable 'hello' is assigned but its value is never used");
            }
        }
    }
}