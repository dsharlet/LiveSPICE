using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace LiveSPICE
{
    /// <summary>
    /// Represents an editor action.
    /// </summary>
    public abstract class Edit
    {
        /// <summary>
        /// Perform this action.
        /// </summary>
        public abstract void Do();
        /// <summary>
        /// Reverse this action.
        /// </summary>
        public abstract void Undo();

        /// <summary>
        /// A description of this action.
        /// </summary>
        public override string ToString() { return ""; }
    }

    /// <summary>
    /// Represents a group of editor actions that will be performed atomically.
    /// </summary>
    public class EditList : Edit
    {
        IEnumerable<Edit> edits;

        private EditList(IEnumerable<Edit> Edits) { edits = Edits; }

        public static Edit New(IEnumerable<Edit> Edits)
        {
            if (Edits.Count() > 1)
                return new EditList(Edits);
            else
                return Edits.First();
        }
        public static Edit New(params Edit[] Edits) { return New(Edits.AsEnumerable()); }

        public override void Do() { foreach (Edit i in edits) i.Do(); }
        public override void Undo() { foreach (Edit i in edits.Reverse()) i.Undo(); }

        public override string ToString() { return edits.FirstOrDefault(i => i.ToString() != "").ToString(); }
    }
    
    /// <summary>
    /// Edit to change a property.
    /// </summary>
    public class PropertyEdit : Edit
    {
        object target;
        PropertyInfo property;
        object undo;
        object value;

        public PropertyEdit(object Target, PropertyInfo Property, object Value)
        {
            target = Target;
            property = Property;
            value = Value;
            undo = property.GetValue(target);
        }

        public override void Do() { property.SetValue(target, value); }
        public override void Undo() { property.SetValue(target, undo); }

        public override string ToString() { return property.Name; }
    }

    /// <summary>
    /// Editor context for undo/redo.
    /// </summary>
    public class EditStack
    {
        private List<Edit> undo = new List<Edit>();
        private List<Edit> redo = new List<Edit>();
        private Stack<List<Edit>> tentative = new Stack<List<Edit>>();

        private List<EventHandler> dirtied = new List<EventHandler>();
        public event EventHandler Dirtied { add { dirtied.Add(value); } remove { dirtied.Remove(value); } }

        private bool dirty = false;
        public bool Dirty 
        { 
            get { return dirty; } 
            set 
            { 
                if (value && value != dirty)
                {
                    EventArgs args = new EventArgs();
                    foreach (EventHandler i in dirtied)
                        i(this, args);
                }
                dirty = value;
            } 
        }
        
        public void Do(params Edit[] Edits)
        {
            Edit edit = EditList.New(Edits);
            edit.Do();
            Dirty = true;

            if (tentative.Any())
            {
                tentative.Peek().Add(edit);
            }
            else
            {
                undo.Add(edit);
                redo.Clear();
            }
        }

        public void BeginEditGroup() { tentative.Push(new List<Edit>()); }
        public void EndEditGroup()
        {
            List<Edit> edits = tentative.Pop();
            if (edits.Any())
            {
                Edit edit = EditList.New(edits);
                if (tentative.Any())
                {
                    tentative.Peek().Add(edit);
                }
                else
                {
                    undo.Add(edit);
                    redo.Clear();
                }
            }
        }
        public void CancelEditGroup()
        {
            IEnumerable<Edit> edits = tentative.Pop();
            foreach (Edit i in edits.Reverse())
                i.Undo();
        }

        public void Undo()
        {
            if (undo.Any())
            {
                undo.Last().Undo();
                redo.Add(undo.Last());
                undo.Remove(undo.Last());
                Dirty = true;
            }
        }
        public void Redo()
        {
            if (redo.Any())
            {
                redo.Last().Do();
                undo.Add(redo.Last());
                redo.Remove(redo.Last());
                Dirty = true;
            }
        }

        public bool CanUndo() { return undo.Any(); }
        public bool CanRedo() { return redo.Any(); }

        public string UndoDescription { get { return undo.Any() ? undo.Last().ToString() : ""; } }
        public string RedoDescription { get { return redo.Any() ? redo.Last().ToString() : ""; } }
    }
}
