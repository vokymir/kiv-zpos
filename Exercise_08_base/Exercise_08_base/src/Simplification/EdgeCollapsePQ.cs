using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Zcu.Mve.ZMalik.Simplification
{
    /// <summary>
    /// Priority queue of edges with minimal edge collapse cost as the priority factor.
    /// </summary>
    public class EdgeCollapsePQ : ICollection<ConnectedEdge>, ICollection
    {
        private const Int32 DefaultCapacity = 16;
        private const double DefaultExcessGrow = 1.5;
        private const double DefaultTrimLimit = 1.5;

        private ConnectedEdge[] _items;

        private Int32 _capacity;
        private Int32 _count;
        private Int32 _initCapacity;


        /// <summary>
        /// Gets or sets the capacity of this priority queue.
        /// </summary>
        public int Capacity
        {
            get { return _capacity; }
            set { SetCapacity(value); }
        }


        /// <summary>
        /// Default contructor. Creates a new instance with a default initial capacity.
        /// </summary>
        public EdgeCollapsePQ() : this(DefaultCapacity) { }

        /// <summary>
        /// Parametric constructor. Creates a new instance with a specified initial capacity
        /// </summary>
        /// <param name="initialCapacity">initial capacity</param>
        public EdgeCollapsePQ(Int32 initialCapacity)
        {
            _count = 0;
            SetCapacity(initialCapacity);
            _initCapacity = initialCapacity;
        }


        /// <summary>
        /// Attempts to set the capacity to a specified value.
        /// </summary>
        /// <param name="newCapacity">new capacity</param>
        private void SetCapacity(int newCapacity)
        {
            int newCap = newCapacity;
            if (newCap < DefaultCapacity) newCap = DefaultCapacity;
            if (newCap < _count) newCap = _count;

            _capacity = newCap;
            if (_items == null)
            {
                _items = new ConnectedEdge[newCap];
                return;
            }
            else
            {
                Array.Resize<ConnectedEdge>(ref _items, newCap);
            }
        }

        /// <summary>
        /// Adds an edge to this priority queue.
        /// </summary>
        /// <param name="newItem">edge to add</param>
        public void Enqueue(ConnectedEdge newItem)
        {
            // if capacity is not enough, grow to DefaultGrow
            if (_count == _capacity) SetCapacity((Int32)(DefaultExcessGrow * _capacity));

            // i = index of the last position (i.e. entering index of the new item)
            int i = _count;
            // iparent = parent index for i
            int iparent = (_count - 1) / 2;

            ++_count;

            // while not at the top and new item's cost is lower than parent's cost
            while ((i > 0) && (newItem.Cost < _items[iparent].Cost))
            {
                // move parent down in the heap (to index i)
                _items[i] = _items[iparent];
                _items[i].Index = i;

                // update the indices
                i = iparent;
                iparent = (i - 1) / 2;
            }

            // i is the destination index for the new item, assign
            _items[i] = newItem;
            newItem.Index = i;
        }

        /// <summary>
        /// Removes edge at the specified index.
        /// </summary>
        /// <param name="index">index of the removed edge</param>
        /// <returns>the removed edge</returns>
        private ConnectedEdge RemoveAt(Int32 index)
        {
            // get the item to be removed
            ConnectedEdge item = _items[index];

            --_count;

            // get the last item in the heap to replace the removed one
            ConnectedEdge tmp = _items[_count];

            // clear the place of the last item
            _items[_count] = null;

            // bubble the replaced item to a proper place
            // (only if the heap isn't empty after the remove and the removed item wasn't at the end)
            if (_count > 0 && index != _count)
            {
                // first attempt to bubble up (if tmp has lower cost than its parent(s))

                // i = index of the removed item (currently index of tmp)
                int i = index;
                // j = parent index of i
                int j = (i - 1) / 2;

                // while not at the top and tmp's cost is lower than parent's cost
                while ((i > 0) && tmp.Cost < _items[j].Cost)
                {
                    // move parent down
                    _items[i] = _items[j];
                    _items[i].Index = i;

                    // update the indices
                    i = j;
                    j = (i - 1) / 2;
                }

                // if tmp didn't move up, try to bubble it down
                if (i == index)
                {
                    // j = left child index of i
                    j = (2 * i) + 1;
                    while (j < _count)
                    {
                        // select the child with the lower cost
                        if ((j + 1 < _count) && (_items[j + 1].Cost < _items[j].Cost)) j++;

                        // if that child has higher cost than tmp, we found tmp's place
                        if (_items[j].Cost > tmp.Cost) break;

                        // otherwise move the child up
                        _items[i] = _items[j];
                        _items[i].Index = i;

                        // update the indices
                        i = j;
                        j = (2 * i) + 1;
                    }
                }

                // store tmp at the found place
                _items[i] = tmp;
                tmp.Index = i;
            }

            return item;
        }

        /// <summary>
        /// Removes and returns the edge with the lowest cost (the topmost one).
        /// </summary>
        /// <returns>the edge with the lowest cost, null if the collection is empty</returns>
        public ConnectedEdge Dequeue()
        {
            if (Count == 0) return null;
            return RemoveAt(0);
        }

        /// <summary>
        /// Returns the edge with the lowest cost without removing it.
        /// </summary>
        /// <returns>the edge with the lowest cost, null if the collection is empty</returns>
        public ConnectedEdge Peek()
        {
            if (Count == 0) return null;
            return _items[0];
        }

        /// <summary>
        /// Minimizes the capacity of the priority queue.
        /// </summary>
        public void TrimExcess()
        {
            if (_count < DefaultTrimLimit * _capacity)
            {
                SetCapacity(_count);
            }
        }

        /// <summary>
        /// Checks if the priority queue is coherent (is heap).
        /// </summary>
        /// <returns>true if the PQ is coherent, else otherwise</returns>
        public bool CheckHeap()
        {
            int i = 0;
            int nh = _count / 2;
            int left, right;

            while (i < nh)
            {
                left = (2 * i) + 1;
                right = left + 1;
                if (_items[i].Cost > _items[left].Cost)
                {
                    return false;
                }
                if (right < _count && _items[i].Cost > _items[right].Cost)
                {
                    return false;
                }
                i++;
            }
            return true;
        }

    public ConnectedEdge this[int index]
    {
        get { return _items[index]; }
    }
        
    #region ICollection<ConnectedEdge> Members

        /// <summary>
        /// Gets the number of items in this collection.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Returns always false :)
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Adds a new item to the collection (calls Enqueue).
        /// </summary>
        /// <param name="item">item to add</param>
        public void Add(ConnectedEdge item)
        {
            Enqueue(item);
        }

        /// <summary>
        /// Removes all items from the collection and sets it to its initial capacity.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _count; ++i)
            {
                _items[i] = null;
            }
            _count = 0;
            _items = new ConnectedEdge[_initCapacity];
        }

        /// <summary>
        /// Checks for an item's presence in the collection.
        /// </summary>
        /// <param name="item">item to search for</param>
        /// <returns>true if the item is present in the collection, false otherwise</returns>
        public bool Contains(ConnectedEdge item)
        {
            return (item.Index >= 0 && item.Index < _count && _items[item.Index].Equals(item));
        }

        /// <summary>
        /// Copies the whole contents of this collection to a provided typed array
        /// starting at the specified index.
        /// </summary>
        /// <param name="array">the destination array</param>
        /// <param name="arrayIndex">starting index in the array</param>
        public void CopyTo(ConnectedEdge[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex is negative.");
            if (array.Rank > 1)
                throw new ArgumentException("array is multidimensional.");
            if (_count == 0)
                return;
            if (arrayIndex >= array.Length)
                throw new ArgumentException("arrayIndex is equal to or greater than the length of the array.");
            if (_count > (array.Length - arrayIndex))
                throw new ArgumentException("The number of elements in the source ICollection is greater than the available space from arrayIndex to the end of the destination array.");

            for (int i = 0; i < _count; i++)
            {
                array[arrayIndex + i] = _items[i];
            }
        }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="item">the item to remove</param>
        /// <returns>true if the item has been removed, false otherwise</returns>
        public bool Remove(ConnectedEdge item)
        {
            if (Contains(item))
            {
                RemoveAt(item.Index);
                return true;
            }
            return false;
        }

    #endregion

    #region ICollection Members

        /// <summary>
        /// Returns always false :)
        /// </summary>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the synchronization lock root.
        /// </summary>
        public object SyncRoot
        {
            get { return _items.SyncRoot; }
        }

        /// <summary>
        /// Copies the whole contents of this collection to an untyped array
        /// starting at the specified index. Throws an exception if the
        /// array's type does not match the collection item's type.
        /// </summary>
        /// <param name="array">the destination array</param>
        /// <param name="index">starting index in the array</param>
        public void CopyTo(Array array, int index)
        {
            this.CopyTo((ConnectedEdge[])array, index);
        }

    #endregion

    #region IEnumerable<ConnectedEdge> Members

        /// <summary>
        /// Provides the typed enumerator for this collection.
        /// </summary>
        /// <returns>the enumerator</returns>
        public IEnumerator<ConnectedEdge> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _items[i];
            }
        }

    #endregion

    #region IEnumerable Members

        /// <summary>
        /// Provides the untyped enumerator for this collection.
        /// </summary>
        /// <returns>the enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    #endregion
    }
}
