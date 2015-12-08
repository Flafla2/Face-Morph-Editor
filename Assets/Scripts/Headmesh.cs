using UnityEngine;

namespace Morpher {
    [ExecuteInEditMode]
    public class Headmesh : MonoBehaviour {

        public Morph[] Morphs
        {
            get { return _Morphs; }
        }
        [SerializeField]
        private Morph[] _Morphs;

        public string Name { get; set; }

        public string DatafilePath
        {
            get
            {
                return _DatafilePath;
            }
        }
        [SerializeField]
        private string _DatafilePath;

        public string PrototypePath
        {
            get
            {
                return _PrototypePath;
            }
        }
        [SerializeField]
        private string _PrototypePath;

        public bool Modified
        {
            get
            {
                return _Modified;
            }
        }
        [SerializeField]
        private bool _Modified = false;

        public SkinnedMeshRenderer SkinnedRenderer;

        public void LoadFile(string path)
        {
            if (path == null || path.Trim().Equals(""))
            {
                Debug.Log("Loaded Invalid Path");
                return;
            }
            
            Debug.Log("Loading Datafile: " + path);
            MorphJsonType type = HeadmeshJsonReader.ReadMorphFile(path);
            _DatafilePath = path;
            _PrototypePath = type.Prototype;
            _Morphs = type.Morphs;
            for (int x = 0; x < _Morphs.Length; x++)
                SetMorphValue(x, (float)Morphs[x].Value); // Sets the value on the skinned mesh
            Name = type.Name;

            _Modified = false;
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
            _Modified = true;
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
    }

    [System.Serializable]
    public struct Morph
    {
        public string Name;
        public string NameInternal;
        public string Category;
        public bool HasNegativeValues;
        public double Value;
    }
}