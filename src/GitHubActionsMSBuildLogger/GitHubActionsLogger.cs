using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GitHubActionsMSBuildLogger
{
    public class GitHubActionsLogger : Logger
    {
        public override void Initialize(IEventSource eventSource)
        {
            Console.WriteLine("Hello World");

            eventSource.MessageRaised += (sender, args) => { };
            eventSource.WarningRaised += (sender, args) => { };
            eventSource.ErrorRaised += (sender, args) => { };
        }
    }
}