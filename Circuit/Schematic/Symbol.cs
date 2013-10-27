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

        // Map a local coordinate to a global coordinate.
        protected Coord MapToGlobal(Coord Local)
        {
            int x = Local.x;
            int y = flip ? Local.y : -Local.y;

            int cos = Cos(rotation);
            int sin = Sin(rotation);
            return new Coord(
                x * cos + y * sin + position.x,
                y * cos - x * sin + position.y);
        }

        public Symbol(Component Component) 
        { 
            component = Component;
            component.Tag = this;

            layout = new SymbolLayout();
            component.LayoutSymbol(layout);
        }

        // Element interface.
        public override IEnumerable<Terminal> Terminals { get { return component.Terminals; } }
        public override Coord MapTerminal(Terminal T) { return MapToGlobal(layout.MapTerminal(T)); }

        protected Coord[] Corners()
        {
            Coord lb = layout.LowerBound;
            Coord ub = layout.UpperBound;
            Coord[] x = 
            {
                MapToGlobal(new Coord(lb.x, lb.y)),
                MapToGlobal(new Coord(ub.x, lb.y)),
                MapToGlobal(new Coord(lb.x, ub.y)),
                MapToGlobal(new Coord(ub.x, ub.y)),
            };
            return x;
        }

        public override Coord LowerBound 
        { 
            get 
            {
                Coord[] corners = Corners();
                return new Coord(corners.Min(i => i.x), corners.Min(i => i.y)); 
            } 
        }
        public override Coord UpperBound 
        { 
            get 
            {
                Coord[] corners = Corners();
                return new Coord(corners.Max(i => i.x), corners.Max(i => i.y));
            } 
        }
        public Coord Size { get { return UpperBound - LowerBound; } }
        public int Width { get { return Size.x; } }
        public int Height { get { return Size.y; } }
        
        public override void Move(Coord dx)
        {
            position += dx;
            OnLayoutChanged();
        }

        public override void RotateAround(int dt, Point at)
        {
            position = (Coord)Point.Round(RotateAround(position, dt, at));
            rotation += dt;
            OnLayoutChanged();
        }

        public override void FlipOver(double y)
        {
            position.y += (int)Math.Round(2 * (y - position.y));
            flip = !flip;
            OnLayoutChanged();
        }

        public override XElement Serialize()
        {
            if (component is UnserializedComponent)
                return ((UnserializedComponent)component).Data;

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

        public new static Symbol Deserialize(XElement X)
        {
            Coord position = new Coord(0, 0);
            try
            {
                position = Coord.Parse(X.Attribute("Position").Value);
                
                string type = X.Attribute("Type").Value;
                Type T = Type.GetType(type);
                if (T == null)
                    throw new System.Exception("Type '" + type + "' not found.");

                Symbol S = new Symbol((Component)Activator.CreateInstance(T));
                S.Position = position;
                if (T == null) return S;

                S.Rotation = int.Parse(X.Attribute("Rotation").Value);
                S.Flip = bool.Parse(X.Attribute("Flip").Value);
                foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<SchematicPersistent>() != null))
                {
                    XAttribute attr = X.Attribute(i.Name);
                    if (attr != null)
                    {
                        System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                        i.SetValue(S.Component, tc.ConvertFromString(attr.Value));
                    }
                }
                return S;
            }
            catch (System.Exception Ex)
            {
                return new Symbol(new Error(X, Ex.Message)) { Position = position };
            }
        }

        public override string ToString()
        {
            return component.ToString() + " at " + Position.ToString();
        }
    }
}
