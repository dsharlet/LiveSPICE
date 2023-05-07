using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Circuit
{
    public abstract class Element
    {
        private List<EventHandler> layoutChanged = new List<EventHandler>();
        protected void OnLayoutChanged() { foreach (EventHandler i in layoutChanged) i(this, null); }

        /// <summary>
        /// Event for when the layout of this node changes.
        /// </summary>
        public event EventHandler LayoutChanged
        {
            add { layoutChanged.Add(value); }
            remove { layoutChanged.Remove(value); }
        }

        private object tag;
        /// <summary>
        /// User defined tag.
        /// </summary>
        public object Tag { get { return tag; } set { tag = value; } }

        /// <summary>
        /// Get the terminals associated with this element.
        /// </summary>
        public abstract IEnumerable<Terminal> Terminals { get; }

        /// <summary>
        /// Check if this element intersects the rectangle formed by x1, x2.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <returns></returns>
        public virtual bool Intersects(Coord x1, Coord x2)
        {
            Coord l = LowerBound;
            if (l.x > x2.x || l.y > x2.y) return false;

            Coord u = UpperBound;
            if (u.x < x1.x || u.y < x1.y) return false;
            return true;
        }

        /// <summary>
        /// Get the location of T in the schematic.
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public abstract Coord MapTerminal(Terminal T);

        /// <summary>
        /// Move this Element by the specified change in position.
        /// </summary>
        /// <param name="dx"></param>
        public abstract void Move(Coord dx);

        /// <summary>
        /// Rotate this Element around the specified point by the specified change in rotation.
        /// </summary>
        /// <param name="Delta"></param>
        /// <param name="Around"></param>
        public abstract void RotateAround(int dt, Point at);

        /// <summary>
        /// Flip this Element over the specified horizontal coordinate.
        /// </summary>
        /// <param name="y"></param>
        public abstract void FlipOver(double y);

        public abstract Coord LowerBound { get; }
        public abstract Coord UpperBound { get; }

        public virtual XElement Serialize()
        {
            XElement X = new XElement("Element");
            X.SetAttributeValue("Type", GetType().AssemblyQualifiedName);
            return X;
        }

        public static Element Deserialize(XElement X)
        {
            try
            {
                Type T = Type.GetType(X.Attribute("Type").Value);
                return (Element)T.GetMethod("Deserialize").Invoke(null, new object[] { X });
            }
            catch (System.Reflection.TargetInvocationException Ex)
            {
                throw Ex.InnerException;
            }
        }

        protected static Point RotateAround(Point x, int dt, Point at)
        {
            Point dx = x - at;

            double Sin = Math.Round(Math.Sin(dt * Math.PI / 2));
            double Cos = Math.Round(Math.Cos(dt * Math.PI / 2));

            Point X = new Point(Cos, Sin);
            Point Y = new Point(-Sin, Cos);

            return new Point(dx * X + at.x, dx * Y + at.y);
        }

        // pi = 2.
        protected static int Cos(int Theta)
        {
            switch (((Theta % 4) + 4) % 4)
            {
                case 0: return 1;
                case 1: return 0;
                case 2: return -1;
                case 3: return 0;
                default: throw new Exception("What the...");
            }
        }

        protected static int Sin(int Theta) { return Cos(Theta - 1); }
    }
}
