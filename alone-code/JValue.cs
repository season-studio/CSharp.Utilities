using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;

namespace SeasonStudio.Common
{
    using JObject = Dictionary<string, JValue>;
    using JArray = List<JValue>;

    public struct JValue : IEquatable<JValue>
    {
        private dynamic value;

        #region  构造
        public JValue(dynamic _value)
        {
            value = null;
            Set(_value);
        }
        #endregion

        #region 基本访问
        public void Set(dynamic _value)
        {
            Type type = _value?.GetType();
            if (null == type)
            {
                value = null;
            }
            else if (_value is JValue)
            {
                value = _value.value;
            }
            else if (_value is byte[])
            {
                value = _value;
            }
            else if ((_value is IDictionary) || ((_value is IEnumerable) && (null != type.GetMethod("ContainsKey"))))
            {
                // 作为对象初始化
                value = new JObject();
                foreach (dynamic item in _value)
                {
                    value[null != item.Key ? item.Key.ToString() : "null"] = new JValue(item.Value);
                }
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.KeyValuePair`"))
            {
                // 作为对象初始化
                value = new JObject();
                if (_value is IEnumerable)
                {
                    foreach (dynamic item in _value)
                    {
                        value[null != item.Key ? item.Key.ToString() : "null"] = new JValue(item.Value);
                    }
                }
                else
                {
                    value[null != _value.Key ? _value.Key.ToString() : "null"] = new JValue(_value.Value);
                }
            }
            else if (_value is string)
            {
                value = _value;
            }
            else if (_value is IEnumerable)
            {
                // 作为数组
                value = new JArray();
                foreach (dynamic item in _value)
                {
                    value.Add(new JValue(item));
                }
            }
            else if ((_value is bool) || (_value is decimal))
            {
                value = _value;
            }
            else if (type.IsPrimitive)
            {
                value = (decimal)_value;
            }
            else if (_value is Color)
            {
                value = (decimal)_value.ToArgb();
            }
            else
            {
                value = _value.ToString();
            }
        }

        public dynamic this[string _key]
        {
            get
            {
                try
                {
                    JValue item;
                    return (value is JObject)
                            ? (value.TryGetValue(_key, out item) ? item : Null())
                            : Null();
                }
                catch
                {
                    return Null();
                }
            }

            set
            {
                if (!(this.value is JObject))
                {
                    this.value = new JObject();
                }

                this.value[_key] = (value is JValue) ? value : new JValue(value);
            }
        }

        public dynamic this[int _index]
        {
            get
            {
                try
                {
                    return (value is JArray) ? value[_index] : this[_index.ToString()];
                }
                catch
                {
                    return Null();
                }
            }

            set
            {
                if (this.value is JObject)
                {
                    value[_index.ToString()] = value;
                }
                else
                {
                    if (!(this.value is JArray))
                    {
                        this.value = new JArray();
                    }

                    if (_index >= 0)
                    {
                        while (_index >= this.value.Count)
                        {
                            this.value.Add(Null());
                        }
                        this.value[_index] = (value is JValue) ? value : new JValue(value);
                    }
                }
            }
        }

        /// <summary>
        /// Get the count of the items in the value
        /// </summary>
        public int Count
        {
            get
            {
                return (value is ICollection) ? value.Count : 0;
            }
        }

        /// <summary>
        /// Get or set the length in the value
        /// </summary>
        public int Length
        {
            get
            {
                return Count;
            }

            set
            {
                if (this.value is JArray)
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
                            this.value.Add(Null());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the type of the raw value
        /// </summary>
        public Type Type
        {
            get
            {
                return (null == value) ? typeof(void) : value.GetType();
            }
        }

        public bool IsNull
        {
            get
            {
                return value == null;
            }
        }

        public bool IsArray
        {
            get
            {
                return value is JArray;
            }
        }

        public bool IsObject
        {
            get
            {
                return (value is JObject);
            }
        }

        public bool IsString
        {
            get
            {
                return (null != value) && (value is string);
            }
        }

        public bool IsNumber
        {
            get
            {
                return (value is decimal) || ((null != value) && !(value is bool) && value.GetType().IsPrimitive);
            }
        }

        public bool IsBuffer
        {
            get
            {
                return value is byte[];
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (null == value)
                        || ((value is string) && string.IsNullOrEmpty(value))
                        || ((value is ICollection) && (0 == value.Count));
            }
        }

        public static JValue Null()
        {
            return new JValue() { value = null };
        }

        public static JValue Object(dynamic _src = null)
        {
            JObject obj = new JObject();
            if (null != _src)
            {
                if ((_src is IDictionary)
                    || ((_src is IEnumerable) && (null != _src.GetType().GetMethod("ContainsKey"))))
                {
                    foreach (dynamic item in _src)
                    {
                        obj[null != item.Key ? item.Key.ToString() : "null"] = new JValue(item.Value);
                    }
                }
                else if (_src.GetType().FullName.StartsWith("System.Collections.Generic.KeyValuePair`"))
                {
                    if (_src is IEnumerable)
                    {
                        foreach (dynamic item in _src)
                        {
                            obj[null != item.Key ? item.Key.ToString() : "null"] = new JValue(item.Value);
                        }
                    }
                    else
                    {
                        obj[null != _src.Key ? _src.Key.ToString() : "null"] = new JValue(_src.Value);
                    }
                }
            }
            return new JValue() { value = new JObject() };
        }

        public static JValue Array(IEnumerable _src = null)
        {
            JArray list = new JArray();
            if (null != _src)
            {
                foreach (dynamic item in _src)
                {
                    list.Add(new JValue(item));
                }
            }
            return new JValue() { value = list };
        }
        #endregion

        #region 数组方法
        public JValue AsArray()
        {
            if (!(value is JArray))
            {
                value = new JArray();
            }
            return this;
        }

        public void Push(dynamic _value, bool _force = false)
        {
            if (!(value is JArray) && _force)
            {
                value = new JArray();
            }
            if (value is JArray)
            {
                value.Add(new JValue(_value));
            }
        }

        public JValue Pop()
        {
            JValue ret = Null();

            if (value is JArray list)
            {
                if (list.Count > 0)
                {
                    int index = list.Count - 1;
                    ret = list[index];
                    list.RemoveAt(index);
                }
            }

            return ret;
        }

        public void Unshift(dynamic _value, bool _force = false)
        {
            if (!(value is JArray) && _force)
            {
                value = new JArray();
            }
            if (value is JArray)
            {
                value.Insert(0, new JValue(_value));
            }
        }

        public JValue Shift()
        {
            JValue ret = Null();

            if (value is JArray list)
            {
                if (list.Count > 0)
                {
                    ret = list[0];
                    list.RemoveAt(0);
                }
            }

            return ret;
        }

        public JValue? FirstItem()
        {
            if ((value is JArray list) && (list.Count > 0))
            {
                return list.First();
            }
            else
            {
                return null;
            }
        }

        public JValue? LastItem()
        {
            if ((value is JArray list) && (list.Count > 0))
            {
                return list.Last();
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 对象方法
        public JValue AsObject()
        {
            if (!(value is JObject))
            {
                value = new JObject();
            }
            return this;
        }

        /// <summary>
        /// Check if the value is an object and contains a special key
        /// </summary>
        /// <param name="_key"></param>
        /// <returns></returns>
        public bool ContainsKey(string _key)
        {
            return (value is JObject)
                    ? value.ContainsKey(_key)
                    : false;
        }

        /// <summary>
        /// Delete a child in the object value
        /// </summary>
        /// <param name="_key"></param>
        public void Delete(string _key)
        {
            if (value is JObject dict)
            {
                if (dict.ContainsKey(_key))
                {
                    dict.Remove(_key);
                }
            }
        }

        public KeyValuePair<string, JValue>? FirstChild()
        {
            if ((value is JObject dict) && (dict.Count > 0))
            {
                return dict.First();
            }
            else
            {
                return null;
            }
        }

        public KeyValuePair<string, JValue>? LastChild()
        {
            if ((value is JObject dict) && (dict.Count > 0))
            {
                return dict.Last();
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 枚举类方法
        /// <summary>
        /// Get an enumerator for the children of the object value
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, JValue>> GetChildren()
        {
            if (value is JObject)
            {
                foreach (KeyValuePair<string, JValue> item in value)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Get an enumerator for the items of the array value
        /// </summary>
        /// <returns></returns>
        public IEnumerable<JValue> GetArrayItems()
        {
            if (value is JArray)
            {
                foreach (JValue item in value)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Get an enumerator for the items of the value
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<dynamic, JValue>> GetItems()
        {
            if (value is JObject)
            {
                foreach (KeyValuePair<string, JValue> item in value)
                {
                    yield return new KeyValuePair<dynamic, JValue>(item.Key, item.Value);
                }
            }
            else if (value is JArray)
            {
                int index = 0;
                foreach (JValue item in value)
                {
                    yield return new KeyValuePair<dynamic, JValue>(index++, item);
                }
            }
            else
            {
                yield return new KeyValuePair<dynamic, JValue>(null, this);
            }
        }
        #endregion

        #region 对比类方法
        public override bool Equals(object _obj)
        {
            return EqualityComparer<dynamic>.Default.Equals(value, (_obj is JValue) ? ((JValue)_obj).value : _obj);
        }

        public bool Equals(JValue _other)
        {
            return EqualityComparer<dynamic>.Default.Equals(value, _other.value);
        }

        public override int GetHashCode()
        {
            return (null == value) ? 0 : value.GetHashCode();
        }

        public static bool operator ==(JValue? _value1, JValue? _value2)
        {
            if (_value1.HasValue)
            {
                return _value1.Value.Equals(_value2);
            }
            else if (_value2.HasValue)
            {
                return _value2.Value.Equals(_value1);
            }
            else
            {
                return true;
            }
        }

        public static bool operator !=(JValue? _value1, JValue? _value2)
        {
            if (_value1.HasValue)
            {
                return !_value1.Value.Equals(_value2);
            }
            else if (_value2.HasValue)
            {
                return !_value2.Value.Equals(_value1);
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region 转换类方法
        public dynamic ToRawValue<T, TRef>(Func<JValue, TRef, ValueTuple<bool, T>> _fn = null, TRef _customData = default)
        {
            if (null != _fn)
            {
                ValueTuple<bool, T> fnRet = _fn(this, _customData);
                if (fnRet.Item1)
                {
                    return fnRet.Item2;
                }
            }

            if (null == value)
            {
                return null;
            }
            else if (value is JArray srcArray)
            {
                object[] array = new object[srcArray.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = srcArray[i].ToRawValue(_fn, _customData);
                }
                return array;
            }
            else if (value is JObject srcDict)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (var obj in srcDict)
                {
                    dict.Add(obj.Key, obj.Value.ToRawValue(_fn, _customData));
                }
                return dict;
            }
            else if (value is byte[])
            {
                return (byte[])value;
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

        public static implicit operator byte[](JValue _value)
        {
            return (_value.value is byte[]) ? (byte[])_value.value : null;
        }

        /// <summary>
        /// convert to int
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator int(JValue _value)
        {
            if (null == _value.value)
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
        public static implicit operator float(JValue _value)
        {
            if (null == _value.value)
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
        public static implicit operator double(JValue _value)
        {
            if (null == _value.value)
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
        public static implicit operator decimal(JValue _value)
        {
            if (null == _value.value)
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
        public static implicit operator string(JValue _value)
        {
            return (null == _value.value)
                        ? null
                        : ((_value.value is string) ? _value.value : _value.ToString(string.Empty, string.Empty, string.Empty));
        }

        /// <summary>
        /// convert to bool with converting the string value in strict mode
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator bool(JValue _value)
        {
            return _value.ToBoolean(true);
        }

        /// <summary>
        /// convert to bool
        /// </summary>
        /// <param name="_strictString"></param>
        /// <returns></returns>
        public bool ToBoolean(bool _strictString = false)
        {
            bool ret;

            if (null == value)
            {
                ret = false;
            }
            else if (value is bool)
            {
                ret = value;
            }
            else if (value is decimal)
            {
                ret = (value != 0);
            }
            else
            {
                string sVal = (value is string) ? value : value.ToString();
                if (_strictString)
                {
                    if (!bool.TryParse(sVal, out ret))
                    {
                        ret = false;
                    }
                }
                else
                {
                    ret = string.IsNullOrEmpty(sVal);
                }
            }

            return ret;
        }

        /// <summary>
        /// convert to Color
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator Color(JValue _value)
        {
            return Color.FromArgb(255, Color.FromArgb((int)_value));
        }

        override public string ToString()
        {
            return ToString(string.Empty, string.Empty, string.Empty);
        }

        public string ToString(string _space)
        {
            return ToString(string.Empty, (null == _space) ? string.Empty : _space, string.IsNullOrEmpty(_space) ? string.Empty : "\n");
        }

        private string ToString(string _prefix, string _space, string _breakLine)
        {
            string ret;

            if (null == value)
            {
                ret = "null";
            }
            else
            {
                string nextPrefix = _prefix + _space;

                if (value is JObject)
                {
                    if (value.Count > 0)
                    {
                        string midSpec = (string.IsNullOrEmpty(_space) ? ":" : ": ");
                        List<string> list = new List<string>();
                        foreach (KeyValuePair<string, JValue> item in value)
                        {
                            list.Add($"\"{item.Key}\"{midSpec}{item.Value.ToString(nextPrefix, _space, _breakLine)}");
                        }
                        ret = $"{{{_breakLine}{nextPrefix}{string.Join($",{_breakLine}{nextPrefix}", list as IEnumerable<string>)}{_breakLine}{_prefix}}}";
                    }
                    else
                    {
                        ret = "{}";
                    }
                }
                else if (value is JArray)
                {
                    if (value.Count > 0)
                    {
                        List<string> list = new List<string>();
                        foreach (JValue item in value)
                        {
                            list.Add(item.ToString(nextPrefix, _space, _breakLine));
                        }
                        ret = $"[{_breakLine}{nextPrefix}{string.Join($",{_breakLine}{nextPrefix}", list as IEnumerable<string>)}{_breakLine}{_prefix}]";
                    }
                    else
                    {
                        ret = "[]";
                    }
                }
                else if (value is bool)
                {
                    ret = ((bool)value) ? "true" : "false";
                }
                else if (value is string)
                {
                    ret = "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                }
                else
                {
                    ret = value.ToString();
                }
            }

            return ret;
        }

        public static implicit operator JValue(decimal value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(string value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(bool value)
        {
            return new JValue(value);
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
                JObject dict = new JObject();
                foreach (XmlNode node in _xmlNode.ChildNodes)
                {
                    string prefix = node.Prefix;
                    string name = node.LocalName;
                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        name = node.Attributes[name].Value;
                    }
                    JValue itemValue = new JValue();
                    itemValue.FromXML(node);
                    dict.Add(name, itemValue);
                }
                Set(dict);
            }
            else if (type == "array")
            {
                JArray list = new JArray();
                foreach (XmlNode node in _xmlNode.ChildNodes)
                {
                    JValue itemValue = new JValue();
                    itemValue.FromXML(node);
                    list.Add(itemValue);
                }
                Set(list);
            }
            else if (type == "boolean")
            {
                Set(0 == string.Compare(_xmlNode.InnerText, "true", StringComparison.OrdinalIgnoreCase));
            }
            else if (type == "number")
            {
                string valueText = _xmlNode.InnerText.Trim();
                decimal ret = 0M;
                if (valueText.StartsWith("0x"))
                {
                    decimal.TryParse(valueText[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ret);
                }
                else
                {
                    decimal.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out ret);
                }
                value = ret;
            }
            else if (type == "null")
            {
                value = null;
            }
            else
            {
                value = _xmlNode.InnerText;
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
            FromXML(doc.DocumentElement);
        }

        /// <summary>
        /// Create a JValue from a JSON string
        /// </summary>
        /// <param name="_szJson"></param>
        /// <returns></returns>
        public static JValue FromString(string _szJson)
        {
            JValue ret = new JValue();
            ret.Parse(_szJson);
            return ret;
        }

        /// <summary>
        /// Create a JValue from a JSON string
        /// </summary>
        /// <param name="_szJson"></param>
        /// <param name="_json"></param>
        /// <returns></returns>
        public static bool TryFromString(string _szJson, out JValue _json)
        {
            try
            {
                _json = FromString(_szJson);
                return true;
            }
            catch (Exception err)
            {
                System.Diagnostics.Trace.WriteLine(err.ToString());
                _json = Null();
                return false;
            }
        }

        /// <summary>
        /// Probe value from string
        /// </summary>
        /// <param name="_value"></param>
        /// <returns></returns>
        private static dynamic ProbeValueFromString(string _value)
        {
            if (null != _value)
            {
                if ((_value.StartsWith("\"") && _value.EndsWith("\"")) || (_value.StartsWith("\'") && _value.EndsWith("\'")))
                {
                    return _value.Substring(1, _value.Length - 2);
                }
                else if (_value.StartsWith("0x") && decimal.TryParse(_value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out decimal mHex))
                {
                    return mHex;
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
        #endregion

        #region 其他运算符重载
        public static JValue operator +(JValue _value1, string _value2)
        {
            if ((null != _value2) && (_value1.value is string))
            {
                return new JValue((string)(_value1.value) + _value2);
            }
            else
            {
                return _value1;
            }
        }

        public static JValue operator +(JValue _value1, decimal _value2)
        {
            if (_value1.IsNumber)
            {
                return new JValue((decimal)(_value1.value) + _value2);
            }
            else
            {
                return _value1;
            }
        }

        public static JValue operator -(JValue _value1, decimal _value2)
        {
            if (!_value1.IsNumber)
                {
                return _value1;
            }
            else
            {
                return new JValue((decimal)_value1 - _value2);
            }
        }

        public static JValue operator *(JValue _value1, decimal _value2)
        {
            if (!_value1.IsNumber)
            {
                return _value1;
            }
            else
            {
                return new JValue((decimal)_value1 * _value2);
            }
        }

        public static JValue operator /(JValue _value1, decimal _value2)
        {
            if (!_value1.IsNumber)
            {
                return _value1;
            }
            else
            {
                return new JValue((decimal)_value1 / _value2);
            }
        }

        public static JValue operator %(JValue _value1, decimal _value2)
        {
            if (!_value1.IsNumber)
            {
                return _value1;
            }
            else
            {
                return new JValue((decimal)_value1 % _value2);
            }
        }

        public static JValue operator ++(JValue _value1)
        {
            if (!_value1.IsNumber)
            {
                return _value1;
            }
            else
            {
                return new JValue((decimal)_value1 + 1);
            }
        }

        public static JValue operator --(JValue _value1)
        {
            if (!_value1.IsNumber)
            {
                return _value1;
            }
            else
            {
                return new JValue((decimal)_value1 - 1);
            }
        }
        #endregion

        #region 杂项方法
        /// <summary>
        /// Clone the current value as a new one
        /// </summary>
        /// <param name="_deep"></param>
        /// <returns></returns>
        public JValue Clone(bool _deep = false)
        {
            JValue ret = new JValue();
            if (!_deep)
            {
                ret.Set(value);
            }
            else if (value is JObject dict)
            {
                JObject newDict = new JObject();
                foreach (KeyValuePair<string, JValue> item in dict)
                {
                    newDict[item.Key] = item.Value.Clone(true);
                }
                ret.value = newDict;
            }
            else if (value is JArray list)
            {
                JArray newList = new JArray();
                foreach (JValue item in list)
                {
                    newList.Add(item.Clone(true));
                }
                ret.value = newList;
            }
            else
            {
                ret.Set(value);
            }

            return ret;
        }

        public void Assign(JValue _other)
        {
        if (_other.value is JObject srcDict)
            {
                AsObject();
                foreach (KeyValuePair<string, JValue> item in srcDict)
                {
                    JValue oriItem = this[item.Key];
                    if (null == oriItem)
                    {
                        value[item.Key] = item.Value;
                    }
                    else
                    {
                        oriItem.Assign(item.Value);
                    }
                }
            }
            else if (_other.value is JArray srcList)
            {
                if (!IsObject)
                {
                    AsArray();
                }
                int index = 0;
                foreach (JValue item in srcList)
                {
                    JValue oriItem = this[index];
                    if (null == oriItem)
                    {
                        value[index] = item;
                    }
                    else
                    {
                        oriItem.Assign(item);
                    }
                    index++;
                }
            }
            else
            {
                value = _other.value;
            }
        }

        /// <summary>
        /// Convert command line arguments to a object JValue
        /// </summary>
        /// <param name="_args"></param>
        /// <param name="_prefixs"></param>
        /// <returns></returns>
        public static JValue FromCommandLineArguments(string[] _args, params string[] _prefixs)
        {
            if (null == _args)
            {
                return Null();
            }
            else
            {
                JValue ret = new JValue();

                if ((_prefixs != null) && (_prefixs.Length > 0))
                {
                    Regex prefixRegex = new Regex($"^({string.Join("|", _prefixs)})");
                    foreach (string item in _args)
                    {
                        string[] compositions = item.Split('=');
                        Match match = prefixRegex.Match(compositions[0]);
                        if (match.Success)
                        {
                            ret[compositions[0].Substring(match.Length)] = ((compositions.Length > 1) ? ProbeValueFromString(compositions[1]) : (dynamic)true);
                        }
                    }
                }
                else
                {
                    foreach (string item in _args)
                    {
                        string[] compositions = item.Split('=');
                        ret[compositions[0]] = ((compositions.Length > 1) ? ProbeValueFromString(compositions[1]) : (dynamic)true);
                    }
                }

                return ret;
            }
        }
        #endregion
    }
}