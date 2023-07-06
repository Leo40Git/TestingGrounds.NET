using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TestingGrounds.Ini
{
    public sealed class IniSection : IniElement
    {
        public static void ThrowIfInvalidName(string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
        {
            if (name.Length == 0)
            {
                throw new ArgumentException("Property name cannot be empty", paramName);
            }
        }

        private string name;

        internal IniSection(string name, IniPropertyCollection properties)
        {
            this.name = name;
            Properties = properties;
        }

        public IniSection(string name, IEqualityComparer<string> propertyComparer)
        {
            ThrowIfInvalidName(name);

            this.name = name;
            Properties = new IniPropertyCollection(propertyComparer);
        }

        public IniSection(string name, bool ignoreCase)
            : this(name, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { }

        public IniSection(string name) : this(name, StringComparer.OrdinalIgnoreCase) { }

        public string Name
        {
            get { return name; }

            set
            {
                ThrowIfInvalidName(name);

                Container?.ChangeSectionName(this, value);
                name = value;
            }
        }

        public IniPropertyCollection Properties { get; init; }

        public IniSectionCollection? Container { get; internal set; }

        public string this[string propertyName]
        {
            get
            {
                return Properties[propertyName].Value;
            }

            set
            {
                IniProperty.ThrowIfInvalidName(propertyName);
                Properties.Set(propertyName, value);
            }
        }

        public void AddComment(string propertyName, string comment)
            => Properties[propertyName].Comments.Add(comment);

        public void AddComment(string propertyName,
            [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string commentFormat, params object[] args)
            => Properties[propertyName].Comments.Add(string.Format(commentFormat, args));

        public void AddComments(string propertyName, IEnumerable<string> comments)
            => Properties[propertyName].Comments.AddRange(comments);

        public void AddComments(string propertyName, params string[] comments)
            => Properties[propertyName].Comments.AddRange(comments);

        public override void Clear()
        {
            Properties.Clear();
            base.Clear();
        }

        public IniSection Clone(IEqualityComparer<string> propertyComparer)
        {
            var clone = new IniSection(name, new IniPropertyCollection(propertyComparer));
            CopyCommentsTo(clone);
            return clone;
        }

        public IniSection Clone(bool ignoreCase) => Clone(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        public IniSection Clone()
        {
            var clone = new IniSection(name, new IniPropertyCollection(Properties));
            CopyCommentsTo(clone);
            return clone;
        }
    }
}
