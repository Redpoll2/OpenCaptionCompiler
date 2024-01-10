using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace OpenCaptionCompiler
{
    internal static class Program
    {
        internal static async Task Main(string[] args)
        {
            var filePathArgument = new Argument<string>(
                name: "file",
                description: "The text file that contains closed captions in a key-value format.");

            filePathArgument.AddValidator((result) =>
            {
                string filePath = result.GetValueOrDefault<string>();

                if (File.Exists(filePath) == false)
                    result.ErrorMessage = $"Could not find \"{filePath}\" file.";
            });

            var compileFileCommand = new RootCommand("Compiles closed captions stored in text file (.txt) into binary file (.dat)");
            compileFileCommand.AddArgument(filePathArgument);
            compileFileCommand.SetHandler(Application.CompileFileAsync, filePathArgument);

            var parser = new CommandLineBuilder(compileFileCommand)
                .UseDefaults()
                .Build();

            await parser.InvokeAsync(args);
        }
    }
}
