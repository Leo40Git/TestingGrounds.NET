using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TestingGrounds.Ini
{
    public sealed class IniSectionCollection : ICollection<IniSection>, ICollection
    {
        private readonly LinkedList<IniSection> list;
        private readonly Dictionary<string, LinkedListNode<IniSection>> lookupDict;

        public IniSectionCollection(IEqualityComparer<string> sectionComparer, IEqualityComparer<string> propertyComparer)
        {
            list = new LinkedList<IniSection>();
            lookupDict = new Dictionary<string, LinkedListNode<IniSection>>(sectionComparer);
            PropertyComparer = propertyComparer;
        }

        public IniSectionCollection(IEqualityComparer<string> comparer) : this(comparer, comparer) { }

        public IniSectionCollection(bool ignoreCase)
            : this(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public IniSectionCollection() : this(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase) { }

        public IniSectionCollection(IniSectionCollection source,
            IEqualityComparer<string> sectionComparer, IEqualityComparer<string> propertyComparer)
        {
            list = new LinkedList<IniSection>();
            lookupDict = new Dictionary<string, LinkedListNode<IniSection>>(source.Count, sectionComparer);
            PropertyComparer = propertyComparer;

            foreach (var section in source)
            {
                Add(section.Clone(propertyComparer));
            }
        }

        public IniSectionCollection(IniSectionCollection source, IEqualityComparer<string> comparer)
            : this(source, comparer, comparer) { }

        public IniSectionCollection(IniSectionCollection source, bool ignoreCase)
            : this(source, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public IniSectionCollection(IniSectionCollection source)
        {
            list = new LinkedList<IniSection>();
            lookupDict = new Dictionary<string, LinkedListNode<IniSection>>(source.Count, source.SectionComparer);
            PropertyComparer = source.PropertyComparer;

            foreach (var section in source)
            {
                Add(section.Clone());
            }
        }

        public int Count => list.Count;

        public IEqualityComparer<string> SectionComparer => lookupDict.Comparer;

        public IEqualityComparer<string> PropertyComparer { get; init; }

        public IniSection this[string name] => lookupDict[name].Value;

        public bool TryGetSection(string name, [MaybeNullWhen(false)] out IniSection section)
        {
            if (lookupDict.TryGetValue(name, out var node))
            {
                section = node.Value;
                return true;
            }
            else
            {
                section = null;
                return false;
            }
        }

        public bool Contains(IniSection section) => section.Container == this;

        public bool Contains(string name) => lookupDict.ContainsKey(name);

        public void Add(IniSection section)
        {
            if (section.Container != null)
            {
                throw new ArgumentException("Section belongs to another collection", nameof(section));
            }

            if (lookupDict.ContainsKey(section.Name))
            {
                throw new ArgumentException("Section with equivalent name already exists in collection", nameof(section));
            }

            section.Container = this;
            lookupDict[section.Name] = list.AddLast(section);
        }

        public IniSection Add(string name)
        {
            IniSection.ThrowIfInvalidName(name);

            if (lookupDict.ContainsKey(name))
            {
                throw new ArgumentException("Section with equivalent name already exists in collection", nameof(name));
            }

            var section = new IniSection(name)
            {
                Container = this
            };
            lookupDict[name] = list.AddLast(section);
            return section;
        }
        
        public IniSection GetOrAdd(string name, out bool added)
        {
            IniSection.ThrowIfInvalidName(name);

            if (TryGetSection(name, out var section))
            {
                added = false;
            }
            else
            {
                Add(section = new IniSection(name, PropertyComparer));
                added = true;
            }

            return section;
        }

        public IniSection GetOrAdd(string name) => GetOrAdd(name, out _);

        internal void ChangeSectionName(IniSection section, string newName)
        {
            if (lookupDict.Comparer.Equals(section.Name, newName))
            {
                // nothing to do
                return;
            }

            if (lookupDict.ContainsKey(newName))
            {
                throw new ArgumentException("Section with equivalent name already exists in collection", nameof(newName));
            }

            lookupDict.Remove(section.Name, out var node);
            lookupDict[newName] = node!;
        }

        public bool Remove(IniSection section)
        {
            if (section.Container == this
                && lookupDict.TryGetValue(section.Name, out var node) && node.Value == section)
            {
                section.Container = null;
                list.Remove(node);
                lookupDict.Remove(section.Name);
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

        public void CopyTo(IniSection[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public IEnumerator<IniSection> GetEnumerator() => list.GetEnumerator();

        #region Explicit interface implementations
        bool ICollection<IniSection>.IsReadOnly => false;

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
