using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using Util;

namespace LiveSPICE
{
    public class Component : INotifyPropertyChanged
    {
        private string name;
        public string Name { get { return name; } }

        private string desc;
        public string Description { get { return desc; } }

        private Circuit.Component instance;
        public Circuit.Component Instance { get { return instance; } }
        public Circuit.SymbolLayout Layout { get { return instance.LayoutSymbol(); } }

        private bool visible = true;
        public bool IsVisible { get { return visible; } set { visible = value; NotifyChanged(nameof(IsVisible)); } }

        public Component(Circuit.Component Instance, string Name, string Description)
        {
            instance = Instance;
            name = Name;
            desc = Description;
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Category : INotifyPropertyChanged
    {
        private ObservableCollection<Category> children = new ObservableCollection<Category>();
        /// <summary>
        /// Categories this Category is a parent of.
        /// </summary>
        public ObservableCollection<Category> Children { get { return children; } }

        private ObservableCollection<Component> components = new ObservableCollection<Component>();
        /// <summary>
        /// Components contained in this Category.
        /// </summary>
        public ObservableCollection<Component> Components { get { return components; } }

        /// <summary>
        /// Remove all items from the Category.
        /// </summary>
        public void Clear() { children.Clear(); components.Clear(); }

        /// <summary>
        /// All the components in this Category and its Children.
        /// </summary>
        public IEnumerable<Component> Flatten { get { return Children.SelectMany(i => i.Flatten).Concat(Components); } }

        private string name;
        /// <summary>
        /// Name of this Category.
        /// </summary>
        public string Name { get { return name; } set { name = value; NotifyChanged(nameof(Name)); } }

        private bool expanded = false;
        /// <summary>
        /// Whether or not the Category is expanded.
        /// </summary>
        public bool IsExpanded { get { return expanded; } set { expanded = value; NotifyChanged(nameof(IsExpanded)); } }

        /// <summary>
        /// Find an immediate sub-category of the given Name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Category FindChild(string Name)
        {
            Category category = children.SingleOrDefault(i => i.Name == Name);
            if (category == null)
            {
                category = new Category() { Name = Name };
                children.Add(category);
            }

            return category;
        }

        private void LoadSpiceLibrary(string Library, string Name)
        {
            // Try to load the library as a SPICE model library.
            try
            {
                Circuit.Spice.Statements statements = new Circuit.Spice.Statements() { Log = Util.Log.Global };
                statements.Parse(Library);
                IEnumerable<Circuit.Spice.Model> models = statements.OfType<Circuit.Spice.Model>().Where(i => i.Component != null);
                if (models.Any())
                {
                    Category child = FindChild(Name);
                    foreach (Circuit.Spice.Model i in models)
                        child.AddComponent(i.Component, i.Component.PartNumber, i.Description);
                }
            }
            catch (Exception Ex)
            {
                Util.Log.Global.WriteLine(Util.MessageType.Warning, "Failed to load component libary '{0}': {1}", Library, Ex.Message);
            }
        }

        private void LoadLibrary(XDocument Doc, string Name)
        {
            XElement library = Doc.Element("Library");
            if (library != null)
            {
                XAttribute category = library.Attribute("Category");
                Category child = FindChild(category != null ? category.Value : Name);

                foreach (XElement i in library.Elements("Component"))
                {
                    try
                    {
                        Circuit.Component C = Circuit.Component.Deserialize(i);
                        child.AddComponent(C);
                    }
                    catch (Exception E)
                    {
                        Util.Log.Global.WriteLine(Util.MessageType.Warning, "Failed to load component: {0}", E.Message);
                    }
                }
            }
            else if (Doc.Element("Schematic") != null)
            {
                Circuit.Schematic S = Circuit.Schematic.Deserialize(Doc.Element("Schematic"));
                Circuit.Circuit C = S.Build();
                AddComponent(C, Name, C.Description);
            }
        }

        /// <summary>
        /// Add the categories and components of the specified library to this Category.
        /// </summary>
        /// <param name="Library"></param>
        public void LoadLibrary(string Library)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(Library);
            try
            {
                LoadLibrary(XDocument.Load(Library), name);
            }
            catch (System.Xml.XmlException)
            {
                LoadSpiceLibrary(Library, name);
            }
            catch (Exception Ex)
            {
                Util.Log.Global.WriteLine(Util.MessageType.Warning, "Failed to load component libary '{0}': {1}", Library, Ex.Message);
            }
        }

        /// <summary>
        /// Add all of the libraries recursively in the given path to this Category.
        /// </summary>
        /// <param name="Path"></param>
        public void LoadLibraries(string Path)
        {
            foreach (string i in System.IO.Directory.GetDirectories(Path))
                FindChild(System.IO.Path.GetFileName(i)).LoadLibraries(i);

            foreach (string i in System.IO.Directory.GetFiles(Path))
                LoadLibrary(i);
        }

        public void AddComponent(Circuit.Component C, string Name, string Description, KeyGesture[] keys = null)
        {
            if (keys != null)
            {
                string shortcuts = "(" + string.Join(", ", keys.Select(j => j.GetDisplayStringForCulture(CultureInfo.CurrentCulture))) + ")";
                if (Description != null)
                    Description += " " + shortcuts;
                else
                    Description = shortcuts;
            }

            Components.Add(new Component(C, Name, Description));
        }
        public void AddComponent(Circuit.Component C, KeyGesture[] keys = null)
        {
            string name = string.IsNullOrEmpty(C.PartNumber) ? C.TypeName : C.PartNumber;
            AddComponent(C, name, C.Description, keys);
        }
        public void AddComponent(Type T, KeyGesture[] keys = null)
        {
            try
            {
                AddComponent((Circuit.Component)Activator.CreateInstance(T), keys);
            }
            catch (Exception) { }
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
