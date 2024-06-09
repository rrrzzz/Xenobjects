using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class DisableAutoRefresh
    {
        private static FileSystemWatcher _watcher;
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
            _watcher = new FileSystemWatcher(Application.dataPath, "*.cs")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                IncludeSubdirectories = true
            };
        
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;
        
            _watcher.EnableRaisingEvents = true;
            EditorApplication.LockReloadAssemblies();
            _isInited = true;
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

