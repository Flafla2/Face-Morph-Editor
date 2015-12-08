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

        private static void WriteJsonPeripheral(ref JsonWriter wr, Peripheral cur, Peripheral par, bool write_all)
        {
            wr.WriteObjectStart();
            wr.WritePropertyName("ResourcePath");
            wr.Write(cur.ResourcePath);

            if (write_all || !cur.Name.Equals(par.Name))
            {
                wr.WritePropertyName("Name");
                wr.Write(cur.Name);
            }

            if (write_all || !cur.Bone.Equals(par.Bone))
            {
                wr.WritePropertyName("Bone");
                wr.Write(cur.Bone);
            }

            if (write_all || cur.Enabled != par.Enabled)
            {
                wr.WritePropertyName("Enabled");
                wr.Write(cur.Enabled);
            }

            if (write_all || cur.Offset != par.Offset)
            {
                wr.Write("Offset_X");
                wr.Write(cur.Offset.x);
                wr.Write("Offset_Y");
                wr.Write(cur.Offset.y);
                wr.Write("Offset_Z");
                wr.Write(cur.Offset.z);
            }
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

            wr.WriteObjectStart();
            wr.WritePropertyName("Name");
            wr.Write(type.Name);
            wr.WritePropertyName("Prototype");
            wr.Write(type.Prototype);

            WriteJsonArray<Morph>(ref wr, "Morphs", type.Morphs, loaded_ptype.Morphs, WriteJsonMorph, 
                (Morph o1, Morph o2) => (o1.NameInternal.Equals(o2.NameInternal)));

            WriteJsonArray<Peripheral>(ref wr, "Peripherals", type.Peripherals, loaded_ptype.Peripherals, WriteJsonPeripheral,
                (Peripheral o1, Peripheral o2) => (o1.ResourcePath.Equals(o2.ResourcePath)));

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