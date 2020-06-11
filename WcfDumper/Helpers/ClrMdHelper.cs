using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using WcfDumper.DataModel;

namespace WcfDumper.Helpers
{
    public static class ClrMdHelper
    {
        public static DataTargetWrapper AttachToLiveProcess(int pid)
        {
            return new DataTargetWrapper(pid);
        }

        public static List<ulong> GetLastObjectInHierarchyAsArray(ClrHeap heap, ulong obj, string[] hierarchy, int currentIndex, string arrayTypeToVerify)
        {
            ulong arrayObj = GetLastObjectInHierarchy(heap, obj, hierarchy, currentIndex);
            ClrType arrayType = heap.GetObjectType(arrayObj);
            
            Debug.Assert(arrayType.Name == arrayTypeToVerify);
            
            List<ulong> arrayItems = GetArrayItems(arrayType, arrayObj);
            return arrayItems;
        }

        public static List<ulong> GetArrayItems(ClrType type, ulong items)
        {
            int length = type.GetArrayLength(items);
            var ret = new List<ulong>();

            for (int i = 0; i < length; i++)
            {
                var val = (ulong)type.GetArrayElementValue(items, i);

                if (val != 0)
                {
                    ret.Add(val);
                }
            }

            return ret;
        }

        public static ulong GetLastObjectInHierarchy(ClrHeap heap, ulong heapobject, string[] hierarchy, int currentIndex)
        {
            ClrType type = heap.GetObjectType(heapobject);
            ClrInstanceField field = type.GetFieldByName(hierarchy[currentIndex]);
            ulong fieldValue = (ulong)field.GetValue(heapobject, false, false);

            currentIndex++;
            if (currentIndex == hierarchy.Length)
            {
                return fieldValue;
            }

            return GetLastObjectInHierarchy(heap, fieldValue, hierarchy, currentIndex);
        }

        public static T GetObjectAs<T>(ClrHeap heap, ulong heapobject, string fieldName)
        {
            ClrType type = heap.GetObjectType(heapobject);
            ClrInstanceField field = type.GetFieldByName(fieldName);
            T fieldValue = (T)field.GetValue(heapobject);

            return fieldValue;
        }
    }
}
