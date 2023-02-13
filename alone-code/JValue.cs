using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace SeasonStudio.Common
{
    using JArray = List<JValue>;
    using JObject = Dictionary<string, JValue>;

#if NET6_0_OR_GREATER
#nullable disable
#endif

    /// <summary>
    /// JSON数据封装
    /// </summary>
    public struct JValue : IEquatable<JValue>
    {
        private object value;

        #region  构造
        public JValue(object _value)
        {
            value = null;
            SetValue(_value, true);
        }
        #endregion

        #region 基本访问
        /// <summary>
        /// 设置为兼容目标值
        /// </summary>
        /// <param name="_value"></param>
        public void Set(object _value)
        {
            SetValue(_value, true);
        }

        /// <summary>
        /// 设置为兼容目标值
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_switchArrayAndObject">true表示允许从Object切换到Array</param>
        private void SetValue(object _value, bool _switchArrayAndObject)
        {
            if (null == _value)
            {
                value = null;
            }
            else
            {
                Type type = _value.GetType();
                if (_value is JValue jVal)
                {
                    value = jVal.value;
                }
                else if (_value is byte[])
                {
                    value = _value;
                }
                else if (_value is string)
                {
                    value = _value;
                }
                else if (_value is IDictionary idict)
                {
                    var obj = new JObject();
                    foreach (DictionaryEntry item in idict)
                    {
                        obj[Convert.ToString(item.Key) ?? "null"] = new JValue(item.Value);
                    }
                    value = obj;
                }
                else if (_value is IEnumerable enumValues)
                {
                    var obj = new JObject();
                    bool isObj = true;
                    foreach (object item in enumValues)
                    {
                        if (item is ITuple tuple)
                        {
                            if (tuple.Length == 2)
                            {
                                obj[Convert.ToString(tuple[0]) ?? "null"] = new JValue(tuple[1]);
                            }
                            else if (_switchArrayAndObject)
                            {
                                isObj = false;
                                break;
                            }
                        }
                        else if (item is DictionaryEntry dictEntry)
                        {
                            obj[Convert.ToString(dictEntry.Key) ?? "null"] = new JValue(dictEntry.Value);
                        }
                        else if ((null != item) && item.GetType().FullName.StartsWith("System.Collections.Generic.KeyValuePair`"))
                        {
                            obj[Convert.ToString(((dynamic)item).Key) ?? "null"] = new JValue(((dynamic)item).Value);
                        }
                        else if (_switchArrayAndObject)
                        {
                            isObj = false;
                            break;
                        }
                    }
                    if (isObj)
                    {
                        value = obj;
                    }
                    else
                    {
                        value = Array(enumValues).value;
                    }
                }
                else if (_value is ITuple tuple)
                {
                    var list = new JArray();
                    int count = tuple.Length;
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(new JValue(tuple[i]));
                    }
                    value = list;
                }
                else if (_value is DictionaryEntry dictEntry)
                {
                    var obj = new JObject
                    {
                        { Convert.ToString(dictEntry.Key) ?? "null", new JValue(dictEntry.Value) }
                    };
                    value = obj;
                }
                else if (type.FullName.StartsWith("System.Collections.Generic.KeyValuePair`"))
                {
                    var obj = new JObject
                    {
                        { Convert.ToString(((dynamic)_value).Key) ?? "null", new JValue(((dynamic)_value).Value) }
                    };
                    value = obj;
                }
                else if ((_value is bool) || (_value is decimal))
                {
                    value = _value;
                }
                else if (type.IsPrimitive)
                {
                    value = Convert.ToDecimal(_value);
                }
                else if (_value is Color colorVal)
                {
                    value = (decimal)colorVal.ToArgb();
                }
                else if (type.IsValueType)
                {
                    var obj = new JObject();
                    foreach (var fieldDef in type.GetFields())
                    {
                        var field = fieldDef.GetValue(_value);
                        obj.Add(fieldDef.Name, new JValue(field));
                    }
                    value = obj;
                }
                else
                {
                    value = _value.ToString();
                }
            }
        }

        /// <summary>
        /// 按照Object的方式取子项内容
        /// </summary>
        /// <param name="_key"></param>
        /// <returns></returns>
        public JValue this[string _key]
        {
            get
            {
                try
                {
                    JValue item;
                    return (value is JObject obj)
                            ? (obj.TryGetValue(_key, out item) ? item : Null())
                            : Null();
                }
                catch (Exception err)
                {
                    Trace.TraceWarning(err.ToString());
                    return Null();
                }
            }

            set
            {
                if (!(this.value is JObject))
                {
                    this.value = new JObject();
                }

                ((JObject)this.value)[_key] = value;
            }
        }

        /// <summary>
        /// 按照Array的方式取子项内容
        /// </summary>
        /// <param name="_index"></param>
        /// <returns></returns>
        public JValue this[int _index]
        {
            get
            {
                try
                {
                    return (value is JArray arr) ? arr[_index] : this[_index.ToString()];
                }
                catch (Exception err)
                {
                    Trace.TraceWarning(err.ToString());
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

                    if ((_index >= 0) && (this.value is JArray arr))
                    {
                        while (_index >= arr.Count)
                        {
                            arr.Add(Null());
                        }
                        arr[_index] = value;
                    }
                }
            }
        }

        public JValue Get(object _key, bool _always = false)
        {
            if (_always)
            {
                if (_key is string keyText)
                {
                    AsObject();
                    if (value is JObject obj)
                    {
                        if (!obj.ContainsKey(keyText))
                        {
                            var newVal = Null();
                            obj[keyText] = newVal;
                            return newVal;
                        }
                        else
                        {
                            return obj[keyText];
                        }
                    }
                    return Null();
                }
                else
                {
                    try
                    {
                        int index = Convert.ToInt32(_key);
                        AsArray();
                        if ((index >= 0) && (value is JArray list))
                        {
                            if (index >= list.Count)
                            {
                                for (int i = list.Count; i < index; i++)
                                {
                                    list.Add(Null());
                                }
                                var newVal = Null();
                                list.Add(newVal);
                                return newVal;
                            }
                            else
                            {
                                return list[index];
                            }
                        }
                        else
                        {
                            return Get(Convert.ToString(_key) ?? string.Empty, true);
                        }
                    }
                    catch (Exception err)
                    {
                        Trace.TraceWarning(err.ToString());
                        return Get(Convert.ToString(_key) ?? string.Empty, true);
                    }
                }
            }
            else
            {
                if (_key is string keyText)
                {
                    return this[keyText];
                }
                else
                {
                    try
                    {
                        return this[Convert.ToInt32(_key)];
                    }
                    catch (Exception err)
                    {
                        Trace.TraceWarning(err.ToString());
                        return this[Convert.ToString(_key)??string.Empty];
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
                return (value is ICollection coll) ? coll.Count : 0;
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
                if (this.value is JArray arr)
                {
                    if (value < arr.Count)
                    {
                        if (value < 0)
                        {
                            value = 0;
                        }
                        arr.RemoveRange(value, arr.Count - value);
                    }
                    else
                    {
                        while (value >= arr.Count)
                        {
                            arr.Add(Null());
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

        /// <summary>
        /// 判断是否是Null值
        /// </summary>
        public bool IsNull
        {
            get
            {
                return value == null;
            }
        }

        /// <summary>
        /// 判断是否是数组
        /// </summary>
        public bool IsArray
        {
            get
            {
                return value is JArray;
            }
        }

        /// <summary>
        /// 判断是否是Object
        /// </summary>
        public bool IsObject
        {
            get
            {
                return (value is JObject);
            }
        }

        /// <summary>
        /// 判断是否是字符串
        /// </summary>
        public bool IsString
        {
            get
            {
                return (null != value) && (value is string);
            }
        }

        /// <summary>
        /// 判断是否是数值
        /// </summary>
        public bool IsNumber
        {
            get
            {
                return (value is decimal) || ((null != value) && !(value is bool) && value.GetType().IsPrimitive);
            }
        }

        /// <summary>
        /// 判断是否是存储buffer
        /// </summary>
        public bool IsBuffer
        {
            get
            {
                return value is byte[];
            }
        }

        /// <summary>
        /// 判断是否为空，包括null值、Object无子项、数组无内容、空字符串等
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return (null == value)
                        || ((value is string str) && string.IsNullOrEmpty(str))
                        || ((value is ICollection coll) && (0 == coll.Count));
            }
        }

        /// <summary>
        /// 生成一个Null值
        /// </summary>
        /// <returns></returns>
        public static JValue Null()
        {
            return new JValue() { value = null };
        }

        /// <summary>
        /// 生成一个Object
        /// </summary>
        /// <param name="_src"></param>
        /// <returns></returns>
        public static JValue Object(object _src = null)
        {
            JValue obj = Null();
            if (null != _src)
            {
                obj.SetValue(_src, false);
                if (!obj.IsObject)
                {
                    obj.AsObject();
                }
            }
            else
            {
                obj.value = new JObject();
            }

            return obj;
        }

        /// <summary>
        /// 生成一个数组
        /// </summary>
        /// <param name="_src"></param>
        /// <returns></returns>
        public static JValue Array(IEnumerable _src = null)
        {
            JArray list = new JArray();
            if (null != _src)
            {
                foreach (object item in _src)
                {
                    list.Add(new JValue(item));
                }
            }
            return new JValue() { value = list };
        }
        #endregion

        #region 数组方法
        /// <summary>
        /// 将值替换为数组
        /// </summary>
        /// <returns></returns>
        public JValue AsArray()
        {
            if (!(value is JArray))
            {
                value = new JArray();
            }
            return this;
        }

        /// <summary>
        /// 在尾部添加数据
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_force">若为false则如果当前结构内不是数组，则不做操作，否则则强制转换为数组</param>
        public void Push(object _value, bool _force = false)
        {
            if (!(value is JArray) && _force)
            {
                value = new JArray();
            }
            if (value is JArray arr)
            {
                arr.Add(new JValue(_value));
            }
        }

        /// <summary>
        /// 从尾部取得一个数据，并将之删除
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 在头部插入一个数据
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_force">若为false则如果当前结构内不是数组，则不做操作，否则则强制转换为数组</param>
        public void Unshift(object _value, bool _force = false)
        {
            if (!(value is JArray) && _force)
            {
                value = new JArray();
            }
            if (value is JArray arr)
            {
                arr.Insert(0, new JValue(_value));
            }
        }

        /// <summary>
        /// 从头部取得一个数据，并将之删除
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 取得首个数据
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 取得最后的数据
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 检索数据位置
        /// </summary>
        /// <param name="_value"></param>
        /// <returns></returns>
        public int IndexOf(object _value)
        {
            return (value is JArray list) ? list.FindIndex(e => e.Equals(_value)) : -1;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="_value"></param>
        public void Remove(object _value)
        {
            if (value is JArray list)
            {
                int index = IndexOf(_value);
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }
            }
        }

        public void Concat(object _value)
        {
            AsArray();
            if (value is JArray list)
            {
                if (_value is JValue jVal)
                {
                    if (jVal.IsArray)
                    {
                        list.AddRange(jVal.GetArrayItems());
                    }
                }
                else if (_value is IEnumerable enumSet)
                {
                    list.AddRange(enumSet.Cast<object>().Select(e => e is JValue ? (JValue)e : new JValue(e)));
                }
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="_compareFn"></param>
        public void Sort(Func<JValue, JValue, int> _compareFn)
        {
            if (value is JArray list)
            {
                list.Sort((a, b) =>
                {
                    return _compareFn(a, b);
                });
            }
        }

        #endregion

        #region 对象方法
        /// <summary>
        /// 将值强制替换为Object
        /// </summary>
        /// <returns></returns>
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
            return (value is JObject obj)
                    ? obj.ContainsKey(_key)
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

        /// <summary>
        /// 取得首个子项
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 取得最后的子项
        /// </summary>
        /// <returns></returns>
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
        public IEnumerable<KeyValuePair<string, JValue>> GetObjectItems()
        {
            if (value is JObject obj)
            {
                foreach (KeyValuePair<string, JValue> item in obj)
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
            if (value is JArray arr)
            {
                foreach (JValue item in arr)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Get an enumerator for the items of the value
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<object, JValue>> GetItems()
        {
            if (value is JObject obj)
            {
                foreach (KeyValuePair<string, JValue> item in obj)
                {
                    yield return new KeyValuePair<object, JValue>(item.Key, item.Value);
                }
            }
            else if (value is JArray arr)
            {
                int index = 0;
                foreach (JValue item in arr)
                {
                    yield return new KeyValuePair<object, JValue>(index++, item);
                }
            }
            else
            {
                yield return new KeyValuePair<object, JValue>(null, this);
            }
        }

        /// <summary>
        /// 清除所有子项数据
        /// </summary>
        public void ClearItems()
        {
            if (value is JObject obj)
            {
                obj.Clear();
            }
            else if (value is JArray arr)
            {
                arr.Clear();
            }
        }
        #endregion

        #region 对比类方法
        public override bool Equals(object _obj)
        {
            return EqualityComparer<object>.Default.Equals(value, (_obj is JValue) ? ((JValue)_obj).value : (new JValue(_obj)).value);
        }

        public bool Equals(JValue _other)
        {
            return EqualityComparer<object>.Default.Equals(value, _other.value);
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
        public object ToRawValue<T, TRef>(Func<JValue, TRef, ValueTuple<bool, T>> _fn = null, TRef _customData = default)
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

            object val = _value.value;
            int ret;

            if (val.GetType().IsPrimitive)
            {
                ret = Convert.ToInt32(val);
            }
            else
            {
                string sVal = Convert.ToString(val)?.Trim() ?? string.Empty;
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
        /// convert to uint
        /// </summary>
        /// <param name="_value"></param>
        public static implicit operator uint(JValue _value)
        {
            if (null == _value.value)
            {
                return 0;
            }

            object val = _value.value;
            uint ret;

            if (val.GetType().IsPrimitive)
            {
                ret = Convert.ToUInt32(val);
            }
            else
            {
                string sVal = Convert.ToString(val)?.Trim() ?? string.Empty;
                if (sVal.StartsWith("0x"))
                {
                    if (!uint.TryParse(sVal.Substring(2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out ret))
                    {
                        ret = 0;
                    }
                }
                else if (!uint.TryParse(sVal, out ret))
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

            object val = _value.value;
            float ret;

            if (val.GetType().IsPrimitive)
            {
                ret = Convert.ToSingle(val);
            }
            else
            {
                string sVal = Convert.ToString(val)?.Trim() ?? string.Empty;
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

            object val = _value.value;
            double ret;

            if (val.GetType().IsPrimitive)
            {
                ret = Convert.ToDouble(val);
            }
            else
            {
                string sVal = Convert.ToString(val)?.Trim() ?? string.Empty;
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

            object val = _value.value;
            decimal ret;

            if (val.GetType().IsPrimitive)
            {
                ret = Convert.ToDecimal(val);
            }
            else
            {
                string sVal = Convert.ToString(val)?.Trim() ?? string.Empty;
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
                        : ((_value.value is string str) ? str : _value.ToString(string.Empty, string.Empty, string.Empty));
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
            else if (value is bool bVal)
            {
                ret = bVal;
            }
            else if (value is decimal dVal)
            {
                ret = (dVal != 0);
            }
            else
            {
                string sVal = (value is string str) ? str : value.ToString();
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

                if (value is JObject obj)
                {
                    if (obj.Count > 0)
                    {
                        string midSpec = (string.IsNullOrEmpty(_space) ? ":" : ": ");
                        List<string> list = new List<string>();
                        foreach (KeyValuePair<string, JValue> item in obj)
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
                else if (value is JArray arr)
                {
                    if (arr.Count > 0)
                    {
                        List<string> list = new List<string>();
                        foreach (JValue item in arr)
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
                else if (value is string str)
                {
                    ret = @"""" + Regex.Replace(str, @"[\r\t\f\v\b\n\\""'\u0085\u2028\u2029]", (match) =>
                    {
                        switch (match.Value[0])
                        {
                            case '\r': return @"\r";
                            case '\t': return @"\t";
                            case '\f': return @"\f";
                            case '\v': return @"\v";
                            case '\b': return @"\b";
                            case '\n': return @"\n";
                            case '\\': return @"\\";
                            case '\"': return @"\""";
                            case '\'': return @"\'";
                            case '\u0085': return @"\u0085";
                            case '\u2028': return @"\u2028";
                            case '\u2029': return @"\u2029";
                            default: return match.Value;
                        }
                    }) + @"""";
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

        public static implicit operator JValue(int value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(uint value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(short value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(ushort value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(byte value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(sbyte value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(float value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(double value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(long value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(ulong value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(Color value)
        {
            return new JValue(value);
        }

        public static implicit operator JValue(byte[] value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// init data from a XMLNode
        /// </summary>
        /// <param name="_xmlNode"></param>
        private void FromXML(XmlNode _xmlNode)
        {
            string type = _xmlNode.Attributes?["type"]?.Value;
            if (type == "object")
            {
                JObject dict = new JObject();
                foreach (XmlNode node in _xmlNode.ChildNodes)
                {
                    string prefix = node.Prefix;
                    string name = node.LocalName;
                    if (!string.IsNullOrWhiteSpace(prefix))
                    {
                        name = (node.Attributes?[name]?.Value ?? name);
                    }
                    JValue itemValue = new JValue();
                    itemValue.FromXML(node);
                    dict[name] = itemValue;
                }
                value = dict;
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
                value = list;
            }
            else if (type == "boolean")
            {
                SetValue(0 == string.Compare(_xmlNode.InnerText, "true", StringComparison.OrdinalIgnoreCase), false);
            }
            else if (type == "number")
            {
                string valueText = _xmlNode.InnerText.Trim();
                decimal ret = 0M;
                if (valueText.StartsWith("0x"))
                {
                    decimal.TryParse(valueText.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ret);
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
            var docElement = doc.DocumentElement;
            if (null != docElement)
            {
                FromXML(docElement);
            }
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
                Trace.TraceWarning(err.ToString());
                _json = Null();
                return false;
            }
        }

        /// <summary>
        /// Probe value from string
        /// </summary>
        /// <param name="_value"></param>
        /// <returns></returns>
        private static object ProbeValueFromString(string _value)
        {
            if (null != _value)
            {
                if ((_value.StartsWith("\"") && _value.EndsWith("\"")) || (_value.StartsWith("\'") && _value.EndsWith("\'")))
                {
                    return _value.Substring(1, _value.Length - 2);
                }
                else if (_value.StartsWith("0x") && decimal.TryParse(_value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out decimal mHex))
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
                return new JValue((decimal)_value1 + _value2);
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
                ret.SetValue(this, true);
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
                ret.SetValue(value, true);
            }

            return ret;
        }

        public void Set(string _key, object _data)
        {
            this[_key] = (_data is JValue) ? (JValue)_data : new JValue(_data);
        }

        public void Set(int _index, object _data)
        {
            this[_index] = (_data is JValue) ? (JValue)_data : new JValue(_data);
        }

        public void Assign(JValue _other)
        {
            if (_other.value is JObject srcDict)
            {
                AsObject();
                if (value is JObject targetDict)
                {
                    foreach (KeyValuePair<string, JValue> item in srcDict)
                    {
                        JValue oriItem = this[item.Key];
                        if (oriItem.IsNull)
                        {
                            targetDict[item.Key] = item.Value;
                        }
                        else
                        {
                            oriItem.Assign(item.Value);
                        }
                    }
                }
            }
            else if (_other.value is JArray srcList)
            {
                int index = 0;
                if (value is JObject targetDict)
                {
                    foreach (JValue item in srcList)
                    {
                        JValue oriItem = this[index];
                        if (oriItem.IsNull)
                        {
                            targetDict[index.ToString()] = item;
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
                    AsArray();
                    if (value is JArray targetList)
                    {
                        foreach (JValue item in srcList)
                        {
                            JValue oriItem = this[index];
                            if (oriItem.IsNull)
                            {
                                targetList[index] = item;
                            }
                            else
                            {
                                oriItem.Assign(item);
                            }
                            index++;
                        }
                    }
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
                            ret[compositions[0].Substring(match.Length)] = new JValue((compositions.Length > 1) ? ProbeValueFromString(compositions[1]) : true);
                        }
                    }
                }
                else
                {
                    foreach (string item in _args)
                    {
                        string[] compositions = item.Split('=');
                        ret[compositions[0]] = new JValue((compositions.Length > 1) ? ProbeValueFromString(compositions[1]) : true);
                    }
                }

                return ret;
            }
        }
        #endregion
    }
#if NET6_0_OR_GREATER
#nullable restore
#endif
}