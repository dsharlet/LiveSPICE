using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Circuit;
using Util;

namespace LiveSPICE.Avalonia;

/// <summary>
/// A part from a component library: a named, pre-configured component such as a 2N3904, as opposed
/// to a bare BJT the user has to fill in by hand.
///
/// The source XML element is kept rather than the deserialized component, so every placement gets a
/// fresh instance - components are mutable and a shared prototype would let edits to one placed
/// part show up in the next one.
/// </summary>
internal sealed class LibraryPart
{
    private readonly XElement source;

    public LibraryPart(XElement Source, string Name, string Category, string Description)
    {
        source = Source;
        this.Name = Name;
        this.Category = Category;
        this.Description = Description;
    }

    public string Name { get; }
    public string Category { get; }
    public string Description { get; }

    public Circuit.Component Create()
    {
        return Circuit.Component.Deserialize(source);
    }
}

/// <summary>
/// Loads the component libraries shipped in the Components folder. This mirrors what the WPF app's
/// Library control does, minus the WPF: the XML format is a Library element of Component elements,
/// each deserializable by Circuit.Component.Deserialize.
/// </summary>
internal static class ComponentLibrary
{
    /// <summary>
    /// Find the Components folder. It sits beside the executable in a normal install, but when
    /// running from a build output it is easier to walk up to the repository and use the copy in
    /// the Circuit project, so a developer build finds parts without an install step.
    /// </summary>
    public static string? FindComponentsDirectory()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            foreach (string candidate in new[]
                     {
                         Path.Combine(directory.FullName, "Components"),
                         Path.Combine(directory.FullName, "Circuit", "Components"),
                     })
            {
                if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.xml").Any())
                    return candidate;
            }
            directory = directory.Parent;
        }
        return null;
    }

    public static IReadOnlyList<LibraryPart> Load()
    {
        string? path = FindComponentsDirectory();
        if (path == null)
        {
            Log.Global.WriteLine(MessageType.Warning, "No component library found; only built-in component types will be listed.");
            return Array.Empty<LibraryPart>();
        }
        return Load(path);
    }

    public static IReadOnlyList<LibraryPart> Load(string Path)
    {
        List<LibraryPart> parts = new List<LibraryPart>();
        foreach (string file in Directory.EnumerateFiles(Path, "*.xml").OrderBy(i => i))
        {
            // One bad library should not hide the others.
            try
            {
                LoadLibrary(file, parts);
            }
            catch (Exception Ex)
            {
                Log.Global.WriteLine(MessageType.Warning, "Failed to load component library '{0}': {1}",
                    System.IO.Path.GetFileName(file), Ex.Message);
            }
        }
        Log.Global.WriteLine(MessageType.Info, "Loaded {0} library parts from '{1}'.", parts.Count, Path);
        return parts;
    }

    private static void LoadLibrary(string File, List<LibraryPart> Parts)
    {
        XElement? library = XDocument.Load(File).Element("Library");
        if (library == null)
            return;

        // The category is declared on the library, falling back to the file name.
        string category = library.Attribute("Category")?.Value
            ?? System.IO.Path.GetFileNameWithoutExtension(File);

        foreach (XElement element in library.Elements("Component"))
        {
            // Deserialize once here to validate the entry and to read its name, then discard it -
            // Create() makes the instances that actually get placed.
            try
            {
                Circuit.Component component = Circuit.Component.Deserialize(element);
                string name = string.IsNullOrEmpty(component.PartNumber) ? component.TypeName : component.PartNumber;
                string description = string.IsNullOrEmpty(component.Description)
                    ? $"{component.TypeName} ({category})"
                    : component.Description;
                Parts.Add(new LibraryPart(element, name, category, description));
            }
            catch (Exception Ex)
            {
                Log.Global.WriteLine(MessageType.Warning, "Failed to load component from '{0}': {1}",
                    System.IO.Path.GetFileName(File), Ex.Message);
            }
        }
    }
}
