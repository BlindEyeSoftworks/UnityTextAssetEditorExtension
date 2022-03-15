// (c) 2022 BlindEye Softworks. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class TextAssetCreator : Editor
{
    [MenuItem("Assets/Create/Text Asset/Text File")]
    public static void CreateTextFile() => CreateAsset("NewTextFile.txt");

    [MenuItem("Assets/Create/Text Asset/HTML File")]
    public static void CreateHTMLFile() => CreateAsset("NewHTMLFile.html");

    [MenuItem("Assets/Create/Text Asset/HTM File")]
    public static void CreateHTMFile() => CreateAsset("NewHTMFile.htm");

    [MenuItem("Assets/Create/Text Asset/XML File")]
    public static void CreateXMLFile() => CreateAsset("NewXMLFile.xml");

    [MenuItem("Assets/Create/Text Asset/JSON File")]
    public static void CreateJSONFile() => CreateAsset("NewJSONFile.json");

    [MenuItem("Assets/Create/Text Asset/CSV File")]
    public static void CreateCSVFile() => CreateAsset("NewCSVFile.csv");

    [MenuItem("Assets/Create/Text Asset/YAML File")]
    public static void CreateYAMLFile() => CreateAsset("NewYAMLFile.yaml");

    private static void CreateAsset(string filename)
    {
        /* Ensure that we both create and give focus to a new project browser window if one does
           not exist in the current layout. */
        EditorWindow projectBrowser = EditorWindow.GetWindow(
            Type.GetType("UnityEditor.ProjectBrowser,UnityEditor.dll"), false, "Project", true);

        // Retrieve the users project browser folder selection history currently residing in memory.
        var folderHistory = (string[])projectBrowser.GetType()
            .GetField("m_LastFolders", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(projectBrowser);

        string projectPath = Application.dataPath,
               relativeSelectionPath = (Selection.activeObject != null) ?
                   AssetDatabase.GetAssetPath(Selection.activeObject) :
                   (folderHistory.Length != 0) ? folderHistory[0] : "Assets",
               /* Since Selection.activeObject emits a UnityEngine.DefaultAsset object which does
                  not expose a specific type ("yet" - per Unity Technologies) we must differentiate
                  between folder and file paths ourselves for sanitization purposes. */
               relativeAssetPath = Directory.Exists(relativeSelectionPath) ?
                    relativeSelectionPath : Path.GetDirectoryName(relativeSelectionPath),
               /* The Application.dataPath property contained in Unity's engine API emits the FQPN
                  of the project folder ("Assets") whereas the AssetDatabase.GetAssetPath method
                  in Unity's editor API returns the RPN of an asset with the project folder as the
                  root. Due to this, as well for both APIs not canonicalizing paths for Windows,
                  platform-agnostic path concatenation may become problematic. Microsot's .NET
                  runtime implementation for Windows starting with Framework 4.6.2 will implicitly
                  defer to the Win32 APIs GetPathFullName function for path normalization, but can
                  also be performed explicitly via the managed Path.GetFullPath method which wraps
                  a call to the native Windows function above. For Windows, this enables us to
                  utilize path specifiers such as '..' when concatenating path segments. */
               uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(
                   Path.Combine(relativeAssetPath, filename)),
               // Path.Combine incorrectly concatenates path segments when passing path specifiers.
               absoluteAssetPath = Path.GetFullPath(projectPath + Path.DirectorySeparatorChar +
                   ".." + Path.DirectorySeparatorChar + uniqueAssetPath);

        TryCreateAsset(absoluteAssetPath, out bool wasSuccessful);

        if (!wasSuccessful)
            return;

        AssetDatabase.ImportAsset(uniqueAssetPath);
        Selection.activeObject = AssetDatabase
            .LoadAssetAtPath<UnityEngine.Object>(uniqueAssetPath);
    }

    private static void TryCreateAsset(string path, out bool wasSuccessful)
    {
        wasSuccessful = true;

        try
        {
            // Mitigate locking Unity's internal file system watcher in an infinite loop.
            File.Create(path).Dispose();
        }
        catch (Exception e)
        {
            wasSuccessful = false;
            EditorUtility.DisplayDialog("Create Text Asset", e.Message, "OK");
        }
    }
}

[CustomEditor(typeof(TextAsset))]
public class TextAssetEditor : Editor
{
    string path,
           contents;

    private void OnEnable()
    {
        path = AssetDatabase.GetAssetPath(target);

        try
        {
            contents = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Open Text Asset", e.Message, "OK");
        }
    }

    public override void OnInspectorGUI()
    {
        GUI.enabled = true;
        contents = EditorGUILayout.TextArea(contents);

        if (GUILayout.Button("Save"))
        {
            TrySaveAsset(path, out bool wasSuccessful);

            if (wasSuccessful)
                EditorWindow.focusedWindow.ShowNotification(
                    new GUIContent() { text = "File Saved" }, .25);
        }
    }

    private void TrySaveAsset(string path, out bool wasSuccessful)
    {
        wasSuccessful = true;

        try
        {
            File.WriteAllText(path, contents);
        }
        catch (Exception e)
        {
            wasSuccessful = false;
            EditorUtility.DisplayDialog("Save Text Asset", e.Message, "OK");
        }
    }
}
