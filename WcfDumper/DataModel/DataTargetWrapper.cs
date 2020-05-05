using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using WcfDumper.Helpers;

namespace WcfDumper.DataModel
{
    public class DataTargetWrapper
    {
        private string myFile;
        private int myPid;

        public DataTargetWrapper(string file)
        {
            myFile = file;
        }

        public DataTargetWrapper(int pid)
        {
            myPid = pid;
        }

        public void Process()
        {
            bool isTargetx64 = ProcessHelper.NativeMethods.Is64Bit(myPid);

            bool archmatch = (isTargetx64 && Environment.Is64BitProcess) ||
                             (!isTargetx64 && !Environment.Is64BitProcess);

            if (!archmatch)
            {
                Console.WriteLine($"PID: {myPid} - Inconsistent process architecture.");
                return;
            }

            using (DataTarget dataTarget = GetDataTarget())
            {
                foreach (ClrInfo clrVersionInfo in dataTarget.ClrVersions)
                {
                    ClrInfoCallback?.Invoke(clrVersionInfo);
                }

                if (dataTarget.ClrVersions.Count == 0)
                {
                    Console.WriteLine($"PID: {myPid} - Not a managed executable.");
                    return;
                }

                ClrInfo runtimeInfo = dataTarget.ClrVersions[0];
                ClrRuntime runtime = runtimeInfo.CreateRuntime();
                ClrHeap heap = runtime.Heap;
                
                if (!heap.CanWalkHeap)
                {
                    ClrHeapIsNotWalkableCallback?.Invoke();
                    return;
                }
                foreach (ulong obj in heap.EnumerateObjectAddresses())
                {
                    ClrType type = heap.GetObjectType(obj);

                    // If heap corruption, continue past this object.
                    if (type == null)
                        continue;

                    if (TypesToDump.Contains(type.Name))
                    {
                        ClrObjectOfTypeFoundCallback(heap, obj, type.Name);
                    }
                }
            }
        }

        private DataTarget GetDataTarget()
        {
            if (myFile != null)
            {
                return DataTarget.LoadCrashDump(myFile);
            }

            return DataTarget.AttachToProcess(myPid, 5000, AttachFlag.NonInvasive);
        }

        public Action<ClrInfo> ClrInfoCallback;
        public Action ClrHeapIsNotWalkableCallback;
        public Action<ClrHeap, ulong, string> ClrObjectOfTypeFoundCallback;
        public HashSet<string> TypesToDump = new HashSet<string>();
    }
}
