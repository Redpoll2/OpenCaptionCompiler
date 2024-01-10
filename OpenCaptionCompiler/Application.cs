using OpenCaptionCompiler.ClosedCaptions;

namespace OpenCaptionCompiler
{
    public static class Application
    {
        public static Task CompileFileAsync(string filePath)
        {
            // TODO: compile lol...
            CaptionFile captions = CaptionFile.ParseFile(filePath);

            return Task.CompletedTask;
        }
    }
}
