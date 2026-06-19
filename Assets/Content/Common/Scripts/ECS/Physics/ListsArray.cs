using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace LittlePhysics
{
    /// <summary>
    /// Fixed-capacity array of lists laid out as a flat 2D buffer.
    /// Layout: Data[X * Y] where list i occupies [i*Y, (i+1)*Y).
    ///
    /// TryAdd is NOT thread-safe per list index. Each list index must be
    /// written by a single thread at a time. Use this for Jacobi-style jobs
    /// where entity i is the sole writer of list i.
    ///
    /// For contended parallel writes (multiple threads writing to the same list
    /// index), use AsParallelWriter() which provides a lock-free atomic TryAdd.
    /// </summary>
    public unsafe struct ListsArray<T> : IDisposable where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] private T* Data;
        [NativeDisableUnsafePtrRestriction] private int* Counts;

        private int ListCount;
        private int ListCapacity;
        private Allocator AllocatorLabel;

        public int TotalListCount => ListCount;
        public int CapacityPerList => ListCapacity;
        public bool IsCreated => Data != null;

        public ListsArray(int listCount, int listCapacity, Allocator allocator)
        {
            ListCount = listCount;
            ListCapacity = listCapacity;
            AllocatorLabel = allocator;

            long dataBytes = (long)UnsafeUtility.SizeOf<T>() * listCount * listCapacity;
            Data = (T*)UnsafeUtility.Malloc(dataBytes, UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.MemClear(Data, dataBytes);

            long countBytes = (long)UnsafeUtility.SizeOf<int>() * listCount;
            Counts = (int*)UnsafeUtility.Malloc(countBytes, UnsafeUtility.AlignOf<int>(), allocator);
            UnsafeUtility.MemClear(Counts, countBytes);
        }

        public bool TryAdd(int listIndex, T value)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.TryAdd: listIndex is out of range");
            int count = Counts[listIndex];

            if (count >= ListCapacity)
            {
                return false;
            }

            Data[listIndex * ListCapacity + count] = value;
            Counts[listIndex] = count + 1;
            return true;
        }

        public int GetCount(int listIndex)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.GetCount: listIndex is out of range");
            return Counts[listIndex];
        }

        public bool CanAdd(int listIndex)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.CanAdd: listIndex is out of range");
            return Counts[listIndex] < ListCapacity;
        }

        public T GetValue(int listIndex, int valueIndex)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.GetValue: listIndex is out of range");
            PhysicsDebug.SafeAssert((uint)valueIndex < (uint)ListCapacity, "ListsArray.GetValue: valueIndex is out of range");
            return Data[listIndex * ListCapacity + valueIndex];
        }

        public void SetValue(int listIndex, int valueIndex, T value)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.SetValue: listIndex is out of range");
            PhysicsDebug.SafeAssert((uint)valueIndex < (uint)ListCapacity, "ListsArray.SetValue: valueIndex is out of range");
            Data[listIndex * ListCapacity + valueIndex] = value;
        }

        public struct Iterator
        {
            public Iterator(int startIndex = 0)
            {
                Index = startIndex;
                HasStarted = false;
            }

            public int Index;
            public bool HasStarted;
        }

        public bool Traverse(int listIndex, ref Iterator iterator, out T value)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.Traverse: listIndex is out of range");
            int count = Counts[listIndex];

            if (!iterator.HasStarted)
            {
                iterator.HasStarted = true;
            }
            else
            {
                iterator.Index++;
            }

            if (iterator.Index >= count)
            {
                value = default;
                return false;
            }

            value = Data[listIndex * ListCapacity + iterator.Index];
            return true;
        }

        public void Clear()
        {
            UnsafeUtility.MemClear(Counts, (long)UnsafeUtility.SizeOf<int>() * ListCount);
        }

        public void ClearAt(int listIndex)
        {
            PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.ClearAt: listIndex is out of range");
            Counts[listIndex] = 0;
        }

        public void Dispose()
        {
            if (Data != null)
            {
                UnsafeUtility.Free(Data, AllocatorLabel);
                Data = null;
            }

            if (Counts != null)
            {
                UnsafeUtility.Free(Counts, AllocatorLabel);
                Counts = null;
            }

            ListCount = 0;
            ListCapacity = 0;
        }

        /// <summary>
        /// Returns a ParallelWriter view of this array. The ParallelWriter shares
        /// the same underlying memory and uses atomic CAS on Counts to support
        /// contended parallel writes (multiple threads writing to the same list index).
        /// </summary>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter
            {
                Data = Data,
                Counts = Counts,
                ListCapacity = ListCapacity,
                ListCount = ListCount,
            };
        }

        /// <summary>
        /// Returns a ParallelHashWriter view of this array. The ParallelHashWriter shares
        /// the same underlying memory and uses a per-list spinlock to safely perform
        /// duplicate-checked writes from multiple threads targeting the same list index.
        /// </summary>
        public ParallelHashWriter AsParallelHashWriter()
        {
            return new ParallelHashWriter
            {
                Data = Data,
                Counts = Counts,
                ListCapacity = ListCapacity,
                ListCount = ListCount,
            };
        }

        /// <summary>
        /// Flat read/write view of the underlying 1D data buffer.
        /// Index arithmetic: flatIndex = listIndex * ListCapacity + slotIndex.
        /// This[flatIndex] provides direct get and set on the raw data.
        /// Use GetCount(listIndex) to bound-check before accessing a slot.
        /// Safe in IJobParallelFor when job i only touches flatIndex values
        /// that belong to list i.
        /// </summary>
        public unsafe struct ParallelReader
        {
            [NativeDisableUnsafePtrRestriction] internal T* Data;
            [NativeDisableUnsafePtrRestriction] internal int* Counts;
            public int ListCapacity;

            public int GetCount(int listIndex) => Counts[listIndex];

            public T this[int flatIndex]
            {
                get => Data[flatIndex];
                set => Data[flatIndex] = value;
            }
        }

        public ParallelReader AsParallelReader() => new ParallelReader
        {
            Data = Data,
            Counts = Counts,
            ListCapacity = ListCapacity,
        };

        /// <summary>
        /// Lock-free parallel writer for ListsArray. Uses Volatile.Read and
        /// Interlocked.CompareExchange to safely claim a slot when multiple threads
        /// may write to the same list index simultaneously.
        /// </summary>
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction] internal T* Data;
            [NativeDisableUnsafePtrRestriction] internal int* Counts;
            internal int ListCapacity;
            internal int ListCount;

            public bool TryAdd(int listIndex, T value)
            {
                PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.ParallelWriter.TryAdd: listIndex is out of range");
                int* countPtr = Counts + listIndex;

                int slot;
                int current;
                do
                {
                    current = Volatile.Read(ref *countPtr);
                    if (current >= ListCapacity)
                    {
                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref *countPtr, current + 1, current) != current);

                slot = current;
                Data[listIndex * ListCapacity + slot] = value;
                return true;
            }
        }

        /// <summary>
        /// Spinlock-guarded parallel writer for ListsArray. Counts[i] doubles as the lock:
        /// -1 means the list is locked. The CAS that acquires the lock also captures the
        /// current count, so no separate read is needed after locking. TryAddUnique scans
        /// existing entries with UnsafeUtility.MemCmp and appends only if not found.
        /// Volatile.Write on unlock provides a release fence ensuring data is visible
        /// before the count is restored. T requires no extra constraint.
        /// </summary>
        public unsafe struct ParallelHashWriter
        {
            [NativeDisableUnsafePtrRestriction] internal T* Data;
            [NativeDisableUnsafePtrRestriction] internal int* Counts;
            internal int ListCapacity;
            internal int ListCount;

            public bool TryAddUnique(int listIndex, T value)
            {
                PhysicsDebug.SafeAssert((uint)listIndex < (uint)ListCount, "ListsArray.ParallelHashWriter.TryAddUnique: listIndex is out of range");

                int count;

                while (true)
                {
                    count = Volatile.Read(ref Counts[listIndex]);
                    
                    if (count == -1)
                    {
                        continue;
                    }
                    if (Interlocked.CompareExchange(ref Counts[listIndex], -1, count) == count)
                    {
                        break;
                    }
                }

                for (int slot = 0; slot < count; slot++)
                {
                    T* slotPtr = Data + (listIndex * ListCapacity + slot);

                    if (UnsafeUtility.MemCmp(slotPtr, UnsafeUtility.AddressOf(ref value), UnsafeUtility.SizeOf<T>()) == 0)
                    {
                        Volatile.Write(ref Counts[listIndex], count);
                        return false;
                    }
                }

                if (count < ListCapacity)
                {
                    Data[listIndex * ListCapacity + count] = value;
                    Volatile.Write(ref Counts[listIndex], count + 1);
                    return true;
                }

                Volatile.Write(ref Counts[listIndex], count);
                return false;
            }
        }
    }
}
