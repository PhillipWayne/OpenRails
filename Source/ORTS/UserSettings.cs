﻿// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.
//  
// USERSETTINGS
// 
// Provides a common storage mechanism for program configuration settings.
// 
// Settings are defined as public properties in the UserSettings class, ie 
//      public bool Alerter { get; set; }
//      public int BrakePipeChargingRate { get; set; }
//      etc   ( enclosed in #region 'User Settings' )
// When the class is constructed, each of these has a 
// default value set in InitUserSettings.
// LoadUserSettings uses source code reflection to scan the list
// of defined properties, checking for overrides
// first in the registry, then on the command line.
// The property is updated and Sources records why.
//
// All command line options start with - or /
//     other parameters are considered data.
// Command line overrides look like this eg
//      -FullScreen=true  -WindowPosition_Activity=5,5


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ORTS
{
    public class DoNotSaveAttribute : Attribute
    {
    }

    public class UserSettings
    {
        readonly string RegistryKey;
        readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>();
                // used in Log() to output the source of the user setting per the following enum.

        public enum Source
        {
            Default,
            CommandLine,
            Registry,
        }


        #region User Settings

        // Please put all user settings in here as auto-properties. Public properties
        // of type 'string', 'int' and 'bool' are automatically loaded/saved.

        // General settings.
        public bool Alerter { get; set; }
        public int BrakePipeChargingRate { get; set; }
        public bool DataLogger { get; set; }
        public bool DynamicShadows { get; set; }
        public bool FullScreen { get; set; }
        public bool GraduatedRelease { get; set; }
        public bool Logging { get; set; }
        public string LoggingFilename { get; set; }
        public string LoggingPath { get; set; }
        public bool MSTSBINSound { get; set; }
        public bool Precipitation { get; set; }
        public bool Profiling { get; set; }
        public int ProfilingFrameCount { get; set; }
        public string ScreenshotPath { get; set; }
        public int ShaderModel { get; set; }
        public bool ShadowAllShapes { get; set; }
        public bool ShadowMapBlur { get; set; }
        public int ShadowMapCount { get; set; }
        public int ShadowMapDistance { get; set; }
        public int ShadowMapResolution { get; set; }
        public bool ShowErrorDialogs { get; set; }
        public int SoundDetailLevel { get; set; }
        public bool SuppressConfirmations { get; set; }
        public bool TrainLights { get; set; }
        public bool UseAdvancedAdhesion { get; set; }
        public bool VerticalSync { get; set; }
        public int ViewingDistance { get; set; }
        public int ViewingFOV { get; set; }
        public bool WindowGlass { get; set; }
        public string WindowSize { get; set; }
        public bool Wire { get; set; }
        public int WorldObjectDensity { get; set; }
		public bool ViewDispatcher { get; set; }
		public int MPUpdateInterval { get; set; }

        // These two are command-line only flags to start multiplayer modes.
        [DoNotSave]
        public bool MultiplayerClient { get; set; }
        [DoNotSave]
        public bool MultiplayerServer { get; set; }

        // Multiplayer settings.
        public bool Multiplayer { get; set; }
        public string Multiplayer_User { get; set; }
        public string Multiplayer_Host { get; set; }
        public int Multiplayer_Port { get; set; }

        // Window position settings.
        public int[] WindowPosition_Activity { get; set; }
        public int[] WindowPosition_Compass { get; set; }
        public int[] WindowPosition_DriverAid { get; set; }
        public int[] WindowPosition_Help { get; set; }
        public int[] WindowPosition_NextStation { get; set; }
        public int[] WindowPosition_Quit { get; set; }
        public int[] WindowPosition_Switch { get; set; }
        public int[] WindowPosition_TrackMonitor { get; set; }
        public int[] WindowPosition_TrainOperations { get; set; }

        #endregion

        public UserSettings(string registryKey, IEnumerable<string> options)
        {
            RegistryKey = registryKey;
            InitUserSettings();
            LoadUserSettings(options);
        }

        void InitUserSettings()
        {
            // Initialize defaults for all user settings here.
            BrakePipeChargingRate = 21;
            Logging = true;
            LoggingPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            LoggingFilename = "OpenRailsLog.txt";
            ProfilingFrameCount = 1000;
            ScreenshotPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.ProductName);
            ShadowMapBlur = true;
            ShadowMapCount = 4;
            ShadowMapResolution = 1024;
            ShowErrorDialogs = true;
            SoundDetailLevel = 5;
            UseAdvancedAdhesion = true;
            ViewingDistance = 2000;
            ViewingFOV = 45; // MSTS uses 60 FOV horizontally, on 4:3 displays this is 45 FOV vertically (what OR uses).
            WindowSize = "1024x768";
            WorldObjectDensity = 10;

            Multiplayer_User = Environment.UserName;
            Multiplayer_Host = "127.0.0.1";
            Multiplayer_Port = 30000;
			MPUpdateInterval = 10;

            WindowPosition_Activity = new[] { 50, 50 };
            WindowPosition_Compass = new[] { 50, 0 };
            WindowPosition_DriverAid = new[] { 100, 100 };
            WindowPosition_Help = new[] { 50, 50 };
            WindowPosition_NextStation = new[] { 0, 100 };
            WindowPosition_Quit = new[] { 50, 50 };
            WindowPosition_Switch = new[] { 0, 50 };
            WindowPosition_TrackMonitor = new[] { 100, 0 };
            WindowPosition_TrainOperations = new[] { 50, 50 };

        }

        void LoadUserSettings(IEnumerable<string> options)      // options enumerates a list of option strings
        {                                                       // ie { "-FullScreen=true", "-WindowPosition_Activity=5,5" }

            // This special command-line option prevents the registry values from being used.
            var allowRegistryValues = !options.Contains("skip-user-settings", StringComparer.OrdinalIgnoreCase);
            
            var optionsDictionary = new Dictionary<string, string>();
            foreach (var option in options)
            {
                // Pull apart the command-line options so we can find them by setting name.
                var k = option.Split(new[] { '=', ':' }, 2)[0].ToLowerInvariant();
                var v = option.Contains('=') || option.Contains(':') ? option.Split(new[] { '=', ':' }, 2)[1].ToLowerInvariant() : "yes";
                optionsDictionary[k] = v;
            }
            // optionsDictionary contains eg { "fullscreen":"true",  "WindowPosition_Activity":"5,5" {

            using (var RK = Registry.CurrentUser.OpenSubKey(Program.RegistryKey))
            {
                // for each property in the UserSettings class ( ie BrakePipeChargingRate, Logging etc )
                foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
                {
                    // Get the default value.
                    var defValue = property.GetValue(this, null);
                    // Read in the registry option, if it exists.
                    var regValue = allowRegistryValues && RK != null ? RK.GetValue(property.Name, null) : null;
                    // Read in the command-line option, if it exists into optValue.
                    var propertyNameLower = property.Name.ToLowerInvariant();
                    var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;

                    // Map registry option for boolean types so 1 is true; everything else is false.
                    if ((regValue != null) && (regValue is int) && (property.PropertyType == typeof(bool)))
                        regValue = (int)regValue == 1;

                    // Map registry option for int[] types.
                    else if ((regValue != null) && (regValue is string) && (property.PropertyType == typeof(int[])))
                        regValue = ((string)regValue).Split(',').Select(s => int.Parse(s)).ToArray();

                    // Parse command-line option for boolean types so true/yes/on/1 are all true; everything else is false.
                    if ((optValue != null) && (property.PropertyType == typeof(bool)))
                        optValue = new[] { "true", "yes", "on", "1" }.Contains(optValue);

                    // Parse command-line option for int types.
                    else if ((optValue != null) && (property.PropertyType == typeof(int)))
                        optValue = int.Parse((string)optValue);

                    // Parse command-line option for int[] types.
                    else if ((optValue != null) && (property.PropertyType == typeof(int[])))
                        optValue = ((string)optValue).Split(',').Select(s => int.Parse(s.Trim())).ToArray();

                    // at this point:
                    //      optValue is a bool,int,or int[] representing the command line override 
                    //                    or null if no command line override
                    //      regValue is a bool,int,or int[] representing the registry entry or null
                    //                    or null if no registry override
                    
                    var value = optValue != null ? optValue : regValue != null ? regValue : defValue;
                    try
                    {
                        // int[] values must have the same number of items as default value.
                        if ((property.PropertyType == typeof(int[])) && (value != null) && ((int[])value).Length != ((int[])defValue).Length)
                            throw new ArgumentException();

                        property.SetValue(this, value, new object[0]);
                        Sources.Add(property.Name, value.Equals(defValue) ? Source.Default : optValue != null ? Source.CommandLine : regValue != null ? Source.Registry : Source.Default);
                    }
                    catch (ArgumentException)
                    {
                        Trace.TraceWarning("Unable to load {0} value from type {1}", property.Name, value.GetType().FullName);
                        value = defValue;
                        Sources.Add(property.Name, Source.Default);
                    }
                }
            }
        }

        public void Log()
        {
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
            {
                var value = property.GetValue(this, null);
                var source = Sources[property.Name];
                if (property.PropertyType == typeof(int[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
                else
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source == Source.CommandLine ? "(command-line)" : source == Source.Registry ? "(registry)" : "");
            }
        }

        /// <summary>
        /// Save UserSettings to the registry
        /// except those decorated with 'DoNoSaveAttribute'.
        /// </summary>
        public void Save()
        {
            Save(null);
        }

        /// <summary>
        /// Save UserSettings to the registry
        /// except those decorated with 'DoNoSaveAttribute'.
        /// Save the specified setting, or all if null specified.
        /// </summary>
        /// <remarks>
        /// Used, eg, by Popups.Window to save their location.
        /// </remarks>
        /// <param name="name"></param>
        public void Save(string name)
        {
            using (var RK = Registry.CurrentUser.CreateSubKey(Program.RegistryKey))
            {
                foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
                {
                    if ((name != null) && (property.Name != name))
                        continue;

                    if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length > 0)
                        continue;

                    var value = property.GetValue(this, null);

                    if (property.PropertyType == typeof(string))
                    {
                        RK.SetValue(property.Name, value, RegistryValueKind.String);
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        RK.SetValue(property.Name, value, RegistryValueKind.DWord);
                    }
                    else if (property.PropertyType == typeof(bool))
                    {
                        RK.SetValue(property.Name, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                    }
                    else if (property.PropertyType == typeof(int[]))
                    {
                        RK.SetValue(property.Name, String.Join(",", ((int[])value).Select(v => v.ToString()).ToArray()), RegistryValueKind.String);
                    }
                }
            }
        }
    }
}