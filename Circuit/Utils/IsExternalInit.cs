///<summary>
/// Required due to a 'bug' in .net standard <see href=">https://github.com/dotnet/roslyn/issues/45510"/>
/// </summary>
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
