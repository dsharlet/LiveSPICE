using System;
using System.Configuration;
using System.Drawing;

namespace LiveSPICE
{
    public class Settings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
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
    }
}
