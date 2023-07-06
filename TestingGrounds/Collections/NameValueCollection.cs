using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TestingGrounds.Extensions;

namespace TestingGrounds.Collections
{
    [Serializable]
    public class NameValueCollection : ICollection<NameValueTuple>, ICollection, ISerializable
    {
        public static IEqualityComparer<string> DefaultComparer => StringComparer.OrdinalIgnoreCase;

        private readonly Dictionary<string, Entry> _lookupDict;
        private readonly List<Entry> _entryList;
        private int _version;

        public NameValueCollection()
        {
            _lookupDict = new(DefaultComparer);
            _entryList = new();
        }

        public NameValueCollection(IEqualityComparer<string>? comparer)
        {
            _lookupDict = new(comparer ?? DefaultComparer);
            _entryList = new();
        }

        public NameValueCollection(int capacity)
        {
            _lookupDict = new(capacity, DefaultComparer);
            _entryList = new(capacity);
        }

        public NameValueCollection(int capacity, IEqualityComparer<string>? comparer)
        {
            _lookupDict = new(capacity, comparer ?? DefaultComparer);
            _entryList = new(capacity);
        }

        public NameValueCollection(NameValueCollection collection)
        {
            _lookupDict = new(collection.Count, DefaultComparer);
            _entryList = new(collection.Count);

            foreach (var tuple in collection)
            {
                Add(tuple.Name, tuple.Values);
            }
        }

        public NameValueCollection(NameValueCollection collection, IEqualityComparer<string>? comparer)
        {
            _lookupDict = new(collection.Count, comparer ?? DefaultComparer);
            _entryList = new(collection.Count);

            foreach (var tuple in collection)
            {
                Add(tuple.Name, tuple.Values);
            }
        }

        public int Count => _entryList.Count;

        public IEqualityComparer<string> Comparer => _lookupDict.Comparer;

        public string this[string name]
        {
            get => Get(name);
            set => Set(name, value);
        }

        public bool TryGetValue(string name, [MaybeNullWhen(false)] out string value)
        {
            if (_lookupDict.TryGetValue(name, out var entry))
            {
                value = entry.ToSingleValue();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryGetValues(string name, [MaybeNullWhen(false)] out string[] values)
        {
            if (_lookupDict.TryGetValue(name, out var entry))
            {
                values = entry.ToValuesArray();
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }

        public string Get(string name)
        {
            if (_lookupDict.TryGetValue(name, out var entry))
            {
                return entry.ToSingleValue();
            }
            else
            {
                throw new KeyNotFoundException("The specified name was not found in the collection");
            }
        }

        public string[] GetValues(string name)
        {
            if (_lookupDict.TryGetValue(name, out var entry))
            {
                return entry.ToValuesArray();
            }
            else
            {
                throw new KeyNotFoundException("The specified name was not found in the collection");
            }
        }

        public string Get(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

            return _entryList[index].ToSingleValue();
        }

        public string[] GetValues(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

            return _entryList[index].ToValuesArray();
        }

        public void CopyTo(NameValueTuple[] array, int arrayIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

            if (arrayIndex + Count > array.Length)
            {
                throw new ArgumentException("Not enough space in array");
            }

            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = _entryList[i].ToTuple();
            }
        }

        public void Add(string name, string value)
        {
            if (_lookupDict.TryGetValue(name, out var entry))
            {
                entry.Add(value);
            }
            else
            {
                entry = new(name, value);
                _entryList.Add(entry);
                _lookupDict.Add(name, entry);
            }

            _version++;
        }

        public void Add(string name, params string[] values)
        {
            if (values.Length == 0)
            {
                return;
            }

            if (_lookupDict.TryGetValue(name, out var entry))
            {
                entry.Add(values);
            }
            else
            {
                entry = new(name, values);
                _entryList.Add(entry);
                _lookupDict.Add(name, entry);
            }

            _version++;
        }

        public void Add(string name, IEnumerable<string> values)
        {
            if (values is IList<string> list)
            {
                if (_lookupDict.TryGetValue(name, out var entry))
                {
                    entry.Add(list);
                }
                else
                {
                    entry = new(name, list);
                    _entryList.Add(entry);
                    _lookupDict.Add(name, entry);
                }
            }
            else
            {
                var listCopy = values.ToList();

                if (_lookupDict.TryGetValue(name, out var entry))
                {
                    entry.Add(listCopy);
                }
                else
                {
                    entry = new(name, listCopy);
                    _entryList.Add(entry);
                    _lookupDict.Add(name, entry);
                }
            }

            _version++;
        }

        public void Set(string name, string value)
        {
            if (_lookupDict.TryGetValue(name, out var entry))
            {
                entry.Set(value);
            }
            else
            {
                entry = new(name, value);
                _entryList.Add(entry);
                _lookupDict.Add(name, entry);
            }

            _version++;
        }

        public bool Remove(string name, [MaybeNullWhen(false)] out string[] values)
        {
            if (_lookupDict.Remove(name, out var entry))
            {
                _entryList.Remove(entry);
                _version++;

                values = entry.ToValuesArray();
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }

        public bool Remove(string name)
        {
            if (_lookupDict.Remove(name, out var entry))
            {
                _entryList.Remove(entry);
                _version++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemoveAt(int index, out string[] values)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

            var entry = _entryList[index];
            _entryList.RemoveAt(index);
            _lookupDict.Remove(entry._name);
            _version++;

            values = entry.ToValuesArray();
        }

        public void RemoveAt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

            var entry = _entryList[index];
            _entryList.RemoveAt(index);
            _lookupDict.Remove(entry._name);
            _version++;
        }

        public void Clear()
        {
            _lookupDict.Clear();
            _entryList.Clear();

            _version++;
        }

        public Enumerator GetEnumerator() => new(this);

        #region Serialization
        private const string NameValueTuplesKey = "NameValueTuples";
        private const string ComparerKey = "Comparer";
        private const string VersionKey = "Version";

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var tuples = new NameValueTuple[Count];
            for (int i = 0; i < Count; i++)
            {
                tuples[i] = _entryList[i].ToTuple();
            }

            info.AddValue(NameValueTuplesKey, tuples);
            info.AddTypedValue(ComparerKey, Comparer);
            info.AddValue(VersionKey, _version);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            => GetObjectData(info, context);

        private NameValueTuple[]? _deserializedItems;
        private readonly int _realVersion;

        protected NameValueCollection(SerializationInfo info, StreamingContext context)
        {
            _deserializedItems = info.GetValue<NameValueTuple[]>(NameValueTuplesKey);
            var comparer = info.GetValue<IEqualityComparer<string>>(ComparerKey);
            _realVersion = info.GetInt32(ComparerKey);

            if (_deserializedItems == null)
            {
                throw new SerializationException($"{NameValueTuplesKey} is null");
            }

            if (comparer == null)
            {
                throw new SerializationException($"{ComparerKey} is null");
            }

            _lookupDict = new Dictionary<string, Entry>(_deserializedItems.Length, comparer);
            _entryList = new List<Entry>(_deserializedItems.Length);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_deserializedItems == null)
            {
                // no deserialization operations pending
                return;
            }

            try
            {
                for (int i = 0; i < _deserializedItems.Length; i++)
                {
                    var item = _deserializedItems[i];
                    Add(item.Name, item.Values);
                }

                _version = _realVersion;
            }
            finally
            {
                _deserializedItems = null;
            }
        }
        #endregion

        #region Explicit interface implementations
        bool ICollection<NameValueTuple>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        private object? _syncRoot;

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new(), null);
                }

                return _syncRoot;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (index + Count > array.Length)
            {
                throw new ArgumentException("Not enough space in array");
            }

            if (array is NameValueTuple[] tuples)
            {
                for (int i = 0; i < Count; i++)
                {
                    tuples[index + i] = _entryList[i].ToTuple();
                }
            }
            else if (array is object[] objects)
            {
                for (int i = 0; i < Count; i++)
                {
                    objects[index + i] = _entryList[i].ToTuple();
                }
            }
            else
            {
                throw new ArgumentException("Array has invalid type", nameof(array));
            }
        }

        bool ICollection<NameValueTuple>.Contains(NameValueTuple item)
        {
            var itemValues = item.Values;
            return itemValues.Count > 0
                && _lookupDict.TryGetValue(item.Name, out var entry) && entry.ValuesMatch(itemValues);
        }

        void ICollection<NameValueTuple>.Add(NameValueTuple item) => Add(item.Name, item.Values);

        bool ICollection<NameValueTuple>.Remove(NameValueTuple item)
        {
            var itemValues = item.Values;
            if (itemValues.Count > 0
                && _lookupDict.TryGetValue(item.Name, out var entry) && entry.ValuesMatch(itemValues))
            {
                _lookupDict.Remove(item.Name);
                _entryList.Remove(entry);
                _version++;

                return true;
            }
            else
            {
                return false;
            }
        }

        IEnumerator<NameValueTuple> IEnumerable<NameValueTuple>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        private class Entry
        {
            internal readonly string _name;
            internal string _singleValue;
            internal List<string>? _values;

            internal Entry(string name, string value)
            {
                _name = name;
                _singleValue = value;
            }

            internal Entry(string name, IList<string> values)
            {
                _name = name;
                _singleValue = "";
                _values = new(values);
            }

            internal Entry(string name, List<string> values)
            {
                _name = name;
                _singleValue = "";
                _values = values;
            }

            internal bool ValuesMatch(IList<string> values)
            {
                if (_values != null)
                {
                    return Enumerable.SequenceEqual(_values, values, StringComparer.Ordinal);
                }
                else
                {
                    return values.Count == 1 && _singleValue == values[0];
                }
            }

            internal string ToSingleValue()
            {
                if (_values != null)
                {
                    return string.Join(',', _values);
                }
                else
                {
                    return _singleValue;
                }
            }

            internal string[] ToValuesArray()
            {
                if (_values != null)
                {
                    return _values.ToArray();
                }
                else
                {
                    return new string[] { _singleValue };
                }
            }

            internal NameValueTuple ToTuple()
            {
                if (_values != null)
                {
                    return new NameValueTuple(_name, _values);
                }
                else
                {
                    return new NameValueTuple(_name, _singleValue);
                }
            }

            internal void Add(string value)
            {
                if (_values == null)
                {
                    _values = new() { _singleValue, value };
                    _singleValue = "";
                }
                else
                {
                    _values.Add(value);
                }
            }

            internal void Add(IList<string> values)
            {
                if (_values == null)
                {
                    _values = new(values.Count + 1) { _singleValue };
                    _singleValue = "";
                }

                _values.AddRange(values);
            }

            internal void Set(string value)
            {
                _values = null;
                _singleValue = value;
            }
        }

        [Serializable]
        public struct Enumerator : IEnumerator<NameValueTuple>, IEnumerator
        {
            private readonly NameValueCollection collection;
            private readonly int version;
            private int index;

            internal Enumerator(NameValueCollection collection)
            {
                this.collection = collection;
                version = collection._version;
                index = -1;
            }

            public readonly NameValueTuple Current
            {
                get
                {
                    if (collection._version != version)
                    {
                        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute");
                    }

                    if (index < 0)
                    {
                        throw new InvalidOperationException("Enumeration has not started yet; call MoveNext");
                    }
                    else if (index >= collection.Count)
                    {
                        throw new InvalidOperationException("Enumeration has already finished");
                    }

                    return collection._entryList[index].ToTuple();
                }
            }

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (collection._version != version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute");
                }

                if (index < collection.Count)
                {
                    index++;
                    return true;
                }
                else
                {
                    //index = collection.Count;
                    return false;
                }
            }

            public void Reset()
            {
                if (collection._version != version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute");
                }

                index = -1;
            }

            readonly void IDisposable.Dispose() { }
        }
    }
}
