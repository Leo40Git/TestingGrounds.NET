using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TestingGrounds.Ini
{
    public sealed class IniPropertyCollection : ICollection<IniProperty>, ICollection
    {
        private readonly LinkedList<IniProperty> list;
        private readonly Dictionary<string, LinkedListNode<IniProperty>> lookupDict;

        public IniPropertyCollection(IEqualityComparer<string> comparer)
        {
            list = new LinkedList<IniProperty>();
            lookupDict = new Dictionary<string, LinkedListNode<IniProperty>>(comparer);
        }

        public IniPropertyCollection(bool ignoreCase) : this(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public IniPropertyCollection() : this(StringComparer.OrdinalIgnoreCase) { }

        public IniPropertyCollection(IniPropertyCollection source, IEqualityComparer<string> comparer)
        {
            list = new LinkedList<IniProperty>();
            lookupDict = new Dictionary<string, LinkedListNode<IniProperty>>(source.Count, comparer);

            foreach (var property in source)
            {
                Add(property.Clone());
            }
        }

        public IniPropertyCollection(IniPropertyCollection source, bool ignoreCase)
            : this(source, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public IniPropertyCollection(IniPropertyCollection source) : this(source, source.Comparer) { }

        public int Count => list.Count;

        public IEqualityComparer<string> Comparer => lookupDict.Comparer;

        public IniProperty this[string name] => lookupDict[name].Value;

        public bool TryGetProperty(string name, [MaybeNullWhen(false)] out IniProperty property)
        {
            if (lookupDict.TryGetValue(name, out var node))
            {
                property = node.Value;
                return true;
            }
            else
            {
                property = null;
                return false;
            }
        }

        public bool Contains(IniProperty property) => property.Container == this;

        public bool Contains(string name) => lookupDict.ContainsKey(name);

        public void Add(IniProperty property)
        {
            if (property.Container != null)
            {
                throw new ArgumentException("Property belongs to another collection", nameof(property));
            }

            if (lookupDict.ContainsKey(property.Name))
            {
                throw new ArgumentException("Property with equivalent name already exists in collection", nameof(property));
            }

            property.Container = this;
            lookupDict[property.Name] = list.AddLast(property);
        }

        public IniProperty Add(string name, string value)
        {
            IniProperty.ThrowIfInvalidName(name);

            if (lookupDict.ContainsKey(name))
            {
                throw new ArgumentException("Property with equivalent name already exists in collection", nameof(name));
            }

            var property = new IniProperty(name, value)
            {
                Container = this
            };
            lookupDict[name] = list.AddLast(property);
            return property;
        }

        public IniProperty Set(string name, string value, out string? lastValue)
        {
            IniProperty.ThrowIfInvalidName(name);

            if (TryGetProperty(name, out var property))
            {
                lastValue = property.Value;
                property.Value = value;
            }
            else
            {
                lastValue = null;
                property = Add(name, value);
            }

            return property;
        }

        public IniProperty Set(string name, string value) => Set(name, value, out _);

        internal void ChangePropertyName(IniProperty property, string newName)
        {
            if (lookupDict.Comparer.Equals(property.Name, newName))
            {
                // nothing to do
                return;
            }

            if (lookupDict.ContainsKey(newName))
            {
                throw new ArgumentException("Property with equivalent name already exists in collection", nameof(newName));
            }

            lookupDict.Remove(property.Name, out var node);
            lookupDict[newName] = node!;
        }

        public bool Remove(IniProperty property)
        {
            if (property.Container == this
                && lookupDict.TryGetValue(property.Name, out var node) && node.Value == property)
            {
                property.Container = null;
                list.Remove(node);
                lookupDict.Remove(property.Name);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Remove(string name)
        {
            if (lookupDict.TryGetValue(name, out var node))
            {
                node.Value.Container = null;
                list.Remove(node);
                lookupDict.Remove(name);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear()
        {
            var current = list.First;
            while (current != null)
            {
                current.Value.Container = null;
                current = current.Next;
            }

            list.Clear();
            lookupDict.Clear();
        }

        public void CopyTo(IniProperty[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public IEnumerator<IniProperty> GetEnumerator() => list.GetEnumerator();

        #region Explicit interface implementations
        bool ICollection<IniProperty>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        private object? _syncRoot;

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }

                return _syncRoot;
            }
        }

        void ICollection.CopyTo(Array array, int index) => ((ICollection)list).CopyTo(array, index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
