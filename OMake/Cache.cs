﻿using System;
using System.Collections.Generic;
using System.IO;

namespace OMake
{
    /// <summary>
    /// The Cache.
    /// </summary>
    public static class Cache
    {

        #region CacheObjectType
        private enum CacheObjectType : byte
        {
            String,
            Byte,
            SByte,
            Bool,
            UShort,
            Short,
            UInt,
            Int,
            ULong,
            Long,
            Float,
            Double,
            Decimal,
            Data
        }
        #endregion

        #region CacheObject
        private struct CacheObject
        {
            public string Name;
            public uint Size;
            public CacheObjectType Type;
            public object Value;

            public CacheObject(string name, CacheObjectType type, object value)
            {
                this.Name = name;
                this.Type = type;
                switch (type)
                {
                    case CacheObjectType.String:
                        this.Value = value;
                        this.Size = (uint)Encoding.GetBytes((string)value).Length;
                        break;
                    case CacheObjectType.Byte:
                        this.Value = value;
                        this.Size = 1;
                        break;
                    case CacheObjectType.SByte:
                        this.Value = value;
                        this.Size = 1;
                        break;
                    case CacheObjectType.Bool:
                        this.Value = value;
                        this.Size = 1;
                        break;
                    case CacheObjectType.UShort:
                        this.Value = value;
                        this.Size = 2;
                        break;
                    case CacheObjectType.Short:
                        this.Value = value;
                        this.Size = 2;
                        break;
                    case CacheObjectType.UInt:
                        this.Value = value;
                        this.Size = 4;
                        break;
                    case CacheObjectType.Int:
                        this.Value = value;
                        this.Size = 4;
                        break;
                    case CacheObjectType.ULong:
                        this.Value = value;
                        this.Size = 8;
                        break;
                    case CacheObjectType.Long:
                        this.Value = value;
                        this.Size = 8;
                        break;
                    case CacheObjectType.Float:
                        this.Value = value;
                        this.Size = 4;
                        break;
                    case CacheObjectType.Double:
                        this.Value = value;
                        this.Size = 8;
                        break;
                    case CacheObjectType.Decimal:
                        this.Value = value;
                        this.Size = 16;
                        break;
                    case CacheObjectType.Data:
                        this.Value = value;
                        this.Size = (uint)((byte[])value).Length;
                        break;
                    default:
                        throw new Exception("Unknown cache object type!");
                }
            }
        }
        #endregion

        /// <summary>
        /// This is the byte sequence for "OMCF" in little-endian ASCII.
        /// </summary>
        private const uint CacheHeader = (byte)'O' | ((byte)'M') << 8 | ((byte)'C') << 16 | ((byte)'F') << 24;
        /// <summary>
        /// The current version of the cache file format.
        /// If the file-format changes at all, increase this
        /// version.
        /// The first 2 bytes are the major version number
        /// the last 2 are the minor version number.
        /// </summary>
        private const uint OMCFVersion = 0x0001;
        private static readonly System.Text.Encoding Encoding = System.Text.ASCIIEncoding.UTF8;
        private static Dictionary<string, CacheObject> objects = new Dictionary<string, CacheObject>();

        public static void Initialize()
        {
            if (File.Exists(Processor.file.Filename + ".cache"))
            {
                FileStream fs = new FileStream(Processor.file.Filename + ".cache", FileMode.Open);
                LoadCache(new BinaryReader(fs, Encoding));
                fs.Flush();
                fs.Close();
                if (Contains("StartupDirectory"))
                {
                    string strupDir = GetString("StartupDirectory");
                    if (strupDir != Directory.GetCurrentDirectory())
                    {
                        ErrorManager.Warning(93, Processor.file);
                        objects = new Dictionary<string, CacheObject>();
                    }
                }
                else
                {
                    objects = new Dictionary<string, CacheObject>();
                }
            }
            SetValue("StartupDirectory", (string)Directory.GetCurrentDirectory());
        }


#pragma warning disable 465
        public static void Finalize()
        {
            FileStream s = new FileStream(Processor.file.Filename + ".cache", FileMode.OpenOrCreate);
            BinaryWriter br = new BinaryWriter(s);
            Cache.SaveCache(br);
            br.Flush();
            br.Close();
        }
#pragma warning restore 465

        #region Load Cache

        private static void LoadCache(BinaryReader rdr)
        {
            if (rdr.ReadUInt32() != CacheHeader)
            {
                ErrorManager.Warning(89, Processor.file, Processor.file.Filename + ".cache");
                return;
            }
            uint version = rdr.ReadUInt32();
            if (version > OMCFVersion)
            {
                ErrorManager.Error(90, Processor.file, OMCFVersion, version);
                return;
            }

            ulong objectCount = rdr.ReadUInt64();
            for (ulong i = 0; i < objectCount; i++)
            {
                CacheObject c = new CacheObject();
                c.Type = (CacheObjectType)rdr.ReadByte();

                #region Read Actual Data
                switch (c.Type)
                {
                    case CacheObjectType.String:
                        c.Size = rdr.ReadUInt32();
                        byte[] buf = rdr.ReadBytes((int)c.Size);
                        c.Value = Encoding.GetString(buf);
                        break;
                    case CacheObjectType.Byte:
                        c.Value = rdr.ReadByte();
                        c.Size = 1;
                        break;
                    case CacheObjectType.SByte:
                        c.Value = rdr.ReadSByte();
                        c.Size = 1;
                        break;
                    case CacheObjectType.Bool:
                        c.Value = (bool)(rdr.ReadByte() == 0 ? false : true);
                        c.Size = 1;
                        break;
                    case CacheObjectType.UShort:
                        c.Value = rdr.ReadUInt16();
                        c.Size = 2;
                        break;
                    case CacheObjectType.Short:
                        c.Value = rdr.ReadInt16();
                        c.Size = 2;
                        break;
                    case CacheObjectType.UInt:
                        c.Value = rdr.ReadUInt32();
                        c.Size = 4;
                        break;
                    case CacheObjectType.Int:
                        c.Value = rdr.ReadInt32();
                        c.Size = 4;
                        break;
                    case CacheObjectType.ULong:
                        c.Value = rdr.ReadUInt64();
                        c.Size = 8;
                        break;
                    case CacheObjectType.Long:
                        c.Value = rdr.ReadInt64();
                        c.Size = 8;
                        break;
                    case CacheObjectType.Float:
                        c.Value = rdr.ReadSingle();
                        c.Size = 4;
                        break;
                    case CacheObjectType.Double:
                        c.Value = rdr.ReadDouble();
                        c.Size = 8;
                        break;
                    case CacheObjectType.Decimal:
                        c.Value = rdr.ReadDecimal();
                        c.Size = 16;
                        break;
                    case CacheObjectType.Data:
                        c.Size = rdr.ReadUInt32();
                        c.Value = rdr.ReadBytes((int)c.Size);
                        break;
                    default:
                        throw new Exception("Unknown cache object type!");
                }
                #endregion

                uint strSize = rdr.ReadUInt32();
                byte[] buf2 = rdr.ReadBytes((int)strSize);
                c.Name = Encoding.GetString(buf2);
                if (objects.ContainsKey(c.Name))
                {
                    ErrorManager.Warning(91, Processor.file, c.Name);
                    objects[c.Name] = c;
                }
                else
                {
                    objects.Add(c.Name, c);
                }
            }
        }

        #endregion

        #region Save Cache
        private static void SaveCache(BinaryWriter wtr)
        {
            wtr.Write(CacheHeader);
            wtr.Write(OMCFVersion);
            wtr.Write((ulong)objects.Count);
            foreach (KeyValuePair<string, CacheObject> obj in objects)
            {
                CacheObject c = obj.Value;
                wtr.Write((byte)c.Type);

                #region Read Actual Data
                switch (c.Type)
                {
                    case CacheObjectType.String:
                        wtr.Write((uint)c.Size);
                        wtr.Write((byte[])Encoding.GetBytes((string)c.Value));
                        break;
                    case CacheObjectType.Byte:
                        wtr.Write((byte)c.Value);
                        break;
                    case CacheObjectType.SByte:
                        wtr.Write((sbyte)c.Value);
                        break;
                    case CacheObjectType.Bool:
                        wtr.Write((byte)(((bool)c.Value) ? 127 : 0));
                        break;
                    case CacheObjectType.UShort:
                        wtr.Write((ushort)c.Value);
                        break;
                    case CacheObjectType.Short:
                        wtr.Write((short)c.Value);
                        break;
                    case CacheObjectType.UInt:
                        wtr.Write((uint)c.Value);
                        break;
                    case CacheObjectType.Int:
                        wtr.Write((int)c.Value);
                        break;
                    case CacheObjectType.ULong:
                        wtr.Write((ulong)c.Value);
                        break;
                    case CacheObjectType.Long:
                        wtr.Write((long)c.Value);
                        break;
                    case CacheObjectType.Float:
                        wtr.Write((float)c.Value);
                        break;
                    case CacheObjectType.Double:
                        wtr.Write((double)c.Value);
                        break;
                    case CacheObjectType.Decimal:
                        wtr.Write((decimal)c.Value);
                        break;
                    case CacheObjectType.Data:
                        wtr.Write(c.Size);
                        wtr.Write((byte[])c.Value);
                        break;
                    default:
                        throw new Exception("Unknown cache object type!");
                }
                #endregion

                byte[] buf = Encoding.GetBytes(c.Name);
                wtr.Write((uint)buf.Length);
                wtr.Write(buf);
            }

        }
        #endregion


        #region Set Value

        public static void SetValue(string name, string value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.String, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.String, value));
            }
        }

        public static void SetValue(string name, byte value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Byte, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Byte, value));
            }
        }

        public static void SetValue(string name, sbyte value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.SByte, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.SByte, value));
            }
        }

        public static void SetValue(string name, bool value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Bool, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Bool, value));
            }
        }

        public static void SetValue(string name, ushort value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.UShort, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.UShort, value));
            }
        }

        public static void SetValue(string name, short value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Short, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Short, value));
            }
        }

        public static void SetValue(string name, uint value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.UInt, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.UInt, value));
            }
        }

        public static void SetValue(string name, int value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Int, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Int, value));
            }
        }

        public static void SetValue(string name, ulong value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.ULong, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.ULong, value));
            }
        }

        public static void SetValue(string name, long value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Long, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Long, value));
            }
        }

        public static void SetValue(string name, float value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Float, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Float, value));
            }
        }

        public static void SetValue(string name, double value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Double, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Double, value));
            }
        }

        public static void SetValue(string name, decimal value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Decimal, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Decimal, value));
            }
        }

        public static void SetValue(string name, byte[] value)
        {
            if (objects.ContainsKey(name))
            {
                objects[name] = new CacheObject(name, CacheObjectType.Data, value);
            }
            else
            {
                objects.Add(name, new CacheObject(name, CacheObjectType.Data, value));
            }
        }

        #endregion

        #region Get Value

        public static string GetString(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (string)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static byte GetByte(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (byte)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static sbyte GetSByte(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (sbyte)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static bool GetBool(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (bool)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static ushort GetUShort(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (ushort)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static short GetShort(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (short)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static uint GetUInt(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (uint)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static int GetInt(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (int)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static ulong GetULong(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (ulong)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static long GetLong(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (long)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static float GetFloat(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (float)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static double GetDouble(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (double)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static decimal GetDecimal(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (decimal)objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        public static byte[] GetData(string name)
        {
            if (objects.ContainsKey(name))
            {
                return (byte[])objects[name].Value;
            }
            else
            {
                throw new Exception("Value requested for a non-existant object!");
            }
        }

        #endregion


        /// <summary>
        /// Returns true if the specified object exists 
        /// in the cache.
        /// </summary>
        public static bool Contains(string name)
        {
            return objects.ContainsKey(name);
        }

    }
}
