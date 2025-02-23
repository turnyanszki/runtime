// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Ordered String/Object collection of name/value pairs with support for null key
 *
 * This class is intended to be used as a base class
 *
 */

#pragma warning disable 618 // obsolete types, namely IHashCodeProvider

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Collections.Specialized
{
    /// <devdoc>
    /// <para>Provides the <see langword='abstract '/>base class for a sorted collection of associated <see cref='string' qualify='true'/> keys
    ///    and <see cref='object' qualify='true'/> values that can be accessed either with the hash code of
    ///    the key or with the index.</para>
    /// </devdoc>
    public abstract class NameObjectCollectionBase : ICollection, ISerializable, IDeserializationCallback
    {
        private bool _readOnly;
        private ArrayList _entriesArray;
        private IEqualityComparer _keyComparer;
        private volatile Hashtable _entriesTable;
        private volatile NameObjectEntry? _nullKeyEntry;
        private KeysCollection? _keys;
        private int _version;

        private static readonly StringComparer s_defaultComparer = CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(CompareOptions.IgnoreCase);

        /// <devdoc>
        /// <para> Creates an empty <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance with the default initial capacity and using the default case-insensitive hash
        ///    code provider and the default case-insensitive comparer.</para>
        /// </devdoc>
        protected NameObjectCollectionBase() : this(s_defaultComparer)
        {
        }

        protected NameObjectCollectionBase(IEqualityComparer? equalityComparer)
        {
            _keyComparer = equalityComparer ?? s_defaultComparer;
            Reset();
        }

        protected NameObjectCollectionBase(int capacity, IEqualityComparer? equalityComparer) : this(equalityComparer)
        {
            Reset(capacity);
        }

        [Obsolete("This constructor has been deprecated. Use NameObjectCollectionBase(IEqualityComparer) instead.")]
        protected NameObjectCollectionBase(IHashCodeProvider? hashProvider, IComparer? comparer)
        {
            _keyComparer = new CompatibleComparer(hashProvider, comparer);
            Reset();
        }

        [Obsolete("This constructor has been deprecated. Use NameObjectCollectionBase(Int32, IEqualityComparer) instead.")]
        protected NameObjectCollectionBase(int capacity, IHashCodeProvider? hashProvider, IComparer? comparer)
        {
            _keyComparer = new CompatibleComparer(hashProvider, comparer);
            Reset(capacity);
        }

        /// <devdoc>
        /// <para>Creates an empty <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance with the specified
        ///    initial capacity and using the default case-insensitive hash code provider
        ///    and the default case-insensitive comparer.</para>
        /// </devdoc>
        protected NameObjectCollectionBase(int capacity)
        {
            _keyComparer = s_defaultComparer;
            Reset(capacity);
        }

        protected NameObjectCollectionBase(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual void OnDeserialization(object? sender)
        {
            throw new PlatformNotSupportedException();
        }

        //
        // Private helpers
        //

        [MemberNotNull(nameof(_entriesArray))]
        [MemberNotNull(nameof(_entriesTable))]
        private void Reset()
        {
            _entriesArray = new ArrayList();
            _entriesTable = new Hashtable(_keyComparer);
            _nullKeyEntry = null;
            _version++;
        }

        [MemberNotNull(nameof(_entriesArray))]
        [MemberNotNull(nameof(_entriesTable))]
        private void Reset(int capacity)
        {
            _entriesArray = new ArrayList(capacity);
            _entriesTable = new Hashtable(capacity, _keyComparer);
            _nullKeyEntry = null;
            _version++;
        }

        private NameObjectEntry? FindEntry(string? key)
        {
            if (key != null)
                return (NameObjectEntry?)_entriesTable[key];
            else
                return _nullKeyEntry;
        }

        internal IEqualityComparer Comparer
        {
            get
            {
                return _keyComparer;
            }
            set
            {
                _keyComparer = value;
            }
        }


        /// <devdoc>
        /// <para>Gets or sets a value indicating whether the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance is read-only.</para>
        /// </devdoc>
        protected bool IsReadOnly
        {
            get { return _readOnly; }
            set { _readOnly = value; }
        }

        /// <devdoc>
        /// <para>Gets a value indicating whether the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance contains entries whose
        ///    keys are not <see langword='null'/>.</para>
        /// </devdoc>
        protected bool BaseHasKeys()
        {
            return (_entriesTable.Count > 0);  // any entries with keys?
        }

        //
        // Methods to add / remove entries
        //

        /// <devdoc>
        ///    <para>Adds an entry with the specified key and value into the
        ///    <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected void BaseAdd(string? name, object? value)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            NameObjectEntry entry = new NameObjectEntry(name, value);

            // insert entry into hashtable
            if (name != null)
            {
                if (_entriesTable[name] == null)
                    _entriesTable.Add(name, entry);
            }
            else
            { // null key -- special case -- hashtable doesn't like null keys
                if (_nullKeyEntry == null)
                    _nullKeyEntry = entry;
            }

            // add entry to the list
            _entriesArray.Add(entry);

            _version++;
        }

        /// <devdoc>
        ///    <para>Removes the entries with the specified key from the
        ///    <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected void BaseRemove(string? name)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            if (name != null)
            {
                // remove from hashtable
                _entriesTable.Remove(name);

                // remove from array
                for (int i = _entriesArray.Count - 1; i >= 0; i--)
                {
                    if (_keyComparer.Equals(name, BaseGetKey(i)))
                        _entriesArray.RemoveAt(i);
                }
            }
            else
            { // null key -- special case
                // null out special 'null key' entry
                _nullKeyEntry = null;

                // remove from array
                for (int i = _entriesArray.Count - 1; i >= 0; i--)
                {
                    if (BaseGetKey(i) == null)
                        _entriesArray.RemoveAt(i);
                }
            }

            _version++;
        }

        /// <devdoc>
        ///    <para> Removes the entry at the specified index of the
        ///    <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected void BaseRemoveAt(int index)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            string? key = BaseGetKey(index);

            if (key != null)
            {
                // remove from hashtable
                _entriesTable.Remove(key);
            }
            else
            { // null key -- special case
                // null out special 'null key' entry
                _nullKeyEntry = null;
            }

            // remove from array
            _entriesArray.RemoveAt(index);

            _version++;
        }

        /// <devdoc>
        /// <para>Removes all entries from the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected void BaseClear()
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            Reset();
        }

        //
        // Access by name
        //

        /// <devdoc>
        ///    <para>Gets the value of the first entry with the specified key from
        ///       the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected object? BaseGet(string? name)
        {
            NameObjectEntry? e = FindEntry(name);
            return (e != null) ? e.Value : null;
        }

        /// <devdoc>
        /// <para>Sets the value of the first entry with the specified key in the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/>
        /// instance, if found; otherwise, adds an entry with the specified key and value
        /// into the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/>
        /// instance.</para>
        /// </devdoc>
        protected void BaseSet(string? name, object? value)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            NameObjectEntry?  entry = FindEntry(name);
            if (entry != null)
            {
                entry.Value = value;
                _version++;
            }
            else
            {
                BaseAdd(name, value);
            }
        }

        //
        // Access by index
        //

        /// <devdoc>
        ///    <para>Gets the value of the entry at the specified index of
        ///       the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected object? BaseGet(int index)
        {
            NameObjectEntry entry = (NameObjectEntry)_entriesArray[index]!;   // no null entry added to the array
            return entry.Value;
        }

        /// <devdoc>
        ///    <para>Gets the key of the entry at the specified index of the
        ///    <see cref='System.Collections.Specialized.NameObjectCollectionBase'/>
        ///    instance.</para>
        /// </devdoc>
        protected string? BaseGetKey(int index)
        {
            NameObjectEntry entry = (NameObjectEntry)_entriesArray[index]!;
            return entry.Key;
        }

        /// <devdoc>
        ///    <para>Sets the value of the entry at the specified index of
        ///       the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected void BaseSet(int index, object? value)
        {
            if (_readOnly)
                throw new NotSupportedException(SR.CollectionReadOnly);

            NameObjectEntry entry = (NameObjectEntry)_entriesArray[index]!;
            entry.Value = value;
            _version++;
        }

        //
        // ICollection implementation
        //

        /// <devdoc>
        /// <para>Returns an enumerator that can iterate through the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/>.</para>
        /// </devdoc>
        public virtual IEnumerator GetEnumerator()
        {
            return new NameObjectKeysEnumerator(this);
        }

        /// <devdoc>
        /// <para>Gets the number of key-and-value pairs in the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        public virtual int Count
        {
            get
            {
                return _entriesArray.Count;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_MultiRank, nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum_Index);
            }

            if (array.Length - index < _entriesArray.Count)
            {
                throw new ArgumentException(SR.Arg_InsufficientSpace);
            }

            for (IEnumerator e = this.GetEnumerator(); e.MoveNext();)
                array.SetValue(e.Current, index++);
        }

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        //
        //  Helper methods to get arrays of keys and values
        //

        /// <devdoc>
        /// <para>Returns a <see cref='string' qualify='true'/> array containing all the keys in the
        /// <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected string?[] BaseGetAllKeys()
        {
            int n = _entriesArray.Count;
            string?[] allKeys = new string[n];

            for (int i = 0; i < n; i++)
                allKeys[i] = BaseGetKey(i);

            return allKeys;
        }

        /// <devdoc>
        /// <para>Returns an <see cref='object' qualify='true'/> array containing all the values in the
        /// <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected object?[] BaseGetAllValues()
        {
            int n = _entriesArray.Count;
            object?[] allValues = new object[n];

            for (int i = 0; i < n; i++)
                allValues[i] = BaseGet(i);

            return allValues;
        }

        /// <devdoc>
        ///    <para>Returns an array of the specified type containing
        ///       all the values in the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        protected object?[] BaseGetAllValues(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            int n = _entriesArray.Count;
            object?[] allValues = (object?[])Array.CreateInstance(type, n);

            for (int i = 0; i < n; i++)
            {
                allValues[i] = BaseGet(i);
            }

            return allValues;
        }

        //
        // Keys property
        //

        /// <devdoc>
        /// <para>Returns a <see cref='System.Collections.Specialized.NameObjectCollectionBase.KeysCollection'/> instance containing
        ///    all the keys in the <see cref='System.Collections.Specialized.NameObjectCollectionBase'/> instance.</para>
        /// </devdoc>
        public virtual KeysCollection Keys
        {
            get
            {
                if (_keys == null)
                    _keys = new KeysCollection(this);
                return _keys;
            }
        }

        //
        // Simple entry class to allow substitution of values and indexed access to keys
        //

        internal sealed class NameObjectEntry
        {
            internal NameObjectEntry(string? name, object? value)
            {
                Key = name;
                Value = value;
            }

            internal string? Key;
            internal object? Value;
        }

        //
        // Enumerator over keys of NameObjectCollection
        //

        internal sealed class NameObjectKeysEnumerator : IEnumerator
        {
            private int _pos;
            private readonly NameObjectCollectionBase _coll;
            private readonly int _version;

            internal NameObjectKeysEnumerator(NameObjectCollectionBase coll)
            {
                _coll = coll;
                _version = _coll._version;
                _pos = -1;
            }

            public bool MoveNext()
            {
                if (_version != _coll._version)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                if (_pos < _coll.Count - 1)
                {
                    _pos++;
                    return true;
                }
                else
                {
                    _pos = _coll.Count;
                    return false;
                }
            }

            public void Reset()
            {
                if (_version != _coll._version)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _pos = -1;
            }

            public object? Current
            {
                get
                {
                    if (_pos >= 0 && _pos < _coll.Count)
                    {
                        return _coll.BaseGetKey(_pos);
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                }
            }
        }

        //
        // Keys collection
        //

        /// <devdoc>
        /// <para>Represents a collection of the <see cref='string' qualify='true'/> keys of a collection.</para>
        /// </devdoc>
        public class KeysCollection : ICollection
        {
            private readonly NameObjectCollectionBase _coll;

            internal KeysCollection(NameObjectCollectionBase coll)
            {
                _coll = coll;
            }

            // Indexed access

            /// <devdoc>
            ///    <para> Gets the key at the specified index of the collection.</para>
            /// </devdoc>
            public virtual string? Get(int index)
            {
                return _coll.BaseGetKey(index);
            }

            /// <devdoc>
            ///    <para>Represents the entry at the specified index of the collection.</para>
            /// </devdoc>
            public string? this[int index]
            {
                get
                {
                    return Get(index);
                }
            }

            // ICollection implementation

            /// <devdoc>
            ///    <para>Returns an enumerator that can iterate through the
            ///    <see cref='System.Collections.Specialized.NameObjectCollectionBase.KeysCollection'/>.</para>
            /// </devdoc>
            public IEnumerator GetEnumerator()
            {
                return new NameObjectKeysEnumerator(_coll);
            }

            /// <devdoc>
            /// <para>Gets the number of keys in the <see cref='System.Collections.Specialized.NameObjectCollectionBase.KeysCollection'/>.</para>
            /// </devdoc>
            public int Count
            {
                get
                {
                    return _coll.Count;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                ArgumentNullException.ThrowIfNull(array);

                if (array.Rank != 1)
                {
                    throw new ArgumentException(SR.Arg_MultiRank, nameof(array));
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum_Index);
                }

                if (array.Length - index < _coll.Count)
                {
                    throw new ArgumentException(SR.Arg_InsufficientSpace);
                }

                for (IEnumerator e = this.GetEnumerator(); e.MoveNext();)
                    array.SetValue(e.Current, index++);
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)_coll).SyncRoot; }
            }


            bool ICollection.IsSynchronized
            {
                get { return false; }
            }
        }
    }
}
