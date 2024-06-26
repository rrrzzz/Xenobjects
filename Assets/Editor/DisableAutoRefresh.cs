using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class DisableAutoRefresh
    {
        private static FileSystemWatcher _watcher;
        private static FileSystemWatcher _watcher2;
        private static FileSystemWatcher _watcher3;
        private static bool _needsRefresh;
        private static bool _wasRefreshCalled;
        private static bool _isInited;
        private static bool _isInPlayMode;
        
        static DisableAutoRefresh()
        {
            if (_isInited)
            {
                return;
            }
            EditorApplication.projectChanged += ProjectChanged;
            EditorApplication.playModeStateChanged += RecompileOnEnterEditMode;
            //Path.Combine(Application.dataPath, "Code")
            _watcher = CreateWatcher("*.cs");
            _watcher2 = CreateWatcher("*.shader");
            _watcher3 = CreateWatcher("*.asmdef");
            
            EditorApplication.LockReloadAssemblies();
            _isInited = true;
        }

        private static FileSystemWatcher CreateWatcher(string extensionToWatch)
        {
            var watcher = new FileSystemWatcher(Application.dataPath, extensionToWatch)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = true
            };
        
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnChanged;
        
            watcher.EnableRaisingEvents = true;

            return watcher;
        }
        
        private static void ProjectChanged()
        {
            _needsRefresh = true;
        }
        
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            _needsRefresh = true;
        }
        
        private static void RecompileOnEnterEditMode(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredPlayMode)
            {
                _isInPlayMode = true;
            }
            else if (obj == PlayModeStateChange.ExitingPlayMode)
            {
                _isInPlayMode = false;
            }

            if (obj != PlayModeStateChange.ExitingEditMode) return;
            Refresh();
        }
        
        [MenuItem("STOPCompile/Disable Auto Refresh")]
        private static void AutoRefreshToggle()
        {
            EditorPrefs.SetInt("kAutoRefresh", 0);
        }
        
        [MenuItem("STOPCompile/Enable Auto Refresh")]
        private static void AutoRefreshEnable()
        {
            EditorPrefs.SetInt("kAutoRefresh", 1);
        }
        
        [MenuItem("STOPCompile/Refresh %q")]
        private static void Refresh()
        {
            if (!_needsRefresh)
            {
                return;
            }
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
            if (_wasRefreshCalled)
            {
                return;
            }

            _wasRefreshCalled = true;
            Refresh();
        }
        
        [InitializeOnLoadMethod]
        private static void LockAssemblies()
        {
            EditorApplication.LockReloadAssemblies();
            _needsRefresh = false;
            _wasRefreshCalled = false;
        }
        
        [MenuItem("STOPCompile/Refresh and Play %r")]
        private static void RefreshAndPlay()
        {
            if (_isInPlayMode)
            {
                EditorApplication.ExitPlaymode();
                return;
            }
            EditorApplication.EnterPlaymode();
        }
    }
}

