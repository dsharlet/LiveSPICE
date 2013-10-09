using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// A component and the associated layout information to place the component on a schematic.
    /// </summary>
    public class Symbol : Element
    {
        private Component component;
        private SymbolLayout layout;
        public Component Component { get { return component; } }

        protected Coord position = new Coord(0, 0);
        public Coord Position
        { 
            get { return position; }
            set 
            {
                if (position == value) return;
                position = value;
                OnLayoutChanged();
            } 
        }

        protected int rotation = 0;
        public int Rotation 
        { 
            get { return rotation; } 
            set 
            {
                if (rotation == value) return;
                rotation = value;
                OnLayoutChanged();
            } 
        }
        
        protected bool flip = false;
        public bool Flip 
        { 
            get { return flip; } 
            set 
            {
                if (flip == value) return;
                flip = value;
                OnLayoutChanged();
            } 
        }

        public Coord Size { get { return layout.UpperBound - layout.LowerBound; } }

        public Symbol(Component Component) 
        { 
            component = Component;
            component.Tag = this;

            layout = new SymbolLayout();
            component.LayoutSymbol(layout);
        }

        // Map a local coordinate to a global coordinate.
        public Coord MapToGlobal(Coord Local)
        {
            int x = Local.x;
            int y = flip ? -Local.y : Local.y;

            int cos = Cos(rotation);
            int sin = Cos(rotation - 1);
            return new Coord(
                x * cos + y * sin + position.x,
                y * cos - x * sin + position.y);
        }
        
        // Map a global coordinate to the coordinates of this symbol.
        public Coord MapToLocal(Coord Global)
        {
            throw new NotImplementedException("MapToLocal");
        }

        // Element interface.
        public override IEnumerable<Terminal> Terminals { get { return component.Terminals; } }
        public override Coord MapTerminal(Terminal T) { return MapToGlobal(layout.MapTerminal(T)); }
        
        public override Coord LowerBound { get { return position + layout.LowerBound; } }
        public override Coord UpperBound { get { return position + layout.UpperBound; } }

        public override void Move(Coord dx)
        {
            position += dx;
            OnLayoutChanged();
        }

        public override void RotateAround(int dt, Point at)
        {
            Point size = Size;
            Point x = (Point)position + size / 2;
            x = RotateAround(x, dt, at);

            position = (Coord)Point.Round(x - size / 2);
            rotation += dt;
            OnLayoutChanged();
        }

        public override void FlipOver(double y)
        {
            Point size = Size;
            Point at = (Point)position + size / 2;
            at.y += 2 * (y - at.y);

            flip = !flip;
            position = (Coord)Point.Round(at - size / 2);
            OnLayoutChanged();
        }

        public override XElement Serialize()
        {
            XElement X = base.Serialize();

            Type T = component.GetType();
            X.SetAttributeValue("Type", T.AssemblyQualifiedName);
            X.SetAttributeValue("Rotation", Rotation);
            X.SetAttributeValue("Flip", Flip);
            X.SetAttributeValue("Position", Position);

            foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<SchematicPersistent>() != null))
            {
                System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                X.SetAttributeValue(i.Name, tc.ConvertToString(i.GetValue(component)));
            }
            return X;
        }
        protected override void OnDeserialize(XElement X)
        {
            Type T = Type.GetType(X.Attribute("Type").Value);
            rotation = int.Parse(X.Attribute("Rotation").Value);
            flip = bool.Parse(X.Attribute("Flip").Value);
            position = Coord.Parse(X.Attribute("Position").Value);
            
            component = (Component)Activator.CreateInstance(T);
            foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<SchematicPersistent>() != null))
            {
                XAttribute attr = X.Attribute(i.Name);
                if (attr != null)
                {
                    System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                    i.SetValue(component, tc.ConvertFromString(attr.Value));
                }
            }
        }
    }
}
