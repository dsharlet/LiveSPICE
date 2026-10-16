using System.Linq;
using Circuit;
using LiveSPICE.Avalonia;
using Xunit;

namespace LiveSPICE.Avalonia.Tests;

public class ComponentLibraryTests
{
    [Fact]
    public void LibrariesAreFoundAndLoaded()
    {
        var parts = ComponentLibrary.Load();

        // All four shipped libraries should contribute; previously only Tubes.xml was copied to
        // the build output, so a host beside its executable found one of four.
        Assert.True(parts.Count > 80, $"Expected the full library, got {parts.Count} parts.");
        foreach (string category in new[] { "Diodes", "Op-Amps", "Transistors", "Tubes" })
            Assert.Contains(parts, i => i.Category == category);
    }

    [Fact]
    public void PartsCarryTheirLibraryParameters()
    {
        LibraryPart part = ComponentLibrary.Load().Single(i => i.Name == "2N3904");

        BipolarJunctionTransistor bjt = Assert.IsType<BipolarJunctionTransistor>(part.Create());
        Assert.Equal(1e-14, (double)bjt.IS, 15);
        Assert.Equal(300, (double)bjt.BF, 6);
        Assert.Equal(4, (double)bjt.BR, 6);
    }

    [Fact]
    public void EachPlacementGetsItsOwnInstance()
    {
        // Components are mutable, so a shared prototype would let an edit to one placed part show
        // up in the next one.
        LibraryPart part = ComponentLibrary.Load().Single(i => i.Name == "2N3904");

        Circuit.Component first = part.Create();
        Circuit.Component second = part.Create();
        Assert.NotSame(first, second);

        ((BipolarJunctionTransistor)first).BF = Quantity.Parse("999");
        Assert.Equal(300, (double)((BipolarJunctionTransistor)second).BF, 6);
    }
}
