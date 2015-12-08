using System.Text;
using LitJson;

namespace Morpher
{
    public class HeadmeshJsonWriter
    {
        private delegate void WriteJsonObject<T>(ref JsonWriter wr, T cur, T par, bool write_all);
        private delegate bool SameIdentifier<T>(T obj1, T obj2);

        private static void WriteJsonArray<T>(ref JsonWriter wr, string PropertyName, T[] arr_cur, T[] arr_par, WriteJsonObject<T> serializer, SameIdentifier<T> comparator)
        {
            wr.WritePropertyName(PropertyName);

            wr.WriteArrayStart();
            for (int x = 0; x < arr_cur.Length; x++)
            {
                T cur = arr_cur[x];
                T par = cur; // Set to cur because we can't set it to null, and it needs to equal something...
                bool found = false;
                for (int y = 0; y < arr_par.Length; y++)
                {
                    if (comparator(cur,arr_par[y]))
                    {
                        par = arr_par[y];
                        found = true;
                        break;
                    }
                }

                serializer(ref wr, cur, par, !found);
            }
            wr.WriteArrayEnd();
        }

        private static void WriteJsonMorph(ref JsonWriter wr, Morph cur, Morph par, bool write_all)
        {
            wr.WriteObjectStart();
            wr.WritePropertyName("NameInternal");
            wr.Write(cur.NameInternal);

            if (write_all || !cur.Name.Equals(par.Name))
            {
                wr.WritePropertyName("Name");
                wr.Write(cur.Name);
            }

            if (write_all || cur.HasNegativeValues != par.HasNegativeValues)
            {
                wr.WritePropertyName("HasNegativeValues");
                wr.Write(cur.HasNegativeValues);
            }

            if (write_all || !cur.Category.Equals(par.Category))
            {
                wr.WritePropertyName("Category");
                wr.Write(cur.Category);
            }

            if (write_all || cur.Value != par.Value)
            {
                wr.WritePropertyName("Value");
                wr.Write(cur.Value);
            }

            wr.WriteObjectEnd();
        }

        public static string WriteJson(MorphSaveType prototype, Headmesh mesh)
        {
            MorphJsonType type = new MorphJsonType();
            type.Morphs = mesh.Morphs;
            type.Name = mesh.Name;
            if (prototype == MorphSaveType.Derivative)
                type.Prototype = mesh.DatafilePath;
            else if (prototype == MorphSaveType.Sibling)
                type.Prototype = mesh.PrototypePath;
            else if (prototype == MorphSaveType.Absolute)
                type.Prototype = "";
            MorphJsonType loaded_ptype = HeadmeshJsonReader.ReadMorphFile(type.Prototype);

            StringBuilder sb = new StringBuilder();
            JsonWriter wr = new JsonWriter(sb);
            wr.PrettyPrint = true;
            //JsonMapper.ToJson(type, wr);

            wr.WriteObjectStart();
            wr.WritePropertyName("Name");
            wr.Write(type.Name);
            wr.WritePropertyName("Prototype");
            wr.Write(type.Prototype);

            WriteJsonArray<Morph>(ref wr, "Morphs", type.Morphs, loaded_ptype.Morphs, WriteJsonMorph, 
                (Morph o1, Morph o2) => (o1.NameInternal.Equals(o2.NameInternal)));

            return sb.ToString();
        }
    }

    public enum MorphSaveType
    {
        Absolute,   // Saves file absolutely, containing all data necessary to contruct headmesh
        Derivative, // Saves file with prototype set to the headmesh at DatafilePath.  Only changed values are saved.
        Sibling     // Saves file with prototype set to the prototype of the headmesh at DatafilePath.  Only changed values are saved.
    }
}