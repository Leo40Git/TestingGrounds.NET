using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestingGrounds.Ini
{
    public sealed class IniProperty : IniElement
    {
        public static void ThrowIfInvalidName(string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
        {
            if (name.Length == 0)
            {
                throw new ArgumentException("Property name cannot be empty", paramName);
            }
        }

        private string name;

        public IniProperty(string name, string value)
        {
            ThrowIfInvalidName(name);

            this.name = name;
            Value = value;
        }

        public string Name
        {
            get { return name; }

            set
            {
                ThrowIfInvalidName(name);

                Container?.ChangePropertyName(this, value);
                name = value;
            }
        }

        public string Value { get; set; }

        public IniPropertyCollection? Container { get; internal set; }

        public override void Clear()
        {
            Value = "";
            base.Clear();
        }

        public IniProperty Clone()
        {
            var clone = new IniProperty(name, Value);
            CopyCommentsTo(clone);
            return clone;
        }
    }
}
