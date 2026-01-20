using System;
using System.Text;
using com.tcs.tools.adminwindow.Core.Dispatch;
using com.tcs.tools.adminwindow.Core.Logging;
using com.tcs.tools.adminwindow.Core.Parsing;
using com.tcs.tools.adminwindow.Core.Persistence;
using com.tcs.tools.adminwindow.Core.Registry;
using UnityEditor;                     // keep: EditorWindow + AssetDatabase
// using UnityEditor.UIElements;      // ❌ remove for runtime controls
using UnityEngine;
using UnityEngine.UIElements;          // ✅ runtime UI Toolkit

namespace com.tcs.tools.adminwindow.Editor
{
    public class AdminWindow : EditorWindow
    {
        TextField _cmd;
        ListView _history;
        ScrollView _log;
        Toggle _dry;
        StringBuilder _logSb = new();
        int _historyIndex = -1;

        // Helper to resolve UI asset paths both in source repo and in installed package
        static class AdminUiPaths
        {
            const string RootAssets = "Assets/TCSTools/AdminWindow";
            const string RootPackage = "Packages/com.tcs.tools.adminwindow";
            public static string PathFor(string relative)
            {
                var a = System.IO.Path.Combine(RootAssets, relative).Replace('\\','/');
                if (System.IO.File.Exists(a)) return a;
                var p = System.IO.Path.Combine(RootPackage, relative).Replace('\\','/');
                return p;
            }
        }

        [MenuItem("TCS/Admin/Admin Window %#`")]
        public static void ShowWindow()
        {
            var w = GetWindow<AdminWindow>();
            w.titleContent = new GUIContent("Admin");
            w.minSize = new Vector2(700, 350);
            w.Show();
        }

        void CreateGUI()
        {
            var root = rootVisualElement;

            // stretch the root
            root.style.flexGrow = 1;

            var uxmlPath = AdminUiPaths.PathFor("Editor/UI/AdminWindow.uxml");
            var ussPath  = AdminUiPaths.PathFor("Editor/UI/AdminWindow.uss");
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var uss  = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (!uxml) Debug.LogError($"AdminWindow UXML not found at {uxmlPath}");
            if (!uss) Debug.LogWarning($"AdminWindow USS not found at {ussPath} – continuing without styles");
            if (uss)  root.styleSheets.Add(uss);
            if (uxml) uxml.CloneTree(root);

            _cmd     = root.Q<TextField>("cmdInput");
            _history = root.Q<ListView>("history");
            _log     = root.Q<ScrollView>("log");
            _dry     = root.Q<Toggle>("dryRun");

            // 🔁 Query runtime controls (match UXML)
            var runBtn        = root.Q<Button>("runButton");
            var refreshBtn    = root.Q<Button>("refresh");
            var clearLogBtn   = root.Q<Button>("clearLog");
            var clearHistoryBtn = root.Q<Button>("clearHistory");

            if (runBtn != null)        runBtn.clicked += () => Execute(_cmd?.value);
            if (refreshBtn != null)    refreshBtn.clicked += () => { AdminRegistry.Rebuild(); AppendLog(AdminLogLevel.Info, "Registry rebuilt."); };
            if (clearLogBtn != null)   clearLogBtn.clicked += () => { AdminLog.Clear(); _log?.Clear(); };
            if (clearHistoryBtn != null)
                clearHistoryBtn.clicked += () =>
                {
                    AdminHistoryStore.Clear();
                    _history?.Rebuild();
                    if (_history != null) _history.selectedIndex = -1;
                    _historyIndex = -1;
                };

            // history
            if (_history != null)
            {
                _history.makeItem = () => new Label();
                _history.bindItem = (e, i) => (e as Label).text = AdminHistoryStore.All[i];
                _history.itemsSource = (System.Collections.IList)AdminHistoryStore.All;
                _history.itemsChosen += objs =>
                {
                    foreach (var o in objs)
                    {
                        if (_cmd != null)
                        {
                            _cmd.value = o.ToString();
                            FocusCmd();
                        }
                        break;
                    }
                    _historyIndex = _history.selectedIndex;
                };

                // Ensure it stretches properly inside flex
                _history.style.flexShrink = 1;
                _history.style.flexGrow = 0;  // width is fixed by USS (#history { width:35% })
                _history.style.minHeight = 0;
            }

            if (_cmd != null)
            {
                _cmd.multiline = false;
                _cmd.isDelayed = false;

                _cmd.RegisterCallback<KeyDownEvent>(evt =>
                {
                    var count = AdminHistoryStore.All?.Count ?? 0;

                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        evt.StopImmediatePropagation();
                        Execute(_cmd.value);
                        _cmd.Focus();
                        _historyIndex = -1;
                        if (_history != null) _history.selectedIndex = -1;
                        return;
                    }

                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        if (count > 0)
                        {
                            if (_historyIndex == -1) _historyIndex = 0;
                            else { _historyIndex++; if (_historyIndex >= count) _historyIndex = 0; }

                            if (_history != null) _history.selectedIndex = _historyIndex;
                            _cmd.value = AdminHistoryStore.All[_historyIndex];
                            _cmd.Focus();
                            _cmd.cursorIndex = _cmd.value != null ? _cmd.value.Length : 0;
                            evt.StopImmediatePropagation();
                        }
                        return;
                    }

                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        if (count > 0)
                        {
                            if (_historyIndex == 0 || _historyIndex == -1)
                            {
                                _cmd.value = string.Empty;
                                _historyIndex = -1;
                                if (_history != null) _history.selectedIndex = -1;
                                _cmd.Focus();
                                evt.StopImmediatePropagation();
                                return;
                            }
                            else
                            {
                                _historyIndex--;
                                if (_historyIndex < 0) _historyIndex = 0;
                                if (_history != null) _history.selectedIndex = _historyIndex;
                                _cmd.value = AdminHistoryStore.All[_historyIndex];
                                _cmd.Focus();
                                _cmd.cursorIndex = _cmd.value != null ? _cmd.value.Length : 0;
                                evt.StopImmediatePropagation();
                            }
                        }
                        return;
                    }
                }, TrickleDown.TrickleDown);
            }

            // Make the right panel stretch
            if (_log != null)
            {
                _log.style.flexGrow = 1;
                _log.style.minHeight = 0;
            }

            FocusCmd();
        }

        void FocusCmd() { if (_cmd == null) return; _cmd.Focus(); _cmd.SelectAll(); }

        void Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            if (_dry != null && _dry.value)
            {
                AppendLog(AdminLogLevel.Info, $"[DRY] {input}");
                AdminHistoryStore.Push(input);
                _history?.Rebuild();
                if (_history != null) _history.selectedIndex = -1;
                if (_cmd != null) { _cmd.value = string.Empty; FocusCmd(); _historyIndex = -1; }
                return;
            }

            try
            {
                var inv = AdminParser.Parse(input);
                var result = AdminDispatcher.Execute(inv);
                AdminHistoryStore.Push(input);
                _history?.Rebuild();
                if (_history != null) _history.selectedIndex = -1;
                AppendLog(AdminLogLevel.Info, FormatResult(result));
                if (_cmd != null) { _cmd.value = string.Empty; FocusCmd(); _historyIndex = -1; }
            }
            catch (Exception ex)
            {
                // Log full details (including inner exceptions and stack) to help diagnose reflected invocation errors
                AppendLog(AdminLogLevel.Error, ex.ToString());
            }
        }

        string FormatResult(object result) => result == null ? "(ok)" : result.ToString();

        void AppendLog(AdminLogLevel level, string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {level}: {msg}";

            // Route errors to Unity Console instead of the Admin window UI
            if (level == AdminLogLevel.Error)
            {
                UnityEngine.Debug.LogError(line);
                return;
            }

            // Non-error logs go to the Admin window UI as before
            var label = new Label(line);
            if (_log != null)
            {
                _log.Add(label);
                _log.ScrollTo(label);
            }
            else
            {
                UnityEngine.Debug.Log(line);
            }
        }
    }
}
