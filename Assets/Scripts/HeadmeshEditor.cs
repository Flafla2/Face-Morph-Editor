using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Headmesh))]
public class HeadmeshEditor : Editor {

    private MorphDirectory Root;
    private Headmesh head;

    public override void OnInspectorGUI()
    {
        head = target as Headmesh;

        if(Root == null)
        {
            Root = new MorphDirectory();
            for(int x=0;x<head.Morphs.Length;x++)
                PlaceMorph(Root, head.Morphs[x], x);
        }

        TraverseTree(Root);
    }

    private void TraverseTree(MorphDirectory p)
    {
        if(!p.Path.Equals(""))
        {
            p.Open = EditorGUILayout.Foldout(p.Open, p.Name);

            if (!p.Open)
                return;
        }
        
        foreach (MorphDirectory q in p.subdirectories)
            TraverseTree(q);

        for(int x=0;x<p.submorphs.Count;x++)
        {
            EditorGUI.BeginChangeCheck();

            float slider = EditorGUILayout.Slider(
                p.submorphs[x].Name,
                head.GetMorphValue(p.submorph_indexes[x]),
                p.submorphs[x].HasNegativeValues ? -1 : 0,
                1);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(head, "Change Head Morph");
                head.SetMorphValue(p.submorph_indexes[x], slider);
                EditorUtility.SetDirty(head);
            }
        }
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
            {
                p.submorphs.Add(q);
                p.submorph_indexes.Add(index);
                return true;
            }
        }
                

        if(q.Category.StartsWith(p.Path))
        {
            MorphDirectory d = new MorphDirectory();
            d.Name = q.Category.Substring(p.Path.Length).Split('>')[0];
            d.Path = p.Path+'>'+d.Name;
            p.subdirectories.Add(d);
            return PlaceMorph(d,q, index);
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
