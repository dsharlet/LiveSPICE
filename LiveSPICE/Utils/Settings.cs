using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;

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

        [UserScopedSetting]
        public string MainWindowLayout { get { return (string)this["MainWindowLayout"]; } set { this["MainWindowLayout"] = value; } }
        [UserScopedSetting]
        public string TransientSimulationLayout { get { return (string)this["TransientSimulationLayout"]; } set { this["TransientSimulationLayout"] = value; } }

        [UserScopedSetting]
        public string AudioDriver { get { return (string)this["AudioDriver"]; } set { this["AudioDriver"] = value; } }
        [UserScopedSetting]
        public string AudioDevice { get { return (string)this["AudioDevice"]; } set { this["AudioDevice"] = value; } }
        [UserScopedSetting]
        public string AudioInput { get { return (string)this["AudioInput"]; } set { this["AudioInput"] = value; } }
        [UserScopedSetting]
        public string[] AudioOutput 
        { 
            get 
            {
                try
                {
                    return (string[])this["AudioOutput"];
                }
                catch (Exception)
                {
                    return new string[0];
                }
            }
            set 
            { 
                this["AudioOutput"] = value; 
            } 
        }
        [UserScopedSetting]
        public double InputGain 
        { 
            get 
            { 
                try
                {
                    return (double)this["InputGain"]; 
                }
                catch (Exception)
                {
                    return 1.0;
                }
            } 
            set { this["InputGain"] = value; } 
        }
        [UserScopedSetting]
        public double OutputGain 
        { 
            get 
            {
                try
                {
                    return (double)this["OutputGain"];
                }
                catch (Exception)
                {
                    return 1.0;
                }
            } 
            set { this["OutputGain"] = value; } 
        }
    }
}
