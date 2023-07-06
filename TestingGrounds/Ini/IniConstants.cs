namespace TestingGrounds.Ini
{
    public static class IniConstants
    {
        public const string CommentPrefix = ";";
        public const string PropertyDelimiter = "=";

        public static IEnumerable<string> GetDefaultCommentPrefixes()
        {
            yield return CommentPrefix;
            yield return "#";
        }

        public static IEnumerable<string> GetStrictCommentPrefixes()
        {
            yield return CommentPrefix;
        }

        public static IEnumerable<string> GetDefaultPropertyDelimiters()
        {
            yield return PropertyDelimiter;
            yield return ":";
        }

        public static IEnumerable<string> GetStrictPropertyDelimiters()
        {
            yield return PropertyDelimiter;
        }
    }
}
