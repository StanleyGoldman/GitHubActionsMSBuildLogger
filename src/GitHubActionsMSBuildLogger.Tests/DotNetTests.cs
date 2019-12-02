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
                    .Contain("::error file=TestConsoleApp1/Program.cs,line=13,col=0::; expected");
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
                        "::warning file=TestConsoleApp1/Program.cs,line=13,col=0::The variable 'hello' is assigned but its value is never used");
            }
        }
    }
}