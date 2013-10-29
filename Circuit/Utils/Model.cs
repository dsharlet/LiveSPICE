using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    public abstract class Model
    {
        private string name;
        public string Name { get { return name; } }

        public Model(string Name) { name = Name; }

        public override string ToString() { return name; }

        private static Dictionary<Type, object> models = new Dictionary<Type, object>();
        public static List<T> GetModels<T>()
        {
            object list;
            if (!models.TryGetValue(typeof(T), out list))
            {
                list = new List<T>();
                models.Add(typeof(T), list);
            }
            return (List<T>)list;
        }
    }
}
