﻿using UnityEngine;
using LitJson;
using System;
using System.Collections.Generic;

namespace Morpher
{
    public class HeadmeshJsonReader
    {

        private delegate T ParseJsonObject<T>(string PropertyName, JsonToken token, object Value);

        // Parse a generic JSON array, in the middle of a ReadMorphFileRaw() call.  We essentially begin where 
        // ReadMorphFileRaw() left off.  In the end we generate an array of whatever element we want, using the
        // ParseJsonObject delegate to handle the individual element parsing.
        private static List<T> ParseJsonArray<T>(ref JsonReader reader, ParseJsonObject<T> parser)
        {
            List<T> temp = new List<T>();
            reader.Read(); // Skip ArrayStart
            while (reader.Token != JsonToken.ArrayEnd)
            {
                if (reader.Token != JsonToken.ObjectStart)
                {
                    Debug.LogError("Json Reader ERROR: Morphs array must only contain objects (Found " + reader.Token + ").  Aborting.");
                    return null;
                }

                reader.Read();
                while (reader.Token != JsonToken.ObjectEnd)
                {
                    if (reader.Token != JsonToken.PropertyName)
                    {
                        Debug.LogError("Json Reader ERROR: All properties in the json file must have names.");
                        return null;
                    }

                    string propname = (reader.Value as string).ToLower();
                    reader.Read(); // Move from property name to actual value

                    T item = parser(propname, reader.Token, reader.Value);
                    if(item != null)
                        temp.Add(item);

                    reader.Read(); // Go to next line token
                }
                reader.Read(); // Skip ObjectEnd
            }
            reader.Read(); // Skip ArrayEnd

            return temp;
        }

        // Implementation of ParseJsonObject for a Morph.  Used for ParseJsonArray<Morph>()
        private static Morph ParseJsonMorph(string PropertyName, JsonToken token, object Value)
        {
            Morph cur = new Morph() { Name = "", NameInternal = "", Category = "" };

            if (PropertyName.Equals("name"))
                cur.Name = Value as string;
            else if (PropertyName.Equals("nameinternal"))
                cur.NameInternal = Value as string;
            else if (PropertyName.Equals("hasnegativevalues"))
                cur.HasNegativeValues = (bool)Value;
            else if (PropertyName.Equals("value"))
            {
                Type t = Value.GetType();
                if (t == typeof(Int32))
                    cur.Value = (Int32)Value;
                else if (t == typeof(Double))
                    cur.Value = (Double)Value;
                else
                    Debug.LogError("Incorrect type for Morph Value - should be Double or Int!");
            }
            else if (PropertyName.Equals("category"))
                cur.Category = Value as string;

            return cur;
        }

        private static MorphJsonType ReadMorphFileRaw(string json)
        {
            JsonReader reader = new JsonReader(json);
            MorphJsonType ret = new MorphJsonType();
            ret.Prototype = "";
            while (reader.Read())
            {
                if (reader.Token == JsonToken.ObjectStart || reader.Token == JsonToken.ObjectEnd)
                    continue;

                if (reader.Token != JsonToken.PropertyName)
                {
                    Debug.LogError("Json Reader ERROR: All properties in the json file must have names (Found token " + reader.Token + " instead).");
                    return new MorphJsonType() { Name = "", Morphs = new Morph[0], Prototype = "" };
                }

                JsonToken expected = JsonToken.None;
                string val = (reader.Value as string).ToLower();

                if (val.Equals("prototype") || val.Equals("name"))
                    expected = JsonToken.String;
                else if (val.Equals("hasnegativevalues"))
                    expected = JsonToken.Boolean;
                else if (val.Equals("morphs"))
                    expected = JsonToken.ArrayStart;
                else if (val.Equals("value"))
                    expected = JsonToken.Double;

                reader.Read();
                if (reader.Token != expected
                    && !(expected == JsonToken.Double && reader.Token == JsonToken.Int)) // allows for 0 instead of 0.0
                {
                    Debug.LogError("Json Reader ERROR: Incorrect data type for property \"" + val + "\": " + reader.Token.ToString() + " (Expected " + expected + ").  Aborting.");
                    return new MorphJsonType() { Name = "", Morphs = new Morph[0], Prototype = "" };
                }

                if (val.Equals("prototype"))
                    ret.Prototype = reader.Value as string;
                else if (val.Equals("name"))
                    ret.Name = reader.Value as string;
                else if (val.Equals("morphs"))
                {
                    List<Morph> morphs = ParseJsonArray<Morph>(ref reader, ParseJsonMorph);
                    if (morphs == null)
                        ret.Morphs = new Morph[0];
                    else
                        ret.Morphs = morphs.ToArray();
                }
            }
            return ret;
        }

        public static MorphJsonType ReadMorphFile(string DataPath, bool add_elements = false)
        {
            TextAsset Data = Resources.Load<TextAsset>(DataPath);
            if (Data == null) // Can't find raw
            {
                MorphJsonType morph = new MorphJsonType();
                morph.Morphs = new Morph[0];
                return morph;
            }

            string json = Data.text;
            MorphJsonType raw = ReadMorphFileRaw(json);

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
                    raw.Name = ptype.Name;
                Resources.UnloadAsset(prototype); prototype = null;

                List<Morph> temp = new List<Morph>(ptype.Morphs.Length);
                for (int x = 0; x < ptype.Morphs.Length; x++)
                {
                    Morph m = ptype.Morphs[x];
                    bool found = false;
                    foreach (Morph q in raw.Morphs)
                    {
                        if (q.NameInternal.Equals(m.NameInternal))
                        {
                            m.Value = q.Value;
                            m.Name = q.Name;
                            m.Category = q.Category;
                            m.HasNegativeValues = q.HasNegativeValues;
                            found = true;

                            temp.Add(m);
                            break;
                        }
                    }
                    if (!found && add_elements)
                        temp.Add(m);
                }
                ptype.Morphs = temp.ToArray();
                return ptype;
            }
            else
                return raw;
        }
    }

    public struct MorphJsonType
    {
        public string Name;
        public string Prototype;
        public Morph[] Morphs;
    }
}