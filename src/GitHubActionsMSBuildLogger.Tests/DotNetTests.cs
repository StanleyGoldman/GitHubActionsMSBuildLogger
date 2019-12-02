using System.Threading.Tasks;
using Xunit;

namespace GitHubActionsMSBuildLogger.Tests
{
    public class DotNetTests
    {
        [Fact]
        public async Task Test1()
        {
            var processResults = await RunProcessAsTask.ProcessEx.RunAsync("dotnet")
                .ConfigureAwait(false);
        }
    }
}