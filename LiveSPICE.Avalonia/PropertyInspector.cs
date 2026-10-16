using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using Circuit;
using Util;

namespace LiveSPICE.Avalonia;

public sealed class PropertyInspector : ScrollViewer
{
    private readonly StackPanel panel = new StackPanel { Spacing = 6, Margin = new global::Avalonia.Thickness(8) };
    private IReadOnlyList<object> selectedObjects = Array.Empty<object>();

    public PropertyInspector()
    {
        Content = panel;
    }

    public event Action<IEditAction>? PropertyChangedByUser;

    public void SetSelectedObject(object? value)
    {
        selectedObjects = value == null ? Array.Empty<object>() : new[] { value };
        Rebuild();
    }

    public void SetSelectedObjects(IReadOnlyList<object> values)
    {
        selectedObjects = values;
        Rebuild();
    }

    private void Rebuild()
    {
        panel.Children.Clear();

        if (selectedObjects.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = "No selection" });
            return;
        }

        object first = selectedObjects[0];
        panel.Children.Add(new TextBlock { Text = selectedObjects.Count == 1 ? first.ToString() : selectedObjects.Count + " objects", FontWeight = global::Avalonia.Media.FontWeight.Bold });

        if (selectedObjects.Any(i => i.GetType() != first.GetType()))
        {
            panel.Children.Add(new TextBlock { Text = "Mixed selection" });
            return;
        }

        foreach (PropertyInfo property in EditableProperties(first))
            AddEditor(selectedObjects, property);
    }

    private static IEnumerable<PropertyInfo> EditableProperties(object instance)
    {
        return instance.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(i => i.CanRead && i.CanWrite)
            .Where(i => i.CustomAttribute<BrowsableAttribute>()?.Browsable != false)
            .Where(i => i.CustomAttribute<Serialize>() != null || IsSimple(i.PropertyType));
    }

    private static bool IsSimple(Type type)
    {
        return type == typeof(string) || type == typeof(int) || type == typeof(double) || type == typeof(decimal) || type == typeof(bool) || type.IsEnum;
    }

    private void AddEditor(IReadOnlyList<object> targets, PropertyInfo property)
    {
        TextBlock label = new TextBlock
        {
            Text = property.Name,
            VerticalAlignment = VerticalAlignment.Center
        };

        Control editor;
        if (property.PropertyType == typeof(bool))
        {
            CheckBox checkBox = new CheckBox { IsChecked = SharedValue(targets, property) as bool? };
            checkBox.IsCheckedChanged += (_, _) => SetValue(targets, property, checkBox.IsChecked == true);
            editor = checkBox;
        }
        else
        {
            TextBox textBox = new TextBox { Text = ConvertToString(property, SharedValue(targets, property)) };
            textBox.LostFocus += (_, _) => SetValueFromString(targets, property, textBox.Text ?? "");
            textBox.KeyDown += (_, e) =>
            {
                if (e.Key == global::Avalonia.Input.Key.Enter)
                {
                    SetValueFromString(targets, property, textBox.Text ?? "");
                    e.Handled = true;
                }
            };
            editor = textBox;
        }

        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("105,*") };
        row.Children.Add(label);
        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);
        panel.Children.Add(row);
    }

    private static string ConvertToString(PropertyInfo property, object? value)
    {
        TypeConverter converter = TypeDescriptor.GetConverter(property.PropertyType);
        return converter.ConvertToString(null, CultureInfo.InvariantCulture, value) ?? string.Empty;
    }

    private static object? SharedValue(IReadOnlyList<object> targets, PropertyInfo property)
    {
        object? first = property.GetValue(targets[0]);
        return targets.All(i => Equals(property.GetValue(i), first)) ? first : null;
    }

    private void SetValueFromString(IReadOnlyList<object> targets, PropertyInfo property, string text)
    {
        try
        {
            TypeConverter converter = TypeDescriptor.GetConverter(property.PropertyType);
            SetValue(targets, property, converter.ConvertFromString(null, CultureInfo.InvariantCulture, text));
        }
        catch
        {
            Rebuild();
        }
    }

    private void SetValue(IReadOnlyList<object> targets, PropertyInfo property, object? value)
    {
        List<object?> before = targets.Select(i => property.GetValue(i)).ToList();
        if (before.All(i => Equals(i, value)))
            return;

        PropertyChangedByUser?.Invoke(new PropertyChangeListAction(targets, property, before, value));
        Rebuild();
    }
}