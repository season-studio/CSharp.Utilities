using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GeneralKit
{
    public class JSONValue : IEquatable<JSONValue>
    {
        public struct ScanItemsArgument
        { 
            public JSONValue Item;
            public bool CancelSubItems;
        }
        public delegate bool ScanItemsCallback(ScanItemsArgument arg);

        public delegate bool ConvertRawObjectCallback(JSONValue jsonValue, out object rawValue, object customData);

        private dynamic value;

        public static readonly JSONValue Null = new JSONValue(null);

        #region  构造及构造时的转换
        private static readonly Dictionary<string, Func<string, dynamic>> ConstructorMap = new Dictionary<string, Func<string, dynamic>>()
        {
            { "string", ConvertString },
            { "number", ConvertNumber },
            { "boolean", ConvertBoolean },
            { "null", ConvertNull }
        };

        private static dynamic ConvertNull(string _value)
        {
            return null;
        }

        private static dynamic ConvertString(string _value)
        {
            return _value;
        }

        private static dynamic ConvertNumber(string _value)
        {
            decimal ret;

            _value = _value.Trim();
            if (_value.StartsWith("0x"))
            {
                int iVal;
                return int.TryParse(_value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out iVal) ? iVal : 0;
            }
            else
            {
                return decimal.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out ret) ? ret : 0M;
            }
        }

        private static dynamic ConvertBoolean(string _value)
        {
            return (0 == string.Compare(_value, "true", StringComparison.OrdinalIgnoreCase)) ? true : false;
        }

        private static dynamic ProbeValueFromString(string _value)
        {
            if (null != _value)
            {
                if ((_value.StartsWith("\"") && _value.EndsWith("\"")) || (_value.StartsWith("\'") && _value.EndsWith("\'")))
                {
                    return _value.Substring(1, _value.Length - 2);
                }
                else if (decimal.TryParse(_value, out decimal mVal))
                {
                    return mVal;
                }
                else
                {
                    return _value;
                }
            }
            else
            {
                return _value;
            }
        }

        public JSONValue(dynamic _value, string _type = null)
        {
            Set(_value, _type);
        }

        public JSONValue()
        {
            value = null;
        }

        public static JSONValue FromCommandLineArguments(string[] _args, params string[] _prefixs)
        {
            if (null == _args)
            {
                return Null;
            }
            else
            {
                JSONValue ret = new JSONValue();

                if ((_prefixs != null) && (_prefixs.Length > 0))
                {
                    Regex prefixRegex = new Regex($"^({string.Join("|", _prefixs)})");
                    foreach (string item in _args)
                    {
                        string[] compositions = item.Split('=');
                        Match match = prefixRegex.Match(compositions[0]);
                        if (match.Success)
                        {
                            ret.SetItem(compositions[0].Substring(match.Length), (compositions.Length > 1) ? ProbeValueFromString(compositions[1]) : (dynamic)true);
                        }
                    }
                }
                else
                {
                    foreach (string item in _args)
                    {
                        string[] compositions = item.Split('=');
                        ret.SetItem(compositions[0], (compositions.Length > 1) ? ProbeValueFromString(compositions[1]) : (dynamic)true);
                    }
                }

                return ret;
            }
        }
        #endregion

        /// <summary>
        /// get or set item value if this JSONValue is an object
        /// </summary>
        /// <param name="_key"></param>
        /// <returns></returns>
        public JSONValue this[string _key]
        {
            get
            {
                try
                {
                    JSONValue item;
                    return (value is Dictionary<string, JSONValue>)
                            ? (value.TryGetValue(_key, out item) ? item : Null)
                            : Null;
                }
                catch
                {
                    return Null;
                }
            }

            set
            {
                if (!(this.value is Dictionary<string, JSONValue>))
                {
                    this.value = new Dictionary<string, JSONValue>();
                }

                this.value[_key] = (value is JSONValue) ? value : new JSONValue(value);
            }
        }

        /// <summary>
        /// check if a key is contained in this JSONValue
        /// </summary>
        /// <param name="_key"></param>
        /// <returns></returns>
        public bool ContainsKey(string _key)
        {
            return (value is Dictionary<string, JSONValue>)
                    ? value.ContainsKey(_key)
                    : false;
        }

        /// <summary>
        /// get or set item value if this JSONValue is an array
        /// </summary>
        /// <param name="_index"></param>
        /// <returns></returns>
        public JSONValue this[int _index]
        {
            get
            {
                try
                {
                    return (value is List<JSONValue>) ? value[_index] : Null;
                }
                catch
                {
                    return Null;
                }
            }

            set
            {
                if (!(this.value is List<JSONValue>))
                {
                    this.value = new List<JSONValue>();
                }

                if (_index >= 0)
                {
                    while (_index >= this.value.Count)
                    {
                        this.value.Add(Null);
                    }
                    this.value[_index] = (value is JSONValue) ? value : new JSONValue(value);
                }
            }
        }

        /// <summary>
        /// get or the length of this JSONValue if it is an array
        /// </summary>
        public int Length
        {
            get
            {
                return (value is List<JSONValue>) ? value.Count : 0;
            }

            set
            {
                if (this.value is List<JSONValue>)
                {
                    if (value < this.value.Count)
                    {
                        if (value < 0)
                        {
                            value = 0;
                        }
                        this.value.RemoveRange(value, this.value.Count - value);
                    }
                    else
                    {
                        while (value >= this.value.Count)
                        {
                            this.value.Add(Null);
                        }
                    }
                }
            }
        }

        public bool IsEmpty()
        {
            return value == null;
        }

        public bool IsArray()
        {
            return value is List<JSONValue>;
        }

        public bool IsObject()
        {
            return value is Dictionary<string, JSONValue>;
        }

        public int ItemCount
        {
            get
            {
                return (value is Dictionary<string, JSONValue> dic) ? dic.Count : 0;
            }
        }

        public bool CheckValueType(Type type)
        {
            return (value != null) && (type == value.GetType());
        }

        public bool IsString()
        {
            return (null != value) && (value is string);
        }

        override public string ToString()
        {
            return null == value ? "null" : (string)this;
        }

        public object ToRawValue(ConvertRawObjectCallback _customer = null, object _customData = null)
        {
            if (null != _customer)
            {
                if (_customer(this, out object rawObj, _customData))
                {
                    return rawObj;
                }
            }

            if (null == value)
            {
                return null;
            }
            else if (value is List<JSONValue> srcArray)
            {
                object[] array = new object[srcArray.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = srcArray[i].ToRawValue(_customer, _customData);
                }
                return array;
            }
            else if (value is Dictionary<string, JSONValue> srcDict)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (var obj in srcDict)
                {
                    dict.Add(obj.Key, obj.Value.ToRawValue(_customer, _customData));
                }
                return dict;
            }
            else if (value is bool)
            {
                return (bool)value;
            }
            else if (value is int)
            {
                return (int)value;
            }
            else if (value is decimal)
            {
                string vStr = value.ToString();
                if (sbyte.TryParse(vStr, out sbyte i8v))
                {
                    return i8v;
                }
                else if (byte.TryParse(vStr, out byte u8v))
                {
                    return u8v;
                }
                else if (short.TryParse(vStr, out short i16v))
                {
                    return i16v;
                }
                else if (ushort.TryParse(vStr, out ushort u16v))
                {
                    return u16v;
                }
                else if (int.TryParse(vStr, out Int32 i32v))
                {
                    return i32v;
                }
                else if (uint.TryParse(vStr, out UInt32 u32v))
                {
                    return u32v;
                }
                else if (long.TryParse(vStr, out Int64 i64v))
                {
                    return i16v;
                }
                else if (ulong.TryParse(vStr, out UInt64 u64v))
                {
                    return u16v;
                }
                else if (float.TryParse(vStr, out float fv))
                {
                    return fv;
                }
                else if (double.TryParse(vStr, out double dv))
                {
                    return dv;
                }
                else
                {
                    return value;
                }
            }
            else
            {
                return value.ToString();
            }
        }

        public void Append(dynamic _value)
        {
            if (!(value is List<JSONValue>))
            {
                value = new List<JSONValue>();
            }
            value.Add(new JSONValue(_value));
        }

        public void SetItem(string _key, dynamic _value, string _type = null)
        {
            this[_key] = _value is JSONValue ? _value : new JSONValue(_value, _type);
        }

        public void SetItem(int _index, dynamic _value, string _type = null)
        {
            this[_index] = _value is JSONValue ? _value : new JSONValue(_value, _type);
        }

        public JSONValue Get(string _key, dynamic _default = null)
        {
            return ContainsKey(_key) ? this[_key] : (_default is JSONValue ? _default : new JSONValue(_default));
        }

        /// <summary>
        /// set the value of this JSONValue
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_type"></param>
        public void Set(dynamic _value, string _type = null)
        {
            if (null == _value)
            {
                value = null;
            }
            else if (_value is JSONValue)
            {
                value = _value.value;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_type))
                {
                    _type = "string";
                }

                if (0 == string.Compare(_type, "object", StringComparison.OrdinalIgnoreCase))
                {
                    value = new Dictionary<string, JSONValue>();
                }
                else if (0 == string.Compare(_type, "array", StringComparison.OrdinalIgnoreCase))
                {
                    value = new List<JSONValue>();
                }
                else
                {
                    if ((_value is int) || (_value is float) || (_value is double))
                    {
                        value = (decimal)_value;
                    }
                    else if ((_value is bool) || (_value is decimal))
                    {
                        value = _value;
                    }
                    else if (_value is Color)
                    {
                        value = (decimal)_value.ToArgb();
                    }
                    else
                    {
                        Func<string, dynamic> func = null;
                        value = ConstructorMap.TryGetValue(_type, out func)
                                    ? func(_value.ToString())
                                    : null;
                    }
                }
            }
        }

        /// <summary>
        /// init data from a XMLNode
        /// </summary>
        /// <param name="_xmlNode"></param>
        private void FromXML(XmlNode _xmlNode)
        {
            string type = _xmlNode.Attributes["type"].Value;
            if (type == "object")
            {
                Set(null, type);
                XmlNodeList list = _xmlNode.ChildNodes;
                int iCount = list.Count;
                for (int index = 0; index < iCount; index++)
                {
                    XmlNode node = list[index];
                    string prefix = node.Prefix;
                    string name = node.LocalName;
                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        name = node.Attributes[name].Value;
                    }
                    JSONValue itemValue = new JSONValue();
                    itemValue.FromXML(node);
                    this[name] = itemValue;
                }
            }
            else if (type == "array")
            {
                Set(null, type);
                XmlNodeList list = _xmlNode.ChildNodes;
                int iCount = list.Count;
                for (int index = 0; index < iCount; index++)
                {
                    XmlNode node = list[index];
                    JSONValue itemValue = new JSONValue();
                    itemValue.FromXML(node);
                    this[index] = itemValue;
                }
            }
            else
            {
                Set(_xmlNode.InnerText, type);
            }
        }

        /// <summary>
        /// init data from a JSON string
        /// </summary>
        /// <param name="_szJson"></param>
        public void Parse(string _szJson)
        {
            XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(_szJson), XmlDictionaryReaderQuotas.Max);
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            this.FromXML(doc.DocumentElement);
        }

        /// <summary>
        /// Create a JSONValue from a JSON string
        /// </summary>
        /// <param name="_szJson"></param>
        /// <returns></returns>
        public static JSONValue FromString(string _szJson)
        {
            JSONValue ret = new JSONValue();
            ret.Parse(_szJson);
            return ret;
        }

        public static bool TryFromString(string _szJson, out JSONValue _json)
        {
            try
            {
                _json = FromString(_szJson);
                return true;
            }
            catch (Exception err)
            {
                System.Diagnostics.Trace.WriteLine(err.ToString());
                _json = null;
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as JSONValue);
        }

        public bool Equals(JSONValue other)
        {
            return other != null &&
                   EqualityComparer<dynamic>.Default.Equals(value, other.value);
        }

        public override int GetHashCode()
        {
            return (null == value) ? 0 : value.GetHashCode();
        }

        public IEnumerable<KeyValuePair<string, JSONValue>> GetChildren()
        {
            if (value is Dictionary<string, JSONValue>)
            {
                foreach (KeyValuePair<string, JSONValue> item in value)
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<JSONValue> GetArrayItems()
        {
            if (value is List<JSONValue>)
            {
                foreach (JSONValue item in value)
                {
                    yield return item;
                }
            }
        }

        public JSONValue Assign(JSONValue other, bool combineArray = false)
        {
            if ((value is Dictionary<string, JSONValue> targetDic) && (other.value is Dictionary<string, JSONValue> srcDic))
            {
                foreach (KeyValuePair<string, JSONValue> item in srcDic)
                {
                    JSONValue target = this[item.Key];
                    if (target == Null)
                    {
                        this[item.Key] = item.Value;
                    }
                    else
                    {
                        target.Assign(item.Value, combineArray);
                    }
                }
            }
            else if ((value is List<JSONValue> targetArray) && (other.value is List<JSONValue> srcArray) && combineArray)
            {
                foreach (JSONValue item in srcArray)
                {
                    targetArray.Add(item);
                }
            }
            else
            {
                value = other.value;
            }

            return this;
        }

        public void ScanItems(ScanItemsCallback _cb)
        {
            if (null != _cb)
            {
                ScanItemsArgument arg = new ScanItemsArgument()
                {
                    Item = this,
                    CancelSubItems = false
                };
                _cb(arg);
                if (!arg.CancelSubItems)
                {
                    if (value is Dictionary<string, JSONValue> dic)
                    {
                        foreach (KeyValuePair<string, JSONValue> item in dic)
                        {
                            item.Value.ScanItems(_cb);
                        }
                    }
                    else if (value is List<JSONValue> array)
                    {
                        foreach (JSONValue item in array)
                        {
                            item.ScanItems(_cb);
                        }
                    }
                }
            }
        }

        #region 数据类型转换自适应
        public static bool operator == (JSONValue _value1, JSONValue _value2)
        {
            dynamic val1 = (_value1 is JSONValue) ? _value1.value : null;
            dynamic val2 = (_value2 is JSONValue) ? _value2.value : null;
            return EqualityComparer<dynamic>.Default.Equals(val1, val2);
        }

        public static bool operator !=(JSONValue _value1, JSONValue _value2)
        {
            dynamic val1 = (null == _value1) ? null : _value1.value;
            dynamic val2 = (null == _value2) ? null : _value2.value;
            return !EqualityComparer<dynamic>.Default.Equals(val1, val2);
        }

        /// <summary>
        /// convert to int
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator int(JSONValue _value)
        {
            if ((null == _value) || (null == _value.value))
            {
                return 0;
            }

            dynamic val = _value.value;
            int ret;

            if (val is decimal)
            {
                ret = (int)val;
            }
            else if (val is bool)
            {
                ret = val ? 1 : 0;
            }
            else
            {
                string sVal = (val is string) ? val.Trim() : val.ToString().Trim();
                if (sVal.StartsWith("0x"))
                {
                    if (!int.TryParse(sVal.Substring(2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out ret))
                    {
                        ret = 0;
                    }
                }
                else if (!int.TryParse(sVal, out ret))
                {
                    ret = 0;
                }
            }

            return ret;
        }

        /// <summary>
        /// convert to float
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator float(JSONValue _value)
        {
            if ((null == _value) || (null == _value.value))
            {
                return 0;
            }

            dynamic val = _value.value;
            float ret;

            if (val is decimal)
            {
                ret = (float)val;
            }
            else if (val is bool)
            {
                ret = val ? 1 : 0;
            }
            else
            {
                string sVal = (val is string) ? val : val.ToString();
                if (!float.TryParse(sVal, NumberStyles.Float, CultureInfo.CurrentCulture, out ret))
                {
                    ret = 0;
                }
            }

            return ret;
        }

        /// <summary>
        /// convert to double
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator double(JSONValue _value)
        {
            if ((null == _value) || (null == _value.value))
            {
                return 0;
            }

            dynamic val = _value.value;
            double ret;

            if (val is decimal)
            {
                ret = (double)val;
            }
            else if (val is bool)
            {
                ret = val ? 1 : 0;
            }
            else
            {
                string sVal = (val is string) ? val : val.ToString();
                if (!double.TryParse(sVal, NumberStyles.Float, CultureInfo.CurrentCulture, out ret))
                {
                    ret = 0;
                }
            }

            return ret;
        }

        /// <summary>
        /// convert to decimal
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator decimal(JSONValue _value)
        {
            if ((null == _value) || (null == _value.value))
            {
                return 0;
            }

            dynamic val = _value.value;
            decimal ret;

            if (val is decimal)
            {
                ret = val;
            }
            else if (val is bool)
            {
                ret = val ? 1 : 0;
            }
            else
            {
                string sVal = (val is string) ? val : val.ToString();
                if (!decimal.TryParse(sVal, NumberStyles.Float, CultureInfo.CurrentCulture, out ret))
                {
                    ret = 0;
                }
            }

            return ret;
        }

        /// <summary>
        /// convert to string
        /// object and array will be converted to JSON string
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator string(JSONValue _value)
        {
            if ((null == _value) || (null == _value.value))
            {
                return null;
            }

            string ret;

            if (_value.value is Dictionary<string, JSONValue>)
            {
                StringBuilder strBuilder = new StringBuilder();
                bool isContinue = false;

                strBuilder.Append("{");
                foreach (KeyValuePair<string, JSONValue> item in _value.value)
                {
                    if (isContinue)
                    {
                        strBuilder.Append(",\n");
                    }
                    else
                    {
                        isContinue = true;
                    }
                    strBuilder.Append(string.Format("\"{0}\":", item.Key));
                    if (item.Value.IsString())
                    {
                        strBuilder.Append(string.Format("\"{0}\"", item.Value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")));
                    }
                    else if (item.Value.value is bool boolVal)
                    {
                        strBuilder.Append(boolVal ? "true" : "false");
                    }
                    else
                    {
                        strBuilder.Append(item.Value.ToString());
                    }
                }
                strBuilder.Append("}");
                ret = strBuilder.ToString();
            }
            else if (_value.value is List<JSONValue>)
            {
                StringBuilder strBuilder = new StringBuilder();
                bool isContinue = false;

                strBuilder.Append("[");
                foreach (JSONValue item in _value.value)
                {
                    if (isContinue)
                    {
                        strBuilder.Append(",\n");
                    }
                    else
                    {
                        isContinue = true;
                    }
                    if (item.IsString())
                    {
                        strBuilder.Append(string.Format("\"{0}\"", item.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")));
                    }
                    else
                    {
                        strBuilder.Append(item.ToString());
                    }
                }
                strBuilder.Append("]");
                ret = strBuilder.ToString();
            }
            else if (_value.value.GetType().Equals(typeof(bool)))
            {
                ret = ((bool)_value) ? "true" : "false";
            }
            else
            {
                ret = _value.value.ToString();
            }

            return ret;
        }

        /// <summary>
        /// convert to bool
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator bool(JSONValue _value)
        {
            if ((null == _value) || (null == _value.value))
            {
                return false;
            }

            dynamic val = _value.value;
            bool ret;

            if (val is bool)
            {
                ret = val;
            }
            else if (val is decimal)
            {
                ret = (val != 0);
            }
            else
            {
                string sVal = (val is string) ? val : val.ToString();
                if (!bool.TryParse(sVal, out ret))
                {
                    ret = false;
                }
            }

            return ret;
        }

        /// <summary>
        /// convert to Color
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator Color(JSONValue _value)
        {
            return Color.FromArgb(255, Color.FromArgb((int)_value));
        }
        #endregion
    }
}
