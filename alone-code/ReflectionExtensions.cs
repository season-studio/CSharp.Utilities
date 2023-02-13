using System.Linq.Expressions;
using System.Reflection;

namespace SeasonStudio.Common
{
    /// <summary>
    /// Reflection extension
    /// </summary>
    public static class ReflectionExtension
    {
        /// <summary>
        /// 动态生成委托类型
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static Type GetDelegateType(this MethodInfo methodInfo)
        {
            return (methodInfo.ReturnType == typeof(void)) ? Expression.GetActionType(methodInfo.GetParameters().Select(e => e.ParameterType).ToArray()) : Expression.GetFuncType(methodInfo.GetParameters().Select(e => e.ParameterType).Append(methodInfo.ReturnType).ToArray());
        }

        /// <summary>
        /// 自动生成动态委托类型，并生成委托实例
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Delegate CreateDelegateDynamic(this MethodInfo methodInfo, object? obj)
        {
            return methodInfo.CreateDelegate(GetDelegateType(methodInfo), obj);
        }

        /// <summary>
        /// 动态生成委托类型
        /// </summary>
        /// <param name="ctorInfo"></param>
        /// <returns></returns>
        public static Type GetDelegateType(this ConstructorInfo ctorInfo)
        {
            return Expression.GetFuncType(ctorInfo.GetParameters().Select(e => e.ParameterType).Append(ctorInfo.DeclaringType!).ToArray());
        }

        /// <summary>
        /// 自动生成动态委托类型，并生成委托实例
        /// </summary>
        /// <param name="ctorInfo"></param>
        /// <returns></returns>
        public static Delegate CreateDelegateDynamic(this ConstructorInfo ctorInfo)
        {
            var paramList = ctorInfo.GetParameters().Select((e, idx) => Expression.Parameter(e.ParameterType, $"a{idx}")).ToArray();
            var call = Expression.New(ctorInfo, paramList);
            return Expression.Lambda(call, paramList).Compile();
        }
    }

    /// <summary>
    /// Utilities for operating the object by reflection
    /// </summary>
    public static class ObjectExtenstion
    {
        public struct ObjectReflectAccessor
        {
            public object? ReflectInfo;
            public object? This;
            public bool Valid => ReflectInfo != null;
            public object? Get()
            {
                if (ReflectInfo is FieldInfo fieldInfo)
                {
                    return fieldInfo.GetValue(This);
                }
                else if (ReflectInfo is PropertyInfo propertyInfo)
                {
                    return propertyInfo.GetValue(This);
                }
                return ReflectInfo;
            }
            public void Set(object _value)
            {
                if (ReflectInfo is FieldInfo fieldInfo)
                {
                    fieldInfo.SetValue(This, _value);
                }
                else if (ReflectInfo is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(This, _value);
                }
            }
            public object? Invoke(params object[] _args)
            {
                if (ReflectInfo is MethodInfo methodInfo)
                {
                    return methodInfo.Invoke(This, _args);
                }
                return null;
            }
        }

        public static ObjectReflectAccessor _Field(this object _obj, string _name, BindingFlags _flags)
        {
            return new ObjectReflectAccessor()
            {
                This = (_flags & BindingFlags.Static) != 0 ? null : _obj,
                ReflectInfo = _obj?.GetType().GetField(_name, _flags)
            };
        }

        public static ObjectReflectAccessor _Field(this object _obj, string _name)
        {
            return _obj._Field(_name, BindingFlags.Instance | BindingFlags.Public);
        }

        public static ObjectReflectAccessor _FieldP(this object _obj, string _name)
        {
            return _obj._Field(_name, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static ObjectReflectAccessor _FieldS(this object _obj, string _name)
        {
            return _obj._Field(_name, BindingFlags.Static | BindingFlags.Public);
        }

        public static ObjectReflectAccessor _FieldPS(this object _obj, string _name)
        {
            return _obj._Field(_name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static ObjectReflectAccessor _Property(this object _obj, string _name, BindingFlags _flags)
        {
            return new ObjectReflectAccessor()
            {
                This = (_flags & BindingFlags.Static) != 0 ? null : _obj,
                ReflectInfo = _obj?.GetType().GetProperty(_name, _flags)
            };
        }

        public static ObjectReflectAccessor _Property(this object _obj, string _name)
        {
            return _obj._Property(_name, BindingFlags.Instance | BindingFlags.Public);
        }

        public static ObjectReflectAccessor _PropertyP(this object _obj, string _name)
        {
            return _obj._Property(_name, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static ObjectReflectAccessor _PropertyS(this object _obj, string _name)
        {
            return _obj._Property(_name, BindingFlags.Static | BindingFlags.Public);
        }

        public static ObjectReflectAccessor _PropertyPS(this object _obj, string _name)
        {
            return _obj._Property(_name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static ObjectReflectAccessor _Method(this object _obj, string _name, BindingFlags _flags)
        {
            return new ObjectReflectAccessor()
            {
                This = (_flags & BindingFlags.Static) != 0 ? null : _obj,
                ReflectInfo = _obj?.GetType().GetMethod(_name, _flags)
            };
        }

        public static ObjectReflectAccessor _Method(this object _obj, string _name)
        {
            return _obj._Method(_name, BindingFlags.Instance | BindingFlags.Public);
        }

        public static ObjectReflectAccessor _MethodP(this object _obj, string _name)
        {
            return _obj._Method(_name, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static ObjectReflectAccessor _MethodS(this object _obj, string _name)
        {
            return _obj._Method(_name, BindingFlags.Static | BindingFlags.Public);
        }

        public static ObjectReflectAccessor _MethodPS(this object _obj, string _name)
        {
            return _obj._Method(_name, BindingFlags.Static | BindingFlags.NonPublic);
        }
    }
}