using Nuke.Common.Tooling;
using Nuke.Common.Utilities;

namespace Nuke.Common;

public static class ProcessExtensions2
{
    public static string ErrToText(this IEnumerable<Output> output)
    {
        return output.Where(x => x.Type == OutputType.Err)
            .Select(x => x.Text)
            .JoinNewLine();
    }
}