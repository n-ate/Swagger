using Microsoft.Extensions.Hosting;
using n_ate.Swagger.CommandLine;

namespace n_ate.Swagger
{
    public static class IHostExtensions
    {
        /// <summary>
        /// Determines if the args contain a recognized n_ate.Swagger command.
        /// </summary>
        /// <param name="args">The arguments passed when the application was executed.</param>
        /// <returns>True if the args contain a recognized command.</returns>
        /// <example>if (app.IsRecognizedSwaggerCommand(args))
        /// {
        ///     await app.RunSwaggerCommandLine(args);
        ///     return; //stops execution because command was valid.
        /// }</example>
        public static bool IsRecognizedSwaggerCommand(this IHost _, string[] args)
        {
            return Commands.IsGeneralHelpCommand(args) || Commands.IsSwaggerMergeCommand(args) || Commands.IsSwaggerGenerateCommand(args);
        }

        /// <summary>
        /// Executes the n_ate.Swagger CLI, if the arguments contain a recognized command. Place before startup.Configure() and app.Run().
        /// </summary>
        /// <param name="args">The arguments passed when the application was executed.</param>
        /// <returns>True if arguments contained a recognized command that was successfully executed.</returns>
        /// <example>if (await app.RunSwaggerCommandLine(args)) return; //stops execution because command was valid.</example>
        public static async Task<bool> RunSwaggerCommandLine(this IHost host, string[] args)
        {
            //args = new string[] { "swagger-merge" }; //mock
            if (Commands.IsGeneralHelpCommand(args))
            {
                Commands.ExecuteHelpCommand();
            }
            else if (Commands.IsSwaggerMergeCommand(args))
            {
                await Commands.ExecuteSwaggerMergeCommand(args);
            }
            else if (Commands.IsSwaggerGenerateCommand(args))
            {
                Commands.ExecuteSwaggerGenerateCommand(host.Services, args);
            }
            else
            {
                Console.WriteLine(@"For Swagger CLI help information, use the ""help"" command/argument.");
                Console.WriteLine("");
                return false;
            }
            return true;
        }
    }
}