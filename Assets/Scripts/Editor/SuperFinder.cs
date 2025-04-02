using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Reflection;

public class UniversalFuzzyFinder : EditorWindow
{
    private string searchString = "";
    private Vector2 scrollPosition;
    private List<string> folderPaths = new List<string>();
    private List<string> filePaths = new List<string>();
    private List<GameObject> sceneObjects = new List<GameObject>();
    private List<CreateMenuItem> createItems = new List<CreateMenuItem>();
    private List<(string path, float score)> filteredPaths = new List<(string, float)>();
    private List<(GameObject obj, float score)> filteredObjects = new List<(GameObject, float)>();
    private List<(CreateMenuItem item, float score)> filteredCreateItems = new List<(CreateMenuItem, float)>();
    private List<(GameObject obj, Component component, float score)> filteredComponents = new List<(GameObject, Component, float)>();
    private int selectedIndex = 0;
    private SearchMode currentMode = SearchMode.Folders;
    private const string RECENT_ACTIONS_KEY = "UniversalFuzzyFinder_RecentActions";
    private const int MAX_RECENT_ACTIONS = 10;
    private List<RecentAction> recentActions = new List<RecentAction>();

    
    [Serializable]
    private class RecentAction
    {
        public string DisplayName;
        public string Path;
        public SearchMode Mode;
        public DateTime LastUsed;
        public string AdditionalData;

        public RecentAction(string displayName, string path, SearchMode mode, string additionalData = "")
        {
            DisplayName = displayName;
            Path = path;
            Mode = mode;
            LastUsed = DateTime.Now;
            AdditionalData = additionalData;
        }
    }
    
    [Serializable]
    private class RecentActionList
    {
        public List<RecentAction> actions = new List<RecentAction>();
    }

    private enum SearchMode
    {
        Files,
        Folders,
        GameObjects,
        Create,
        Components
    }

    [MenuItem("Window/Universal Fuzzy Finder %p")] // Ctrl+P shortcut
    public static void ShowWindow()
    {
        var window = GetWindow<UniversalFuzzyFinder>("Universal Finder");
        window.minSize = new Vector2(600, 500);
        window.Focus();
        window.LoadRecentActions();
        window.RefreshLists();
    }

    private void AddRecentAction(string displayName, string path, SearchMode mode, string additionalData = "")
    {
        // Remove existing entry if present
        recentActions.RemoveAll(x => x.Path == path && x.Mode == mode);
        
        // Add new entry
        recentActions.Insert(0, new RecentAction(displayName, path, mode, additionalData));
        
        // Trim list to maximum size
        if (recentActions.Count > MAX_RECENT_ACTIONS)
            recentActions.RemoveRange(MAX_RECENT_ACTIONS, recentActions.Count - MAX_RECENT_ACTIONS);
        
        SaveRecentActions();
    }

    private void LoadRecentActions()
    {
        string json = EditorPrefs.GetString(RECENT_ACTIONS_KEY, "[]");
        try
        {
            recentActions = JsonUtility.FromJson<RecentActionList>(json).actions;
        }
        catch
        {
            recentActions = new List<RecentAction>();
        }
    }
    
    private void SaveRecentActions()
    {
        var actionList = new RecentActionList { actions = recentActions };
        string json = JsonUtility.ToJson(actionList);
        EditorPrefs.SetString(RECENT_ACTIONS_KEY, json);
    }


    private void RefreshLists()
    {
        folderPaths = GetAllFolderPaths();
        filePaths = GetAllFilePaths();
        sceneObjects = GetAllSceneObjects();
        createItems = GetAllCreateMenuItems();
        UpdateFilteredItems();
    }

    private List<CreateMenuItem> GetAllCreateMenuItems()
    {
        var items = new List<CreateMenuItem>();

        var menuPaths = Unsupported.GetSubmenus("Assets/Create");
        foreach (var path in menuPaths)
        {
            var displayName = GetDisplayName(path);
            items.Add(new CreateMenuItem
            {
                DisplayName = displayName,
                FullPath = path
            });
        }

        foreach (var type in TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>())
        {
            var attr = type.GetCustomAttribute<CreateAssetMenuAttribute>();
            items.Add(new CreateMenuItem
            {
                DisplayName = attr.menuName,
                Type = type
            });
        }

        return items.Distinct().OrderBy(i => i.DisplayName).ToList();
    }

    private string GetDisplayName(string menuPath)
    {
        var parts = menuPath.Split('/');
        return parts.Last().Replace("&", "");
    }

    private List<string> GetAllFolderPaths()
    {
        string assetsPath = Application.dataPath;
        return Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories)
            .Select(p => "Assets" + p.Substring(assetsPath.Length))
            .ToList();
    }

    private List<string> GetAllFilePaths()
    {
        string assetsPath = Application.dataPath;
        return Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith(".meta"))
            .Select(p => "Assets" + p.Substring(assetsPath.Length))
            .ToList();
    }

    private List<GameObject> GetAllSceneObjects()
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => !EditorUtility.IsPersistent(go))
            .ToList();
    }

    private void OnGUI()
    {
        HandleKeyboardInput();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.SetNextControlName("SearchField");
        string newSearchString = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField);
        EditorGUILayout.EndVertical();

        if (newSearchString != searchString)
        {
            searchString = newSearchString;
            DetermineSearchMode();
            UpdateFilteredItems();
            selectedIndex = 0;
        }

        if (focusSearchField)
        {
            EditorGUI.FocusTextInControl("SearchField");
            focusSearchField = false;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawResults();
        EditorGUILayout.EndScrollView();
    }

    private void DetermineSearchMode()
    {
        currentMode = searchString switch
        {
            // !@#$
            var s when s.StartsWith("!") => SearchMode.Files,
            var s when s.StartsWith("@") => SearchMode.Folders,
            var s when s.StartsWith("#") => SearchMode.Create,
            var s when s.StartsWith("$") => SearchMode.Components,
            _ => SearchMode.GameObjects
        };
    }

    private void DrawResults()
    {
        float itemHeight = EditorGUIUtility.singleLineHeight * 2 + 4;

        if (string.IsNullOrEmpty(searchString))
        {
            DrawRecentActions(itemHeight);
            return;
        }

        // Original drawing code...
        switch (currentMode)
        {
            case SearchMode.GameObjects:
                for (int i = 0; i < filteredObjects.Count; i++)
                    DrawGameObjectItem(i, itemHeight);
                break;
            case SearchMode.Components:
                for (int i = 0; i < filteredComponents.Count; i++)
                    DrawComponentItem(i, itemHeight);
                break;
            case SearchMode.Create:
                for (int i = 0; i < filteredCreateItems.Count; i++)
                    DrawCreateItem(i, itemHeight);
                break;
            default:
                for (int i = 0; i < filteredPaths.Count; i++)
                    DrawPathItem(i, itemHeight);
                break;
        }
    }
    
    private void DrawRecentActions(float itemHeight)
    {
        EditorGUILayout.LabelField("Recent Actions", EditorStyles.boldLabel);
        
        for (int i = 0; i < recentActions.Count; i++)
        {
            var action = recentActions[i];
            
            Rect itemRect = EditorGUILayout.GetControlRect(false, itemHeight);
            if (i == selectedIndex)
            {
                EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.4f, 0.8f, 0.2f));
            }

            var nameStyle = new GUIStyle(EditorStyles.boldLabel);
            var pathStyle = new GUIStyle(EditorStyles.miniLabel);
            pathStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            Rect nameRect = new Rect(itemRect.x + 4, itemRect.y + 2, itemRect.width - 8, EditorGUIUtility.singleLineHeight);
            Rect pathRect = new Rect(itemRect.x + 4, nameRect.yMax, itemRect.width - 8, EditorGUIUtility.singleLineHeight);

            string typeIndicator = GetTypeIndicator(action.Mode);
            EditorGUI.LabelField(nameRect, $"{typeIndicator} {action.DisplayName}", nameStyle);
            EditorGUI.LabelField(pathRect, action.Path, pathStyle);

            HandleItemInteraction(itemRect, i, () => SelectRecentAction(action));
        }
    }
    
    private string GetTypeIndicator(SearchMode mode)
    {
        return mode switch
        {
            SearchMode.Files => "📄",
            SearchMode.Folders => "📁",
            SearchMode.GameObjects => "🎮",
            SearchMode.Components => "⚙️",
            SearchMode.Create => "➕",
            _ => "•"
        };
    }
    
    private void SelectRecentAction(RecentAction action)
    {
        switch (action.Mode)
        {
            case SearchMode.GameObjects:
                var obj = GameObject.Find(action.Path);
                if (obj != null) SelectGameObject(obj);
                break;
            case SearchMode.Components:
                var gameObj = GameObject.Find(action.Path);
                if (gameObj != null)
                {
                    var component = gameObj.GetComponent(Type.GetType(action.AdditionalData));
                    if (component != null) SelectGameObjectWithComponent(gameObj, component);
                }
                break;
            case SearchMode.Create:
                var createItem = createItems.FirstOrDefault(i => i.DisplayName == action.DisplayName);
                if (createItem != null) CreateItem(createItem);
                break;
            default:
                SelectPath(action.Path);
                break;
        }
    }

    private void DrawComponentItem(int index, float itemHeight)
    {
        var (gameObj, component, _) = filteredComponents[index];
        string hierarchy = GetGameObjectPath(gameObj);
        string componentName = component.GetType().Name;

        Rect itemRect = EditorGUILayout.GetControlRect(false, itemHeight);
        if (index == selectedIndex)
        {
            EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.4f, 0.8f, 0.2f));
        }

        var nameStyle = new GUIStyle(EditorStyles.boldLabel);
        var pathStyle = new GUIStyle(EditorStyles.miniLabel);
        pathStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        Rect nameRect = new Rect(itemRect.x + 4, itemRect.y + 2, itemRect.width - 8, EditorGUIUtility.singleLineHeight);
        Rect pathRect = new Rect(itemRect.x + 4, nameRect.yMax, itemRect.width - 8, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(nameRect, $"{gameObj.name} ({componentName})", nameStyle);
        EditorGUI.LabelField(pathRect, hierarchy, pathStyle);

        HandleItemInteraction(itemRect, index, () => SelectGameObjectWithComponent(gameObj, component));
    }

    private void DrawCreateItem(int index, float itemHeight)
    {
        var item = filteredCreateItems[index].item;
        
        Rect itemRect = EditorGUILayout.GetControlRect(false, itemHeight);
        if (index == selectedIndex)
        {
            EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.4f, 0.8f, 0.2f));
        }

        var nameStyle = new GUIStyle(EditorStyles.boldLabel);
        var pathStyle = new GUIStyle(EditorStyles.miniLabel);
        pathStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        Rect nameRect = new Rect(itemRect.x + 4, itemRect.y + 2, itemRect.width - 8, EditorGUIUtility.singleLineHeight);
        Rect pathRect = new Rect(itemRect.x + 4, nameRect.yMax, itemRect.width - 8, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(nameRect, item.DisplayName, nameStyle);
        EditorGUI.LabelField(pathRect, item.FullPath ?? $"ScriptableObject: {item.Type?.Name}", pathStyle);

        HandleItemInteraction(itemRect, index, () => CreateItem(item));
    }

    private void DrawGameObjectItem(int index, float itemHeight)
    {
        var gameObj = filteredObjects[index].obj;
        string hierarchy = GetGameObjectPath(gameObj);

        Rect itemRect = EditorGUILayout.GetControlRect(false, itemHeight);
        if (index == selectedIndex)
        {
            EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.4f, 0.8f, 0.2f));
        }

        var nameStyle = new GUIStyle(EditorStyles.boldLabel);
        var pathStyle = new GUIStyle(EditorStyles.miniLabel);
        pathStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        Rect nameRect = new Rect(itemRect.x + 4, itemRect.y + 2, itemRect.width - 8, EditorGUIUtility.singleLineHeight);
        Rect pathRect = new Rect(itemRect.x + 4, nameRect.yMax, itemRect.width - 8, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(nameRect, gameObj.name, nameStyle);
        EditorGUI.LabelField(pathRect, hierarchy, pathStyle);

        HandleItemInteraction(itemRect, index, () => SelectGameObject(gameObj));
    }

    private void DrawPathItem(int index, float itemHeight)
    {
        var path = filteredPaths[index].path;
        string fileName = Path.GetFileName(path);
        string directory = Path.GetDirectoryName(path);

        Rect itemRect = EditorGUILayout.GetControlRect(false, itemHeight);
        if (index == selectedIndex)
        {
            EditorGUI.DrawRect(itemRect, new Color(0.2f, 0.4f, 0.8f, 0.2f));
        }

        var nameStyle = new GUIStyle(EditorStyles.boldLabel);
        var pathStyle = new GUIStyle(EditorStyles.miniLabel);
        pathStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        Rect nameRect = new Rect(itemRect.x + 4, itemRect.y + 2, itemRect.width - 8, EditorGUIUtility.singleLineHeight);
        Rect pathRect = new Rect(itemRect.x + 4, nameRect.yMax, itemRect.width - 8, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(nameRect, fileName, nameStyle);
        EditorGUI.LabelField(pathRect, directory, pathStyle);

        HandleItemInteraction(itemRect, index, () => SelectPath(path));
    }

    private void HandleKeyboardInput()
    {
        var currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown)
        {
            switch (currentEvent.keyCode)
            {
                case KeyCode.DownArrow:
                    selectedIndex = Mathf.Min(selectedIndex + 1, GetCurrentResultCount() - 1);
                    currentEvent.Use();
                    Repaint();
                    break;
                case KeyCode.UpArrow:
                    selectedIndex = Mathf.Max(selectedIndex - 1, 0);
                    currentEvent.Use();
                    Repaint();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SelectCurrent();
                    currentEvent.Use();
                    Close();
                    break;
                case KeyCode.Escape:
                    Close();
                    currentEvent.Use();
                    break;
            }
        }
    }

    private int GetCurrentResultCount()
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return recentActions.Count;
        }

        return currentMode switch
        {
            SearchMode.GameObjects => filteredObjects.Count,
            SearchMode.Components => filteredComponents.Count,
            SearchMode.Create => filteredCreateItems.Count,
            _ => filteredPaths.Count
        };
    }

    private void SelectCurrent()
    {
        if (selectedIndex < 0) return;

        if (string.IsNullOrEmpty(searchString) && selectedIndex < recentActions.Count)
        {
            SelectRecentAction(recentActions[selectedIndex]);
            return;
        }

        switch (currentMode)
        {
            case SearchMode.GameObjects when selectedIndex < filteredObjects.Count:
                SelectGameObject(filteredObjects[selectedIndex].obj);
                FocusHierarchyWindow();
                break;
            case SearchMode.Components when selectedIndex < filteredComponents.Count:
                var (obj, component, _) = filteredComponents[selectedIndex];
                SelectGameObjectWithComponent(obj, component);
                FocusHierarchyWindow();
                break;
            case SearchMode.Create when selectedIndex < filteredCreateItems.Count:
                CreateItem(filteredCreateItems[selectedIndex].item);
                FocusProjectWindow();
                break;
            case SearchMode.Files or SearchMode.Folders when selectedIndex < filteredPaths.Count:
                SelectPath(filteredPaths[selectedIndex].path);
                FocusProjectWindow();
                break;
        }
    }

    private bool focusSearchField = true;
    private void OnEnable()
    {
        focusSearchField = true;
    }

    private void UpdateFilteredItems()
    {
        string searchTerm = GetSearchTerm();

        filteredPaths.Clear();
        filteredObjects.Clear();
        filteredCreateItems.Clear();
        filteredComponents.Clear();

        switch (currentMode)
        {
            case SearchMode.Files:
                UpdateFilteredPaths(filePaths, searchTerm);
                break;
            case SearchMode.Folders:
                UpdateFilteredPaths(folderPaths, searchTerm);
                break;
            case SearchMode.GameObjects:
                UpdateFilteredObjects(searchTerm);
                break;
            case SearchMode.Components:
                UpdateFilteredComponents(searchTerm);
                break;
            case SearchMode.Create:
                UpdateFilteredCreateItems(searchTerm);
                break;
        }
    }

    private void UpdateFilteredComponents(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            foreach (var obj in sceneObjects)
            {
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component != null)
                    {
                        filteredComponents.Add((obj, component, 1f));
                    }
                }
            }
            filteredComponents = filteredComponents.Take(100).ToList();
            return;
        }

        var searchTerms = searchTerm.ToLower().Split(' ');

        foreach (var obj in sceneObjects)
        {
            var components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;
                
                string componentName = component.GetType().Name;
                float totalScore = CalculateMatchScore(componentName.ToLower(), searchTerms);

                if (totalScore > 0)
                {
                    filteredComponents.Add((obj, component, totalScore));
                }
            }
        }

        filteredComponents = filteredComponents
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.obj.name.Length)
            .Take(100)
            .ToList();
    }

    private void UpdateFilteredCreateItems(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            filteredCreateItems = createItems.Select(item => (item, 1f)).Take(100).ToList();
            return;
        }

        var searchTerms = searchTerm.ToLower().Split(' ');

        foreach (var item in createItems)
        {
            float totalScore = CalculateMatchScore(item.DisplayName.ToLower(), searchTerms);
            if (totalScore > 0)
            {
                filteredCreateItems.Add((item, totalScore));
            }
        }
    }

    private string GetSearchTerm()
    {
        return currentMode switch
        {
            SearchMode.Files => searchString.TrimStart('!'),
            SearchMode.Folders => searchString.TrimStart('@'),
            SearchMode.Create => searchString.TrimStart('#'),
            SearchMode.Components => searchString.TrimStart('$'),
            _ => searchString
        };
    }

    private void UpdateFilteredPaths(List<string> paths, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            filteredPaths = paths.Select(p => (p, 1f)).Take(100).ToList();
            return;
        }

        var searchTerms = searchTerm.ToLower().Split(' ');

        foreach (var path in paths)
        {
            string finalName = Path.GetFileName(path);
            float score = CalculateMatchScore(finalName.ToLower(), searchTerms);

            if (score > 0)
                filteredPaths.Add((path, score));
        }

        filteredPaths = filteredPaths
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.path.Length)
            .Take(100)
            .ToList();
    }

    private void UpdateFilteredObjects(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            filteredObjects = sceneObjects.Select(obj => (obj, 1f)).Take(100).ToList();
            return;
        }

        var searchTerms = searchTerm.ToLower().Split(' ');

        foreach (var obj in sceneObjects)
        {
            float totalScore = CalculateMatchScore(obj.name.ToLower(), searchTerms);

            if (totalScore > 0)
                filteredObjects.Add((obj, totalScore));
        }

        filteredObjects = filteredObjects
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.obj.name.Length)
            .Take(100)
            .ToList();
    }

    private float CalculateMatchScore(string text, string[] searchTerms)
    {
        float totalScore = 0;

        foreach (var term in searchTerms)
        {
            float termScore = FuzzyMatch(text, term);
            if (termScore == 0)
            {
                totalScore = 0;
                break;
            }
            totalScore += termScore;
        }

        if (totalScore > 0)
        {
            if (text.Contains(searchString.ToLower()))
                totalScore += 10;
            if (text.StartsWith(searchString.ToLower()))
                totalScore += 20;
        }

        return totalScore;
    }

    private float FuzzyMatch(string str, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return 0;
        
        str = str.ToLower();
        pattern = pattern.ToLower();
        
        if (str == pattern) return 1000;
        
        if (str.StartsWith(pattern)) return 500;
        
        if (str.Contains(pattern)) return 100;
        
        int score = 0;
        int strIndex = 0;
        int patternIndex = 0;
        
        while (strIndex < str.Length && patternIndex < pattern.Length)
        {
            if (str[strIndex] == pattern[patternIndex])
            {
                score += 1;
                patternIndex++;
            }
            strIndex++;
        }
        
        return patternIndex == pattern.Length ? score : 0;
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    private void HandleItemInteraction(Rect itemRect, int index, System.Action selectAction)
    {
        if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
        {
            selectedIndex = index;
            if (Event.current.clickCount == 2)
            {
                selectAction();
                Close();
            }
            Repaint();
        }
    }

    private void SelectPath(string path)
    {
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (obj != null)
        {
            AddRecentAction(Path.GetFileName(path), path, 
                Path.HasExtension(path) ? SearchMode.Files : SearchMode.Folders);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
            FocusProjectWindow();
        }
    }


    private void FocusProjectWindow()
    {
        Type projectBrowserType = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
        if (projectBrowserType != null)
        {
            var window = EditorWindow.GetWindow(projectBrowserType);
            window.Focus();
            
            // Force the project window to grab keyboard focus
            var windowObject = new SerializedObject(window);
            if (windowObject != null)
            {
                var hasFocusProp = windowObject.FindProperty("m_HasFocus");
                if (hasFocusProp != null)
                {
                    hasFocusProp.boolValue = true;
                    windowObject.ApplyModifiedProperties();
                }
            }
        }
    }

    private void FocusHierarchyWindow()
    {
        Type hierarchyWindowType = Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
        if (hierarchyWindowType != null)
        {
            var window = EditorWindow.GetWindow(hierarchyWindowType);
            window.Focus();
            
            // Force the hierarchy window to grab keyboard focus
            var windowObject = new SerializedObject(window);
            if (windowObject != null)
            {
                var hasFocusProp = windowObject.FindProperty("m_HasFocus");
                if (hasFocusProp != null)
                {
                    hasFocusProp.boolValue = true;
                    windowObject.ApplyModifiedProperties();
                }
            }
        }
    }

    private void SelectGameObjectWithComponent(GameObject obj, Component component)
    {
        if (obj != null)
        {
            AddRecentAction($"{obj.name} ({component.GetType().Name})", 
                GetGameObjectPath(obj), 
                SearchMode.Components,
                component.GetType().AssemblyQualifiedName);
            
            Selection.activeGameObject = obj;
            if (SceneView.lastActiveSceneView != null)
            {
                FrameGameObject(obj);
            }
            
            var editor = UnityEditor.Editor.CreateEditor(component);
            if (editor != null)
            {
                Selection.activeObject = component;
                UnityEngine.Object.DestroyImmediate(editor);
            }
            
            EditorGUIUtility.PingObject(obj);
            FocusHierarchyWindow();
        }
    }

    private void SelectGameObject(GameObject obj)
    {
        if (obj != null)
        {
            AddRecentAction(obj.name, GetGameObjectPath(obj), SearchMode.GameObjects);
            Selection.activeGameObject = obj;
            if (SceneView.lastActiveSceneView != null)
            {
                FrameGameObject(obj);
            }
            EditorGUIUtility.PingObject(obj);
            FocusHierarchyWindow();
        }
    }

    private void FrameGameObject(GameObject obj)
    {
        var view = SceneView.lastActiveSceneView;
        
        var renderers = obj.GetComponentsInChildren<Renderer>();
        var colliders = obj.GetComponentsInChildren<Collider>();
        
        Bounds bounds = new Bounds(obj.transform.position, Vector3.one * 0.1f);
        
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        foreach (var collider in colliders)
        {
            bounds.Encapsulate(collider.bounds);
        }
        
        if (renderers.Length == 0 && colliders.Length == 0)
        {
            bounds = new Bounds(obj.transform.position, Vector3.one * 2f);
        }
        
        bounds.size *= 1.2f;
        
        view.Frame(bounds, false);
    }

    private void CreateItem(CreateMenuItem item)
    {
        try
        {
            AddRecentAction(item.DisplayName, item.FullPath ?? item.Type?.Name, SearchMode.Create);
            
            if (!string.IsNullOrEmpty(item.FullPath))
            {
                EditorApplication.ExecuteMenuItem(item.FullPath);
            }
            else if (item.Type != null)
            {
                var path = GetNewAssetPath(item.DisplayName + ".asset");
                var asset = ScriptableObject.CreateInstance(item.Type);
                ProjectWindowUtil.CreateAsset(asset, path);
            }
            FocusProjectWindow();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create item: {e.Message}");
        }
        finally
        {
            Close();
        }
    }

    private string GetNewAssetPath(string defaultName)
    {
        var path = "Assets";
        if (Selection.activeObject != null)
        {
            path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }
        }
        return AssetDatabase.GenerateUniqueAssetPath(path + "/" + defaultName);
    }

    private class CreateMenuItem
    {
        public string DisplayName;
        public string FullPath;
        public Type Type;

        public override bool Equals(object obj) => 
            obj is CreateMenuItem other && 
            DisplayName == other.DisplayName &&
            FullPath == other.FullPath;

        public override int GetHashCode() => 
            HashCode.Combine(DisplayName, FullPath);
    }
}