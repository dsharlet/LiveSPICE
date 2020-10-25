using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Util;

namespace LiveSPICE
{
    public class Settings : ApplicationSettingsBase
    {
        [UserScopedSetting]
        public string[] Mru
        {
            get
            {
                string[] mru = (string[])this["Mru"];
                return mru != null ? mru : new string[0];
            }
            set
            {
                this["Mru"] = value;
            }
        }

        public void Used(string Filename)
        {
            List<string> mru = Mru.ToList();
            mru.Remove(Filename);
            mru.Insert(0, Filename);
            if (mru.Count > 20)
                mru.RemoveRange(20, mru.Count - 20);
            Mru = mru.ToArray();
        }
        public void RemoveFromMru(string Filename)
        {
            List<string> mru = Mru.ToList();
            mru.Remove(Filename);
            Mru = mru.ToArray();
        }

        private T This<T>(string Name, T Default)
        {
            object obj = this[Name];
            if (obj != null && obj is T t)
                return t;
            return Default;
        }

        [UserScopedSetting]
        public string MainWindowLayout { get { return This("MainWindowLayout", ""); } set { this["MainWindowLayout"] = value; } }
        [UserScopedSetting]
        public string LiveSimulationLayout { get { return This("LiveSimulationLayout", ""); } set { this["LiveSimulationLayout"] = value; } }

        [UserScopedSetting]
        public string AudioDriver { get { return This("AudioDriver", ""); } set { this["AudioDriver"] = value; } }
        [UserScopedSetting]
        public string AudioDevice { get { return This("AudioDevice", ""); } set { this["AudioDevice"] = value; } }
        [UserScopedSetting]
        public string[] AudioInputs { get { return This("AudioInputs", new string[0]); } set { this["AudioInputs"] = value; } }
        [UserScopedSetting]
        public string[] AudioOutputs { get { return This("AudioOutputs", new string[0]); } set { this["AudioOutputs"] = value; } }

        [UserScopedSetting]
        public MessageType LogVerbosity { get { return This("LogVerbosity", MessageType.Info); } set { this["LogVerbosity"] = value; } }
    }
}
