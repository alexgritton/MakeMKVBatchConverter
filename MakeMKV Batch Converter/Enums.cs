using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MakeMKV_Batch_Converter
{
    public enum FileType
    {
        [StringValue(".iso")]
        Iso ,
        [StringValue(".m2ts")]
        BluRay,
        [StringValue(".ifo")]
        VideoTs
    }

    public class StringValue : Attribute
    {
        private string _value;

        public StringValue(string value)
        {
            _value = value;
        }

        public string Value
        {
            get { return _value; }
        }

    }

    public static class StringEnum
    {
        public static FileType GetFileType(string str)
        {
            switch (str.ToLower())
            {
                case ".iso":
                    return FileType.Iso;
                case ".m2ts":
                    return FileType.BluRay;
                case ".ifo":
                    return FileType.VideoTs;
            }
            return FileType.Iso;
        }
        public static string GetStringValue(Enum value)
        {
            string output = null;
            Type type = value.GetType();

            FieldInfo fi = type.GetField(value.ToString());
            StringValue[] attrs =
               fi.GetCustomAttributes(typeof(StringValue),
                                       false) as StringValue[];
            if (attrs != null && attrs.Length > 0)
            {
                output = attrs[0].Value;
            }

            return output;
        }
    }
}
