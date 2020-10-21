using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace Altinn2Convert.Commands.Extract
{
    /// <summary>
    /// Extract command handler.
    /// </summary>
    [Command(
      Name = "extract",
      OptionsComparison = StringComparison.InvariantCultureIgnoreCase,
      UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue)]
    [Subcommand(typeof(Texts))]
    [Subcommand(typeof(Layout))]
    public class Extract : IBaseCmd
    {
        /// <inheritdoc/>    
        protected override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }
}
