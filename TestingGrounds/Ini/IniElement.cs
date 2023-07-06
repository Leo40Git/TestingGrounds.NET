namespace TestingGrounds.Ini
{
    public abstract class IniElement
    {
        private List<string>? comments;

        public bool HasComments => comments != null && comments.Count > 0;

        public List<string> Comments
        {
            get
            {
                if (comments == null)
                {
                    Interlocked.CompareExchange(ref comments, new List<string>(), null);
                }

                return comments;
            }
        }

        public string? InlineComment { get; set; }

        protected void CopyCommentsTo(IniElement other)
        {
            if (comments != null && comments.Count > 0)
            {
                other.comments = new List<string>(comments);
            }

            other.InlineComment = InlineComment;
        }

        public virtual void Clear()
        {
            if (comments != null)
            {
                comments.Clear();
            }

            InlineComment = null;
        }
    }
}
