using UnityEngine;
using LitJson;

[ExecuteInEditMode]
public class Headmesh : MonoBehaviour {

    public Morph[] Morphs
    {
        get { return _Morphs; }
    }

    private Morph[] _Morphs;

    public string DatafilePath;  // A datafile that
    public SkinnedMeshRenderer SkinnedRenderer;

    void Awake()
    {
        
    }

    public MorphJsonType ReadMorphFile(string DataPath)
    {
        string json = Resources.Load<TextAsset>(DataPath).text;
        MorphJsonType raw = JsonMapper.ToObject<MorphJsonType>(json);
        
        if(!raw.Prototype.Equals(""))
        {
            TextAsset prototype = Resources.Load<TextAsset>(Data.)
        }
        
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
                SetMorphValue(x, Random.Range(min, 1));
        }
    }

    public float GetMorphValue(int morph_index)
    {
        return Morphs[morph_index].Value;
    }

    public struct Morph
    {
        public string Name;
        public string NameInternal;
        public string Category;
        public bool HasNegativeValues;
        public float Value;
    }

    private struct MorphJsonType
    {
        public string Name;
        public string Prototype;
        public Morph[] Morphs;
    }
}
