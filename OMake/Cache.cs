using System;
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
        private enum CacheObjectType
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
            public ulong Size;
            public CacheObjectType Type;
            public object Value;

            public CacheObject(string name, CacheObjectType type, object value)
            {
                this.Name = name;
                this.Type = type;
                switch (type)
                {
                    case CacheObjectType.String:
                        byte[] buf = System.Text.ASCIIEncoding.UTF32.GetBytes((string)value);
                        this.Value = buf;
                        this.Size = (ulong)buf.LongLength;
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
                        this.Value = (byte)(((bool)value) ? 127 : 0);
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
                        this.Size = (ulong)((byte[])value).LongLength;
                        break;
                    default:
                        throw new Exception("Unknown cache object type!");
                }
            }
        }
        #endregion

        /// <summary>
        /// This is the byte sequence for "OMCC" in ASCII.
        /// </summary>
        private const uint CacheHeader = (byte)'O' | ((byte)'M') << 8 | ((byte)'C') << 16 | ((byte)'C') << 24;
        private static Dictionary<string, CacheObject> objects = new Dictionary<string, CacheObject>();

        public static void Initialize()
        {
            if (File.Exists(Processor.file.Filename + ".cache"))
            {
                LoadCache(new BinaryReader(new FileStream(Processor.file.Filename + ".cache", FileMode.Open), System.Text.ASCIIEncoding.UTF32));
            }
        }

        private static void LoadCache(BinaryReader rdr)
        {
            if (rdr.ReadUInt32() != CacheHeader)
            {
                ErrorManager.Warning(89, Processor.file, Processor.file.Filename + ".cache");
                return;
            }
            ulong objectCount = rdr.ReadUInt64();
            CacheObject c = new CacheObject();

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
    }
}
