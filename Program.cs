using System;
using System.IO;
using System.Threading.Tasks;
using Altinn2Convert.Commands.Extract;
using Altinn2Convert.Configuration;
using Altinn2Convert.Services;

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn2Convert
{
    /// <summary>
    /// Program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Global environment used in commands.
        /// </summary>
        public static string Environment { get; set; }

        private static readonly string _prompt = "Altinn 2 Convert";
        private static readonly CommandLineApplication<Extract> _extractCmd = new CommandLineApplication<Extract>();
        private static IConfigurationRoot _configuration;

        /// <summary>
        /// Main method.
        /// </summary>
        public static async Task Main()
        {
            _configuration = BuildConfiguration();
            IServiceCollection services = GetAndRegisterServices();
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            _extractCmd.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(serviceProvider);

            while (true)
            {
                if (string.IsNullOrEmpty(Environment))
                {
                    Console.Write($"{_prompt}> ");
                }
                else
                {
                    Console.Write($"{_prompt} [{Environment}]> ");
                }

                string[] args = Console.ReadLine().Trim().Split(' ');

                switch (args[0].ToLower())
                {            
                    case "extract":
                        await _extractCmd.ExecuteAsync(args);
                        break;
                    case "exit":
                        return;
                    default:
                        Console.WriteLine($"Unknown argument {string.Join(" ", args)}, Valid commands are data, instance and settings.");
                        break;
                }
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

            return builder.Build();
        }

        private static IServiceCollection GetAndRegisterServices()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging();

            services.AddSingleton<ITextService, TextService>();
            services.AddSingleton<ILayoutService, LayoutService>();

            services.Configure<GeneralSettings>(_configuration.GetSection("GeneralSettings"));

            return services;
        }
    }
}
