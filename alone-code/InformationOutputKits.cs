using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GeneralKit
{
    public abstract class TraceOut
    {
        private static string prefix = null;

        public static string Prefix
        {
            get
            {
                if (null == prefix)
                {
                    AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute));
                    prefix = $"{((null != titleAttr) ? titleAttr.Title : string.Empty)} TRACE";
                }

                return $"[{prefix}]";
            }

            set
            {
                prefix = value;
            }
        }

        public static void Print(string _szFormat, params object[] _args)
        {
            Trace.WriteLine(string.Format(Prefix + _szFormat, _args));
        }

        public static void Print(object _outContent)
        {
            Trace.WriteLine(Prefix + ((null == _outContent) ? "null" : _outContent.ToString()));
        }
    }

    public abstract class DebugOut
    {
        private static string prefix = null;

        public static string Prefix
        {
            get
            {
                if (null == prefix)
                {
                    AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute));
                    prefix = ((null != titleAttr) ? titleAttr.Title : string.Empty);
                }

                return $"[{prefix}]";
            }

            set
            {
                prefix = value;
            }
        }
        public static void Print(string _szFormat, params object[] _args)
        {
            Debug.WriteLine(string.Format(Prefix + _szFormat, _args));
        }

        public static void Print(object _outContent)
        {
            Debug.WriteLine(Prefix + ((null == _outContent) ? "null" : _outContent.ToString()));
        }
    }
}
