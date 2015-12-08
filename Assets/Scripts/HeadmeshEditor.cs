using UnityEngine;
using UnityEditor;
using Morpher;
using System;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(Headmesh))]
public class HeadmeshEditor : Editor {

    // Internal //
    private MorphDirectory Root;
    private Headmesh head;
    private bool[] peripheral_open;

    // Temporary Resource Storage //
    private TextAsset asset;
    private string writepath = "Resources/saved.json";

    void OnEnable()
    {
        head = target as Headmesh;

        if (head.DatafilePath != null && !head.DatafilePath.Trim().Equals(""))
            asset = Resources.Load<TextAsset>(head.DatafilePath);

        ReloadDatafile();

        Undo.undoRedoPerformed += ReloadDatafile;
    }

    private void ReloadDatafile()
    {
        if (head.DatafilePath == null || head.DatafilePath.Trim().Equals(""))
            return;

        Root = new MorphDirectory();
        for (int x = 0; x < head.Morphs.Length; x++)
            PlaceMorph(Root, head.Morphs[x], x);

        peripheral_open = new bool[head.Peripherals.Length];
    }
    
    private string AbsolutePathToUnityPath(string absolute)
    {
        if (absolute == null)
            return null;

        if (absolute.ToLower().EndsWith(".json"))
            absolute = absolute.Substring(0, absolute.Length - 5);
        else
            return null;


        int res_index = absolute.IndexOf("/Resources/");
        if (res_index >= 0)
            absolute = absolute.Substring(res_index + 11);
        else
            return null;

        return absolute;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        SkinnedMeshRenderer rend = EditorGUILayout.ObjectField("Skinned Mesh Renderer: ", head.SkinnedRenderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(head, "Change Headmesh Renderer");
            head.SkinnedRenderer = rend;
            EditorUtility.SetDirty(head);
        }

        if (head.SkinnedRenderer == null)
            return;

        EditorGUILayout.BeginHorizontal();
        asset = EditorGUILayout.ObjectField("Change Data File Here: ", asset, typeof(TextAsset), false) as TextAsset;

        if (asset != null)
        {
            string asset_path = AbsolutePathToUnityPath(AssetDatabase.GetAssetPath(asset));
            bool DatafilePathValid = asset_path != null;

            if (!DatafilePathValid)
            {
                EditorGUILayout.HelpBox("Data file must be of type .json and must be located in a Resources folder or subfolder.", MessageType.Error);
            } else if (GUILayout.Button("Apply Datafile"))
            {
                Undo.RecordObject(head, "Change Headmesh Datafile");

                head.LoadFile(asset_path);
                ReloadDatafile();

                EditorUtility.SetDirty(head);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (head.DatafilePath == null || head.DatafilePath.Trim().Equals(""))
        {
            EditorGUILayout.HelpBox("Please input a data file to continue.", MessageType.Error);
            return;
        }

        GUILayout.Label("Current Datafile: " + head.DatafilePath + (head.Modified ? " (Modified)" : ""));

        if (Resources.Load<TextAsset>(head.DatafilePath) == null)
        {
            EditorGUILayout.HelpBox("Datafile path is invalid (perhaps it was moved or deleted).  Please reimport datafile.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Facial Morph Parameters", EditorStyles.boldLabel);

        TraverseTree(Root);

        if (GUILayout.Button("Randomize"))
        {
            Undo.RecordObject(head, "Randomize Headmesh");
            head.Randomize();
            EditorUtility.SetDirty(head);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Peripheral Selection", EditorStyles.boldLabel);

        for (int x=0; x < head.Peripherals.Length; x++)
        {
            peripheral_open[x] = EditorGUILayout.Foldout(peripheral_open[x], head.Peripherals[x].Name);

            if (!peripheral_open[x])
                continue;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            bool e = EditorGUILayout.Toggle("Enabled", head.GetPeripheralEnabled(x));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(head, "Toggle Peripheral (" + head.Peripherals[x].Name + ")");
                head.SetPeripheralEnabled(x, e);
                EditorUtility.SetDirty(head);
            }

            EditorGUI.BeginChangeCheck();
            Vector3 p = EditorGUILayout.Vector3Field("Offset", head.GetPeripheralOffset(x));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(head, "Change Peripheral Offset (" + head.Peripherals[x].Name + ")");
                head.SetPeripheralOffset(x, p);
                EditorUtility.SetDirty(head);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Save to JSON", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        writepath = EditorGUILayout.TextField("Save Path: ",writepath);
        if (EditorGUI.EndChangeCheck())
        {
            writepath = writepath.Replace('\\', '/');
            if (writepath.StartsWith("/"))
                writepath = writepath.Substring(1);
            if (writepath.ToLower().StartsWith("assets/"))
                writepath = writepath.Substring(7);
        }

        string writepath_unity = AbsolutePathToUnityPath("Assets/"+writepath);
        string message = "Hover over save buttons for more information";

        GUILayout.BeginHorizontal();

        GUI.enabled = writepath_unity != null;
        bool absolute = GUILayout.Button("Save Absolute");
        Rect rect = GUILayoutUtility.GetLastRect();
        if (GUI.enabled && rect.Contains(Event.current.mousePosition))
            message = "Save as a standalone file, without using parent files or inheritance.";

        GUI.enabled = writepath_unity != null && !writepath_unity.Equals(head.DatafilePath);
        bool deriv = GUILayout.Button("Save as Derivative");
        rect = GUILayoutUtility.GetLastRect();
        if (GUI.enabled && rect.Contains(Event.current.mousePosition))
            message = "Save as a derivative of the currently loaded file, using the currently loaded file ("+head.DatafilePath+") as the parent / prototype.  Only changes are saved, so any changes in the parent file will appear when loaded.";

        GUI.enabled = writepath_unity != null && !head.PrototypePath.Equals("") && !writepath_unity.Equals(head.PrototypePath);
        bool sibling = GUILayout.Button("Save as Sibling");
        rect = GUILayoutUtility.GetLastRect();
        if (GUI.enabled && rect.Contains(Event.current.mousePosition))
            message = "Save as a sibling of the currently loaded file, using the parent of the currently loaded file (" + head.PrototypePath + ") as the parent / prototype.  Only changes are saved, so any changes in the parent file will appear when loaded.";

        GUI.enabled = true;
        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(message, MessageType.Info);

        if (absolute || deriv || sibling)
        {
            string path = Application.dataPath + "/" + writepath;
            path = path.Replace('/', Path.DirectorySeparatorChar);

            MorphSaveType type = MorphSaveType.Absolute;
            if (deriv)
                type = MorphSaveType.Derivative;
            else if (sibling)
                type = MorphSaveType.Sibling;

            File.WriteAllText(path, HeadmeshJsonWriter.WriteJson(type,head));
            AssetDatabase.ImportAsset("Assets/" + writepath);

            head.LoadFile(writepath_unity);
            ReloadDatafile();
            EditorUtility.SetDirty(head);
        }
        
    }

    public override bool RequiresConstantRepaint()
    {
        return false;
    }

    private void TraverseTree(MorphDirectory p)
    {
        if(!p.Path.Equals(""))
        {
            p.Open = EditorGUILayout.Foldout(p.Open, p.Name);

            if (!p.Open)
                return;

            EditorGUI.indentLevel++;
        }

        foreach (MorphDirectory q in p.subdirectories)
            TraverseTree(q);
        
        for (int x=0;x<p.submorphs.Count;x++)
        {
            EditorGUI.BeginChangeCheck();

            float slider = EditorGUILayout.Slider(
                p.submorphs[x].Name,
                (float)head.GetMorphValue(p.submorph_indexes[x]),
                p.submorphs[x].HasNegativeValues ? -1 : 0,
                1);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(head, "Change Head Morph");
                head.SetMorphValue(p.submorph_indexes[x], slider);
                EditorUtility.SetDirty(head);
            }
        }

        if(!p.Path.Equals(""))
            EditorGUI.indentLevel--;
    }

    private bool PlaceMorph(MorphDirectory p, Morph q, int index)
    {
        if(p.Path.Equals(q.Category))
        {
            p.submorphs.Add(q);
            p.submorph_indexes.Add(index);
            return true;
        }

        foreach (MorphDirectory sub in p.subdirectories)
        {
            if (PlaceMorph(sub, q, index))
                return true;
        }
                

        if(q.Category.StartsWith(p.Path))
        {
            MorphDirectory d = new MorphDirectory();
            
            if (p.Path.Equals(""))
            {
                d.Name = q.Category.Split('>')[0];
                d.Path = d.Name;
            }
            else
            {
                d.Name = q.Category.Substring(p.Path.Length+1).Split('>')[0];
                d.Path = p.Path + '>' + d.Name;
            }
            p.subdirectories.Add(d);
            return PlaceMorph(d, q, index);
        }

        return false;
    }

    private class MorphDirectory
    {
        public List<MorphDirectory> subdirectories;
        public List<Morph> submorphs;
        public List<int> submorph_indexes; // indexes in Headmesh.Morphs

        public string Name = "";
        public string Path = ""; // Uses > as path delimiter

        public bool Open = false; // Open in the inspector

        public MorphDirectory()
        {
            subdirectories = new List<MorphDirectory>();
            submorphs = new List<Morph>();
            submorph_indexes = new List<int>();
        }
    }
}
