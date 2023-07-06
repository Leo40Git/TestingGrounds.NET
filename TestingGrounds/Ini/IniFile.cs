using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TestingGrounds.Ini
{
    public sealed class IniFile
    {
        private List<string>? leadingComments, trailingComments;
        private IniPropertyCollection? globalProperties;

        public IniFile(IEqualityComparer<string> sectionComparer, IEqualityComparer<string> propertyComparer)
        {
            Sections = new IniSectionCollection(sectionComparer, propertyComparer);
        }

        public IniFile(IEqualityComparer<string> comparer) : this(comparer, comparer) { }

        public IniFile(bool ignoreCase) : this(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public IniFile() : this(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase) { }

        public bool HasLeadingComments => leadingComments != null && leadingComments.Count > 0;

        public List<string> LeadingComments
        {
            get
            {
                if (leadingComments == null)
                {
                    Interlocked.CompareExchange(ref leadingComments, new List<string>(), null);
                }

                return leadingComments;
            }
        }

        public bool HasGlobalProperties => globalProperties != null && globalProperties.Count > 0;

        public IniPropertyCollection GlobalProperties
        {
            get
            {
                if (globalProperties == null)
                {
                    Interlocked.CompareExchange(ref globalProperties, new IniPropertyCollection(Sections.PropertyComparer), null);
                }

                return globalProperties;
            }
        }

        public IniSectionCollection Sections { get; init; }

        public bool HasTrailingComments => trailingComments != null && trailingComments.Count > 0;

        public List<string> TrailingComments
        {
            get
            {
                if (trailingComments == null)
                {
                    Interlocked.CompareExchange(ref trailingComments, new List<string>(), null);
                }

                return trailingComments;
            }
        }

        public char PathSeparator { get; set; } = '.';

        public string this[string sectionName, string propertyName]
        {
            get
            {
                if (HasGlobalProperties && sectionName.Length == 0)
                {
                    return GlobalProperties[propertyName].Value;
                }
                else
                {
                    return Sections[sectionName].Properties[propertyName].Value;
                }
            }

            set
            {
                IniProperty.ThrowIfInvalidName(propertyName);

                if (sectionName.Length == 0)
                {
                    GlobalProperties.Set(propertyName, value);
                }
                else
                {
                    Sections.GetOrAdd(sectionName).Properties.Set(propertyName, value);
                }
            }
        }

        public string this[string path]
        {
            get { return GetProperty(path).Value; }
            set
            {
                ParsePath(path, out string sectionName, out string propertyName);
                if (propertyName.Length == 0)
                {
                    throw new ArgumentException("Invalid path: property name part is empty", nameof(path));
                }

                if (sectionName.Length == 0)
                {
                    GlobalProperties.Set(propertyName, value);
                }
                else
                {
                    Sections.GetOrAdd(sectionName).Properties.Set(sectionName, value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParsePath(string path, out string sectionName, out string propertyName)
        {
            int propertyNameStart = path.LastIndexOf(PathSeparator) + 1;
            if (propertyNameStart > 0)
            {
                sectionName = path[0..(propertyNameStart - 1)];
                propertyName = path[propertyNameStart..];
            }
            else
            {
                sectionName = "";
                propertyName = path;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IniProperty GetProperty(string path)
        {
            ParsePath(path, out string sectionName, out string propertyName);

            try
            {
                if (HasGlobalProperties && sectionName.Length == 0)
                {
                    return GlobalProperties[propertyName];
                }
                else
                {
                    return Sections[sectionName].Properties[propertyName];
                }
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException("Couldn't find property at specified path");
            }
        }

        public bool TryGetValue(string sectionName, string propertyName, [MaybeNullWhen(false)] out string value)
        {
            if (HasGlobalProperties && sectionName.Length == 0)
            {
                if (GlobalProperties.TryGetProperty(propertyName, out var property))
                {
                    value = property.Value;
                    return true;
                }
            }
            else if (Sections.TryGetSection(sectionName, out var section))
            {
                if (section.Properties.TryGetProperty(propertyName, out var property))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetProperty(string path, [MaybeNullWhen(false)] out IniProperty property)
        {
            ParsePath(path, out string sectionName, out string propertyName);

            if (HasGlobalProperties && sectionName.Length == 0)
            {
                return GlobalProperties.TryGetProperty(propertyName, out property);
            }
            else if (Sections.TryGetSection(sectionName, out var section))
            {
                return section.Properties.TryGetProperty(propertyName, out property);
            }
            else
            {
                property = null;
                return false;
            }
        }

        public bool TryGetValue(string path, [MaybeNullWhen(false)] out string value)
        {
            if (TryGetProperty(path, out var property))
            {
                value = property.Value;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public void AddSectionComment(string sectionName, string comment)
            => Sections[sectionName].Comments.Add(comment);

        public void AddSectionComment(string sectionName,
            [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string commentFormat, params object[] args)
            => Sections[sectionName].Comments.Add(string.Format(commentFormat, args));

        public void AddSectionComments(string sectionName, IEnumerable<string> comments)
            => Sections[sectionName].Comments.AddRange(comments);

        public void AddSectionComments(string sectionName, params string[] comments)
            => Sections[sectionName].Comments.AddRange(comments);

        public void AddPropertyComment(string sectionName, string propertyName, string comment)
            => Sections[sectionName].Properties[propertyName].Comments.Add(comment);

        public void AddPropertyComment(string sectionName, string propertyName,
            [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string commentFormat, params object[] args)
            => Sections[sectionName].Properties[propertyName].Comments.Add(string.Format(commentFormat, args));

        public void AddPropertyComments(string sectionName, string propertyName, IEnumerable<string> comments)
            => Sections[sectionName].Properties[propertyName].Comments.AddRange(comments);

        public void AddPropertyComments(string sectionName, string propertyName, params string[] comments)
            => Sections[sectionName].Properties[propertyName].Comments.AddRange(comments);

        public void AddPropertyComment(string path, string comment)
            => GetProperty(path).Comments.Add(comment);

        public void AddPropertyComment(string path,
            [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string commentFormat, params object[] args)
            => GetProperty(path).Comments.Add(string.Format(commentFormat, args));

        public void AddPropertyComments(string path, IEnumerable<string> comments)
            => GetProperty(path).Comments.AddRange(comments);

        public void AddPropertyComments(string path, params string[] comments)
            => GetProperty(path).Comments.AddRange(comments);
    }
}
