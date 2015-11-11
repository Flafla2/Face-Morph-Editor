using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Headmesh : MonoBehaviour {

    public Morph[] Morphs
    {
        get { return _Morphs; }
    }
    private Morph[] _Morphs = new Morph[] {

        new Morph { Name = "Size",          NameInternal = "neck_size",         HasNegativeValues = true,  Category = "Neck" },
        new Morph { Name = "Size",          NameInternal = "upper_lip_size",    HasNegativeValues = false, Category = "Lip>Upper" },
        new Morph { Name = "Size",          NameInternal = "lower_lip_size",    HasNegativeValues = false, Category = "Lip>Lower" },
        new Morph { Name = "Depth",         NameInternal = "mouth_depth",       HasNegativeValues = true,  Category = "Mouth" },
        new Morph { Name = "Width",         NameInternal = "mouth_width",       HasNegativeValues = true,  Category = "Mouth" },
        new Morph { Name = "Height",        NameInternal = "nose_height",       HasNegativeValues = true,  Category = "Nose" },
        new Morph { Name = "Angle",         NameInternal = "nose_angle",        HasNegativeValues = true,  Category = "Nose" },
        new Morph { Name = "Depth",         NameInternal = "nose_depth",        HasNegativeValues = true,  Category = "Nose" },
        new Morph { Name = "Depth",         NameInternal = "nose_tip_depth",    HasNegativeValues = true,  Category = "Nose>Tip" },
        new Morph { Name = "Height",        NameInternal = "nose_tip_height",   HasNegativeValues = true,  Category = "Nose>Tip" },
        new Morph { Name = "Width",         NameInternal = "chin_width",        HasNegativeValues = false, Category = "Chin" },
        new Morph { Name = "Height",        NameInternal = "chin_height",       HasNegativeValues = false, Category = "Chin" },
        new Morph { Name = "Hairline Puff", NameInternal = "hairline_puff",     HasNegativeValues = false, Category = "" },
        new Morph { Name = "Fat",           NameInternal = "cheek_fat",         HasNegativeValues = true,  Category = "Cheek" },
        new Morph { Name = "Depth",         NameInternal = "cheek_depth",       HasNegativeValues = true,  Category = "Cheek" }

    };

    [SerializeField]
    private float[] MorphValues;
    private SkinnedMeshRenderer SkinnedRenderer;

    void Awake()
    {
        MorphValues = new float[Morphs.Length];
        SkinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }
    
    public void SetMorphValue(int morph_index, float value)
    {
        Morph morph = Morphs[morph_index];
        if (morph.HasNegativeValues && value < 0)
            return;
        value = Mathf.Clamp01(value);

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
                SkinnedRenderer.SetBlendShapeWeight(x, Mathf.Abs(value));
                break;
            }
        }
        
        MorphValues[morph_index] = value;
    }

    public float GetMorphValue(int morph_index)
    {
        return MorphValues[morph_index];
    }

    public struct Morph
    {
        public string Name;
        public string NameInternal;
        public string Category;
        public bool HasNegativeValues;
        public float Value;
    }
}
