using System.Collections.ObjectModel;
using System.Text;

namespace TestingGrounds.Collections
{
    [Serializable]
    public readonly struct NameValueTuple
    {
        private readonly string name;
        private readonly string[] values;

        public NameValueTuple(string name, params string[] values)
        {
            this.name = name;
            this.values = (string[])values.Clone();
        }

        public NameValueTuple(string name, IEnumerable<string> values)
        {
            this.name = name;
            this.values = values.ToArray();
        }

        public string Name => name;

        public ReadOnlyCollection<string> Values => new(values);

        public override string ToString()
        {
            switch (values.Length)
            {
                case 0: return $"{{'{name}'}}";
                case 1: return $"{{'{name}'='{values[0]}'}}";

                default:
                    var sb = new StringBuilder("{'")
                        .Append(name)
                        .Append("'=");

                    for (int i = 0; i < values.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        sb.Append('\'').Append(values[i]).Append('\'');
                    }

                    return sb.Append('}').ToString();
            }
        }
    }
}
