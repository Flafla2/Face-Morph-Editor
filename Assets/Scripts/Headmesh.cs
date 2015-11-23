using UnityEngine;
using LitJson;
using System;
using System.Collections.Generic;
using System.Text;

public class Headmesh : MonoBehaviour {

    public Morph[] Morphs
    {
        get { return _Morphs; }
    }
    private Morph[] _Morphs;

    public string Name { get; set; }

    public string DatafilePath = null;
    public SkinnedMeshRenderer SkinnedRenderer;

    void Awake()
    {
        LoadFile();
    }

    public void LoadFile()
    {
        MorphJsonType type = ReadMorphFile(DatafilePath);
        _Morphs = type.Morphs;
        for (int x = 0; x < _Morphs.Length; x++)
            SetMorphValue(x, (float)Morphs[x].Value); // Sets the value on the skinned mesh
        Name = type.Name;
    }

    public string WriteJson(bool prototype)
    {
        MorphJsonType type = new MorphJsonType();
        type.Morphs = Morphs;
        type.Name = Name;
        if (prototype)
            type.Prototype = DatafilePath;
        else
            type.Prototype = "";

        StringBuilder sb = new StringBuilder();
        JsonWriter wr = new JsonWriter(sb);
        wr.PrettyPrint = true;
        JsonMapper.ToJson(type, wr);
        return sb.ToString();
    }

    private static MorphJsonType ReadMorphFile(string DataPath, bool add_elements = false)
    {
        TextAsset Data = Resources.Load<TextAsset>(DataPath);
        if (Data == null) // Can't find raw
        {
            MorphJsonType morph = new MorphJsonType();
            morph.Morphs = new Morph[0];
            return morph;
        }

        string json = Data.text;
        MorphJsonType raw = JsonMapper.ToObject<MorphJsonType>(json);

        bool has_prototype = !raw.Prototype.Equals("");

        if (has_prototype)
        {
            string[] split = DataPath.Split('/');
            Array.Resize<string>(ref split, split.Length - 1);

            TextAsset prototype = Resources.Load<TextAsset>(raw.Prototype);
            if (prototype == null) // Can't find prototype
            { 
                MorphJsonType morph = new MorphJsonType();
                morph.Morphs = new Morph[0];
                return morph;
            }

            MorphJsonType ptype = ReadMorphFile(raw.Prototype, add_elements);
            if (raw.Name == null)
                ptype.Name = raw.Name;
            Resources.UnloadAsset(prototype);prototype = null;

            List<Morph> temp = new List<Morph>(ptype.Morphs.Length);
            for (int x = 0; x < ptype.Morphs.Length; x++)
            {
                Morph m = ptype.Morphs[x];
                bool found = false;
                foreach (Morph q in raw.Morphs)
                {
                    if (q.NameInternal.Equals(m.NameInternal))
                    {
                        Debug.Log(m.NameInternal + " " + q.Value);
                        m.Value = q.Value;
                        m.Name = q.Name;
                        m.Category = q.Category;
                        m.HasNegativeValues = q.HasNegativeValues;
                        found = true;

                        temp.Add(m);
                        break;
                    }
                }
                if(!found && add_elements)
                    temp.Add(m);
            }
            ptype.Morphs = temp.ToArray();
            return ptype;
        }
        else
            return raw;
    }
    
    public void SetMorphValue(int morph_index, float value)
    {
        Morph morph = Morphs[morph_index];
        if (!morph.HasNegativeValues && value < 0)
            return;
        value = Mathf.Clamp(value,-1,1);

        string shapename = morph.NameInternal;
        if(morph.HasNegativeValues)
        {
            if (value > 0)
                shapename += "_pos";
            else
                shapename += "_neg";
        }

        Mesh m = SkinnedRenderer.sharedMesh;
        for(int x=0;x<m.blendShapeCount;x++)
        {
            if (m.GetBlendShapeName(x).Equals(shapename))
            {
                SkinnedRenderer.SetBlendShapeWeight(x, Mathf.Abs(value)*100);
                break;
            }
        }
        
        Morphs[morph_index].Value = value;
    }

    public void Randomize()
    {
        for (int x = 0; x < Morphs.Length; x++)
        {
            float min = Morphs[x].HasNegativeValues ? -1 : 0;
            if(!Morphs[x].NameInternal.StartsWith("hairline"))
                SetMorphValue(x, UnityEngine.Random.Range(min, 1));
        }
    }

    public double GetMorphValue(int morph_index)
    {
        return Morphs[morph_index].Value;
    }

    public struct Morph
    {
        public string Name;
        public string NameInternal;
        public string Category;
        public bool HasNegativeValues;
        public double Value;
    }

    private struct MorphJsonType
    {
        public string Name;
        public string Prototype;
        public Morph[] Morphs;
    }
}
