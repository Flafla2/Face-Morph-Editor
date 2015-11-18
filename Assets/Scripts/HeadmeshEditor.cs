using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(Headmesh))]
public class HeadmeshEditor : Editor {

    private MorphDirectory Root;
    private Headmesh head;
    private string writepath = null;

    void OnEnable()
    {
        head = target as Headmesh;
        head.LoadFile();

        Root = new MorphDirectory();
        for (int x = 0; x < head.Morphs.Length; x++)
            PlaceMorph(Root, head.Morphs[x], x);

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        Root = new MorphDirectory();
        for (int x = 0; x < head.Morphs.Length; x++)
            PlaceMorph(Root, head.Morphs[x], x);
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

        EditorGUI.BeginChangeCheck();
        TextAsset asset = EditorGUILayout.ObjectField("Data File: ", head.Datafile, typeof(TextAsset), false) as TextAsset;
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(head, "Change Headmesh Datafile");

            head.Datafile = asset;
            head.DatafilePath = AssetDatabase.GetAssetPath(asset);
            head.LoadFile();

            Root = new MorphDirectory();
            for (int x = 0; x < head.Morphs.Length; x++)
                PlaceMorph(Root, head.Morphs[x], x);

            EditorUtility.SetDirty(head);
        }

        TraverseTree(Root);

        if (GUILayout.Button("Randomize"))
        {
            Undo.RecordObject(head, "Randomize Headmesh");
            head.Randomize();
            EditorUtility.SetDirty(head);
        }

        if (head.Datafile == null || head.DatafilePath == null)
            return;

        if(writepath == null)
        {
            string[] split = head.DatafilePath.Split('/');
            Array.Resize<string>(ref split, split.Length - 1);
            writepath = string.Join("/", split) + "/saved.json";
        }

        writepath = EditorGUILayout.TextField("Save Path: ",writepath);

        GUI.enabled = writepath.EndsWith(".json");
        if (GUILayout.Button("Save"))
        {
            string path = Application.dataPath + "/" + writepath.Substring(7); //substring because Assets/ is contained in both paths
            path = path.Replace('/', Path.DirectorySeparatorChar);
            Debug.Log(path);
            File.WriteAllText(head.WriteJson(), path);
           
            head.DatafilePath = writepath;
            head.Datafile = Resources.Load<TextAsset>(writepath);
        }
        GUI.enabled = true;
            
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

    private bool PlaceMorph(MorphDirectory p, Headmesh.Morph q, int index)
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
        public List<Headmesh.Morph> submorphs;
        public List<int> submorph_indexes; // indexes in Headmesh.Morphs

        public string Name = "";
        public string Path = ""; // Uses > as path delimiter

        public bool Open = false; // Open in the inspector

        public MorphDirectory()
        {
            subdirectories = new List<MorphDirectory>();
            submorphs = new List<Headmesh.Morph>();
            submorph_indexes = new List<int>();
        }
    }
}
