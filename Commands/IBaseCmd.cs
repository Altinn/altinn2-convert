using System;
using System.Reflection;
using System.Threading.Tasks;

using McMaster.Extensions.CommandLineUtils;

namespace Altinn2Convert.Commands
{
    /// <summary>
    /// Abstract baseclass for all commands
    /// </summary>
    [HelpOption("--help")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    public abstract class IBaseCmd
    {
        /// <summary>
        /// Async execution of a command
        /// </summary>
        protected virtual Task OnExecuteAsync(CommandLineApplication app)
        {
            return Task.CompletedTask;
        }

        private static string GetVersion()
       => typeof(IBaseCmd).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
