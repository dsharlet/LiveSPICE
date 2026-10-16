using LiveSPICE.Avalonia;
using Xunit;

namespace LiveSPICE.Avalonia.Tests;

public sealed class MenuOpenPathTests
{
    [Fact]
    public void OpenPathDecodesFileUriWithSpaces()
    {
        Assert.Equal("/tmp/MXR Phase 90.schx", MainWindow.OpenPath(null, new Uri("file:///tmp/MXR%20Phase%2090.schx")));
    }

    [Fact]
    public void OpenPathPrefersLocalPathWhenAvailable()
    {
        Assert.Equal("/chosen/MXR Phase 90.schx", MainWindow.OpenPath("/chosen/MXR Phase 90.schx", new Uri("file:///tmp/MXR%20Phase%2090.schx")));
    }

    [Fact]
    public void UntouchedStartupDocumentCanBeReplacedByFirstOpen()
    {
        Assert.True(MainWindow.IsUntouchedStartupDocument(SchematicDocument.New()));
    }

    [Fact]
    public void DirtyStartupDocumentIsNotReplacedByFirstOpen()
    {
        SchematicDocument document = SchematicDocument.New();
        document.MarkDirty();

        Assert.False(MainWindow.IsUntouchedStartupDocument(document));
    }
}