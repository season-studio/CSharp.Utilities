using System;
using System.Linq;

namespace SeasonStudio.Common
{
    public static class ArrayExtension
    {
        public static bool Compare<T>(T[] _arr1, T[] _arr2)
        {
            int count = _arr1.Length;
            if (count == _arr2.Length)
            {
                for (int i = 0; i < count; i++)
                {
                    if (!Equals(_arr1[i], _arr2[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}