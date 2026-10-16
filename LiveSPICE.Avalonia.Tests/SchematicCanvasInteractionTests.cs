using Circuit;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using LiveSPICE.Avalonia;
using Xunit;

namespace LiveSPICE.Avalonia.Tests;

public sealed class SchematicCanvasInteractionTests
{
    [AvaloniaFact]
    public void ClickSelectsSymbol()
    {
        TestContext context = TestContext.Load();
        Symbol symbol = context.FirstSymbol;

        Assert.True(context.Canvas.TestClick(Center(symbol)));
        Assert.Same(symbol.Component, context.Canvas.SelectedObjects.Single());
    }

    [AvaloniaFact]
    public void ControlClickTogglesSelection()
    {
        TestContext context = TestContext.Load();
        Symbol first = context.FirstSymbol;
        Symbol second = context.SecondSymbol;

        context.Canvas.TestClick(Center(first));
        context.Canvas.TestClick(Center(second), control: true);
        Assert.Equal(2, context.Canvas.SelectedObjects.Count);

        context.Canvas.TestClick(Center(first), control: true);
        Assert.Single(context.Canvas.SelectedObjects);
    }

    [AvaloniaFact]
    public void DragSelectedSymbolRecordsOneUndo()
    {
        TestContext context = TestContext.Load();
        Symbol symbol = context.FirstSymbol;
        Coord before = symbol.Position;
        Coord from = Center(symbol);
        Coord to = from + new Coord(30, 0);

        context.Canvas.TestClick(from);
        context.Canvas.TestDragSelected(from, to);

        Assert.Equal(before + new Coord(30, 0), symbol.Position);
        Assert.True(context.Document.CanUndo);
        context.Document.Undo();
        Assert.Equal(before, symbol.Position);
        context.Document.Redo();
        Assert.Equal(before + new Coord(30, 0), symbol.Position);
    }

    [AvaloniaFact]
    public void RectangleDragSelectsMultipleElements()
    {
        TestContext context = TestContext.Load();
        Coord lower = context.Document.Schematic.LowerBound - new Coord(20, 20);
        Coord upper = context.Document.Schematic.UpperBound + new Coord(20, 20);

        context.Canvas.TestDragSelect(lower, upper);

        Assert.True(context.Canvas.SelectedObjects.Count >= 2);
    }

    [AvaloniaFact]
    public void DeleteSelectionIsUndoable()
    {
        TestContext context = TestContext.Load();
        int before = context.Document.Schematic.Elements.Count();

        context.Canvas.TestClick(Center(context.FirstSymbol));
        context.Canvas.DeleteSelection();

        Assert.Equal(before - 1, context.Document.Schematic.Elements.Count());
        context.Document.Undo();
        Assert.Equal(before, context.Document.Schematic.Elements.Count());
    }

    [AvaloniaFact]
    public void WireClicksCreateUndoableWire()
    {
        TestContext context = TestContext.Load();
        int before = context.Document.Schematic.Wires.Count();
        Coord a = new Coord(-120, -80);
        Coord b = new Coord(-60, -80);

        Assert.False(context.Canvas.TestWireClick(a));
        Assert.True(context.Canvas.TestWireClick(b));

        Assert.Equal(before + 1, context.Document.Schematic.Wires.Count());
        Assert.True(context.Document.CanUndo);
        context.Document.Undo();
        Assert.Equal(before, context.Document.Schematic.Wires.Count());
    }

    [AvaloniaFact]
    public void CopyPasteDuplicatesSelection()
    {
        TestContext context = TestContext.Load();
        int before = context.Document.Schematic.Elements.Count();

        context.Canvas.TestClick(Center(context.FirstSymbol));
        string? xml = context.Canvas.CopySelectionXml();

        Assert.False(string.IsNullOrWhiteSpace(xml));
        Assert.True(context.Canvas.PasteSelectionXml(xml!));
        Assert.Equal(before + 1, context.Document.Schematic.Elements.Count());
        Assert.Single(context.Canvas.SelectedObjects);
        context.Document.Undo();
        Assert.Equal(before, context.Document.Schematic.Elements.Count());
    }

    [AvaloniaFact]
    public void DrawTransformMatchesSpeakerTerminals()
    {
        TestContext context = TestContext.Load();
        Symbol speaker = context.Document.Schematic.Symbols.Single(i => i.Component is Speaker);

        foreach (Terminal terminal in speaker.Terminals)
            Assert.Equal(speaker.MapTerminal(terminal), SchematicCanvas.TestTransformPoint(speaker, speaker.Component.LayoutSymbol().MapTerminal(terminal)));
    }

    [AvaloniaFact]
    public void DrawTransformMatchesGroundTerminal()
    {
        Symbol ground = new Symbol(new Ground()) { Position = new Coord(120, -40), Rotation = 1 };
        Terminal terminal = ground.Terminals.Single();

        Assert.Equal(ground.MapTerminal(terminal), SchematicCanvas.TestTransformPoint(ground, ground.Component.LayoutSymbol().MapTerminal(terminal)));
    }

    [AvaloniaFact]
    public void ResistorValueAndNameTextAreOutsideBody()
    {
        SymbolLayout layout = new Resistor().LayoutSymbol();
        SymbolLayout.Text name = layout.Texts.Single(i => i.String == "R1");
        SymbolLayout.Text value = layout.Texts.Single(i => i.String.Contains("Ω"));

        Assert.True(name.x.x >= 12);
        Assert.True(value.x.x <= -12);
    }

    private static Coord Center(Element element)
    {
        return (element.LowerBound + element.UpperBound) / 2;
    }

    private sealed class TestContext
    {
        private TestContext(SchematicDocument document, SchematicCanvas canvas)
        {
            Document = document;
            Canvas = canvas;
        }

        public SchematicDocument Document { get; }

        public SchematicCanvas Canvas { get; }

        public Symbol FirstSymbol => Document.Schematic.Symbols.First();

        public Symbol SecondSymbol => Document.Schematic.Symbols.Skip(1).First();

        public static TestContext Load()
        {
            SchematicDocument document = SchematicDocument.Open(FindFixture("Tests/Circuits/Passive 1stOrder Highpass RC.schx"));
            SchematicCanvas canvas = Dispatcher.UIThread.CheckAccess()
                ? new SchematicCanvas { Document = document }
                : Dispatcher.UIThread.Invoke(() => new SchematicCanvas { Document = document });
            return new TestContext(document, canvas);
        }

        private static string FindFixture(string relativePath)
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                    return candidate;
                directory = directory.Parent;
            }
            throw new FileNotFoundException("Could not locate test fixture.", relativePath);
        }
    }
}