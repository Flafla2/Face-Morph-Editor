using UnityEngine;
using System;

using System.Collections.Generic;

namespace Morpher {
    [ExecuteInEditMode]
    public class Headmesh : MonoBehaviour {

        // TODO: Properly encapsulate Morphs and Peripherals
        public Morph[] Morphs
        {
            get { return _Morphs; }
        }
        [SerializeField]
        private Morph[] _Morphs;

        public Peripheral[] Peripherals
        {
            get { return _Peripherals; }
        }
        [SerializeField]
        private Peripheral[] _Peripherals;

        private Dictionary<string, Transform> LoadedPeripherals;

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

        void Start()
        {
            LoadedPeripherals = new Dictionary<string, Transform>();

            // Destroy any and all peripherals that may have persisted through saving the game.
            // This resets everything so we can reload them next.
            if(SkinnedRenderer != null)
            {
                foreach(Transform t in SkinnedRenderer.bones)
                {
                    Transform[] arr = new Transform[t.childCount];
                    foreach (Transform q in arr)
                        if (q.GetComponent<Renderer>() != null)
                            Destroy(q.gameObject);
                }
            }

            // "Enforce" saved morphs and peripherals
            // This makes sure that nothing happened to the skinned mesh renderer, etc between loads
            for (int x = 0; x < _Morphs.Length; x++)
                SetMorphValue(x, (float)Morphs[x].Value);
            for (int x = 0; x < _Peripherals.Length; x++)
                SetPeripheralEnabled(x, Peripherals[x].Enabled);
        }

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
            _Peripherals = type.Peripherals;
            for (int x = 0; x < Peripherals.Length; x++)
                SetPeripheralEnabled(x, Peripherals[x].Enabled);
            Name = type.Name;

            _Modified = false;
        }

        public void SetPeripheralEnabled(int per_index, bool enabled)
        {
            Peripheral per = Peripherals[per_index];
            string path = per.ResourcePath.ToLower();
            bool contains = LoadedPeripherals.ContainsKey(path);

            if (contains && !enabled)
            {
                Transform instance = LoadedPeripherals[path];
                LoadedPeripherals.Remove(path);
                Destroy(instance.gameObject);
            }
            else if(!contains && enabled)
            {
                GameObject load = Resources.Load<GameObject>(path);
                if(load == null)
                {
                    Debug.LogError("Tried to load prefab at path " + path + " but couldn't find it.  Make sure that the path" +
                        " is formatted correctly and that it points to the right place.");
                    return;
                }

                Transform bone = Array.Find<Transform>(SkinnedRenderer.bones, x => x.name.Equals(per.Bone));

                GameObject instance = Instantiate(load) as GameObject;
                instance.transform.SetParent(bone);
                instance.transform.localPosition = per.Offset;
            }
        }

        public bool GetPeripheralEnabled(int per_index)
        {
            return Peripherals[per_index].Enabled;
        }

        public void SetPeripheralOffset(int per_index, Vector3 Offset)
        {
            Peripheral per = Peripherals[per_index];
            string path = per.ResourcePath.ToLower();
            bool contains = LoadedPeripherals.ContainsKey(path);

            if (!contains)
                return;

            Transform instance = LoadedPeripherals[path];
            instance.localPosition = Offset;
        }

        public Vector3 GetPeripheralOffset(int per_index)
        {
            return Peripherals[per_index].Offset;
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

        public double GetMorphValue(int morph_index)
        {
            return Morphs[morph_index].Value;
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

    [System.Serializable]
    public struct Peripheral
    {
        public string Name;
        public string Bone;
        public string ResourcePath;
        public bool Enabled;
        public Vector3 Offset;
    }
}