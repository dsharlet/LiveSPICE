using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Circuit;

namespace LiveSPICE.Avalonia;

public sealed class SchematicDocument
{
    public SchematicDocument(Schematic schematic, string? filePath = null)
    {
        Schematic = schematic;
        FilePath = filePath;
        UpdateSavedWriteTime();
    }

    public Schematic Schematic { get; }

    public string? FilePath { get; private set; }

    public bool Dirty { get; private set; }

    private DateTime savedWriteTimeUtc;

    private readonly Stack<IEditAction> undo = new Stack<IEditAction>();
    private readonly Stack<IEditAction> redo = new Stack<IEditAction>();

    public string Title
    {
        get
        {
            string title = FilePath == null ? "<Untitled>" : Path.GetFileNameWithoutExtension(FilePath);
            return Dirty ? title + " *" : title;
        }
    }

    public static SchematicDocument New()
    {
        return new SchematicDocument(new Schematic());
    }

    public static SchematicDocument Open(string path)
    {
        return new SchematicDocument(Schematic.Load(path), path);
    }

    public void MarkDirty()
    {
        Dirty = true;
    }

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public void Do(IEditAction action)
    {
        action.Do();
        Record(action);
    }

    public void Record(IEditAction action)
    {
        undo.Push(action);
        redo.Clear();
        MarkDirty();
    }

    public void Undo()
    {
        if (undo.Count == 0)
            return;

        IEditAction action = undo.Pop();
        action.Undo();
        redo.Push(action);
        MarkDirty();
    }

    public void Redo()
    {
        if (redo.Count == 0)
            return;

        IEditAction action = redo.Pop();
        action.Do();
        undo.Push(action);
        MarkDirty();
    }

    public void Save(string path)
    {
        Schematic.Save(path);
        FilePath = path;
        Dirty = false;
        UpdateSavedWriteTime();
    }

    public bool WasModifiedExternally()
    {
        if (FilePath == null || Dirty || !File.Exists(FilePath))
            return false;

        return File.GetLastWriteTimeUtc(FilePath) != savedWriteTimeUtc;
    }

    private void UpdateSavedWriteTime()
    {
        savedWriteTimeUtc = FilePath != null && File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : DateTime.MinValue;
    }
}

public interface IEditAction
{
    void Do();

    void Undo();
}

public sealed class AddElementsAction : IEditAction
{
    private readonly Schematic schematic;
    private readonly List<Element> elements;

    public AddElementsAction(Schematic schematic, IEnumerable<Element> elements)
    {
        this.schematic = schematic;
        this.elements = elements.ToList();
    }

    public IReadOnlyList<Element> Elements => elements;

    public void Do() => schematic.Add(elements);

    public void Undo() => schematic.Remove(elements);
}

public sealed class RemoveElementsAction : IEditAction
{
    private readonly Schematic schematic;
    private readonly List<Element> elements;

    public RemoveElementsAction(Schematic schematic, IEnumerable<Element> elements)
    {
        this.schematic = schematic;
        this.elements = elements.ToList();
    }

    public void Do() => schematic.Remove(elements);

    public void Undo() => schematic.Add(elements);
}

public sealed class MoveElementsAction : IEditAction
{
    private readonly List<Element> elements;
    private readonly Coord delta;

    public MoveElementsAction(IEnumerable<Element> elements, Coord delta)
    {
        this.elements = elements.ToList();
        this.delta = delta;
    }

    public void Do()
    {
        foreach (Element element in elements)
            element.Move(delta);
    }

    public void Undo()
    {
        foreach (Element element in elements)
            element.Move(-delta);
    }
}

public sealed class RotateElementsAction : IEditAction
{
    private readonly List<Element> elements;
    private readonly int delta;
    private readonly Point center;

    public RotateElementsAction(IEnumerable<Element> elements, int delta, Point center)
    {
        this.elements = elements.ToList();
        this.delta = delta;
        this.center = center;
    }

    public void Do()
    {
        foreach (Element element in elements)
            element.RotateAround(delta, center);
    }

    public void Undo()
    {
        foreach (Element element in elements)
            element.RotateAround(-delta, center);
    }
}

public sealed class FlipElementsAction : IEditAction
{
    private readonly List<Element> elements;
    private readonly double y;

    public FlipElementsAction(IEnumerable<Element> elements, double y)
    {
        this.elements = elements.ToList();
        this.y = y;
    }

    public void Do()
    {
        foreach (Element element in elements)
            element.FlipOver(y);
    }

    public void Undo() => Do();
}

public sealed class PropertyChangeAction : IEditAction
{
    private readonly object target;
    private readonly PropertyInfo property;
    private readonly object? before;
    private readonly object? after;

    public PropertyChangeAction(object target, PropertyInfo property, object? before, object? after)
    {
        this.target = target;
        this.property = property;
        this.before = before;
        this.after = after;
    }

    public void Do() => property.SetValue(target, after);

    public void Undo() => property.SetValue(target, before);
}

public sealed class PropertyChangeListAction : IEditAction
{
    private readonly List<object> targets;
    private readonly PropertyInfo property;
    private readonly List<object?> before;
    private readonly object? after;

    public PropertyChangeListAction(IEnumerable<object> targets, PropertyInfo property, IEnumerable<object?> before, object? after)
    {
        this.targets = targets.ToList();
        this.property = property;
        this.before = before.ToList();
        this.after = after;
    }

    public void Do()
    {
        foreach (object target in targets)
            property.SetValue(target, after);
    }

    public void Undo()
    {
        for (int i = 0; i < targets.Count; i++)
            property.SetValue(targets[i], before[i]);
    }
}