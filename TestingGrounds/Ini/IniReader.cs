using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestingGrounds.Ini
{
    public static class IniReader
    {
        public static IniFile Read(TextReader reader, IniReaderSettings settings)
        {
            var file = new IniFile(settings.SectionComparer, settings.PropertyComparer);
            bool inGlobalProperties = true;
            IniSection? currentSection = null;

            List<string>? comments = null;

            void AddComment(string comment)
            {
                comments ??= new List<string>();
                comments.Add(comment);
            }

            void ApplyComments(IniElement el)
            {
                if (comments != null && comments.Count > 0)
                {
                    el.Comments.AddRange(comments);
                    comments.Clear();
                }
            }

            int lineNum = 0;
            string? line;

            void ReportError(string errorMessage, int linePos)
            {
                if (settings.ReadErrorCallback?.Invoke(ref errorMessage, line!, settings.LineNumberOffset + lineNum, linePos) ?? true)
                {
                    // TODO create special exception type?
                    throw new IOException(errorMessage);
                }
            }

            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;

                string trimmedLine = line.Trim();

                if (inGlobalProperties || currentSection != null)
                {
                    if (trimmedLine.Length == 0)
                    {
                        if (settings.PreserveComments)
                        {
                            if (inGlobalProperties)
                            {
                                if (comments != null && comments.Count > 0)
                                {
                                    file.LeadingComments.AddRange(comments);
                                    file.LeadingComments.Add(line);
                                    comments.Clear();
                                }
                            }
                            else if (currentSection != null)
                            {
                                AddComment(line);
                            }
                        }

                        continue;
                    }
                    else
                    {
                        foreach (string commentPrefix in settings.CommentPrefixes)
                        {
                            if (trimmedLine.StartsWith(commentPrefix))
                            {
                                if (settings.PreserveComments)
                                {
                                    AddComment(line);
                                }

                                continue;
                            }
                        }
                    }
                }

                string? inlineComment = null;
                if (settings.OptionalFeatures.HasFlag(IniOptionalFeatures.InlineComments))
                {
                    int firstCommentPrefixIndex = -1;
                    foreach (string commentPrefix in settings.CommentPrefixes)
                    {
                        int index = trimmedLine.IndexOf(commentPrefix);
                        if (index >= 0 && index < firstCommentPrefixIndex)
                        {
                            firstCommentPrefixIndex = index;
                        }
                    }

                    if (firstCommentPrefixIndex > 0)
                    {
                        trimmedLine = trimmedLine[..firstCommentPrefixIndex];

                        if (settings.PreserveComments)
                        {
                            inlineComment = line[(line.Length - trimmedLine.Length + firstCommentPrefixIndex)..];
                        }
                    }
                }

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    // section
                    // TODO escape sequences??
                    string sectionName = trimmedLine[1..^2];
                    if (currentSection != null
                        && settings.RelativeNestingPrefix != null && sectionName.StartsWith(settings.RelativeNestingPrefix))
                    {
                        sectionName = currentSection.Name + sectionName;
                    }

                    currentSection = file.Sections.GetOrAdd(sectionName, out bool isNew);
                    inGlobalProperties = false;

                    if (!isNew)
                    {
                        switch (settings.DuplicateSectionCallback?.Invoke(currentSection, comments, inlineComment)
                            ?? settings.DuplicateSectionBehavior)
                        {
                            case IniDuplicateElementBehavior.Error:
                                ReportError("Duplicate section '" + sectionName + "'", line.Length - trimmedLine.Length + 1);
                                // clear current section to ignore everything up to the next section declaration
                                currentSection = null;
                                continue;
                            case IniDuplicateElementBehavior.Ignore:
                                // clear current section to ignore everything up to the next section declaration
                                currentSection = null;
                                continue;
                            case IniDuplicateElementBehavior.Replace:
                                currentSection.Clear();
                                // move to end
                                file.Sections.Remove(currentSection);
                                file.Sections.Add(currentSection);
                                break;
                            case IniDuplicateElementBehavior.Merge:
                                break;
                            case IniDuplicateElementBehavior.Continue:
                                continue;
                        }
                    }

                    ApplyComments(currentSection);
                    currentSection.InlineComment = inlineComment;
                    continue;
                }

                if (inGlobalProperties || currentSection != null)
                {
                    string? propertyDelimiter = null;
                    int propertyDelimiterIndex = -1;

                    foreach (string possibleDelimiter in settings.PropertyDelimiters)
                    {
                        propertyDelimiterIndex = trimmedLine.IndexOf(possibleDelimiter);
                        if (propertyDelimiterIndex >= 0)
                        {
                            propertyDelimiter = possibleDelimiter;
                            break;
                        }
                    }

                    if (propertyDelimiter != null)
                    {
                        // property
                        // TODO escape sequences and line continuation
                        // TODO quoted values
                        string propertyName = trimmedLine[0..propertyDelimiterIndex].Trim();
                        string propertyValue = trimmedLine[(propertyDelimiterIndex + propertyDelimiter.Length)..].Trim();

                        var propertyCollection = currentSection?.Properties;
                        if (propertyCollection == null)
                        {
                            if (settings.OptionalFeatures.HasFlag(IniOptionalFeatures.GlobalProperties))
                            {
                                propertyCollection = file.GlobalProperties;
                            }
                            else
                            {
                                ReportError("Property outside of section", line.Length - trimmedLine.Length + 1);
                                continue;
                            }
                        }

                        var property = propertyCollection.Set(propertyName, propertyValue, out string? propertyLastValue);
                        if (propertyLastValue != null)
                        {
                            // reset property value for callback
                            property.Value = propertyLastValue;

                            switch (settings.DuplicatePropertyCallback?.Invoke(property, propertyValue, comments, inlineComment)
                                ?? settings.DuplicatePropertyBehavior)
                            {
                                case IniDuplicateElementBehavior.Error:
                                    ReportError("Duplicate property '" + propertyName + "'", line.Length - trimmedLine.Length + 1);
                                    continue;
                                case IniDuplicateElementBehavior.Ignore:
                                case IniDuplicateElementBehavior.Continue:
                                    continue;
                                case IniDuplicateElementBehavior.Replace:
                                    property.Value = propertyValue;
                                    property.Clear();
                                    // move to end
                                    propertyCollection.Remove(property);
                                    propertyCollection.Add(property);
                                    break;
                                case IniDuplicateElementBehavior.Merge:
                                    // TODO use DuplicatePropertyMergeHandler
                                    property.Value
                                        = propertyLastValue + settings.DuplicatePropertyMergeValueDelimiter + propertyValue;
                                    break;
                            }
                        }

                        ApplyComments(property);
                        property.InlineComment = inlineComment;
                        continue;
                    }
                }

                ReportError("Unknown line format", 0);
            }

            return file;
        }
    }

    public struct IniReaderSettings
    {
        public IniReaderSettings() { }

        public IniReadErrorCallback? ReadErrorCallback { get; set; }

        public int LineNumberOffset { get; set; } = 0;

        public IEnumerable<string> PropertyDelimiters { get; set; } = IniConstants.GetDefaultPropertyDelimiters();

        public IEnumerable<string> CommentPrefixes { get; set; } = IniConstants.GetDefaultCommentPrefixes();

        public bool PreserveComments { get; set; } = true;

        public IniOptionalFeatures OptionalFeatures { get; set; } = IniOptionalFeatures.None;

        public string? RelativeNestingPrefix { get; set; } = null;

        public IEqualityComparer<string> SectionComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        public IEqualityComparer<string> PropertyComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        public IEqualityComparer<string> Comparer
        {
            set
            {
                SectionComparer = value;
                PropertyComparer = value;
            }
        }

        public bool IgnoreCase
        {
            set { Comparer = value ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal; }
        }

        public IniDuplicateElementBehavior DuplicateSectionBehavior { get; set; } = IniDuplicateElementBehavior.Merge;

        public IniDuplicateSectionCallback? DuplicateSectionCallback { get; set; }

        public IniDuplicateElementBehavior DuplicatePropertyBehavior { get; set; } = IniDuplicateElementBehavior.Merge;

        public IniDuplicatePropertyCallback? DuplicatePropertyCallback { get; set; }

        public string DuplicatePropertyMergeValueDelimiter { get; set; } = ",";

        public IIniDuplicatePropertyMergeHandler? DuplicatePropertyMergeHandler { get; set; }
    }

    public delegate bool IniReadErrorCallback(ref string errorMessage, string line, int lineNum, int linePos);

    [Flags]
    public enum IniOptionalFeatures
    {
        None = 0,
        InlineComments   = 1 << 0,
        GlobalProperties = 1 << 1,
        QuotedValues     = 1 << 2,
        EscapeCharacters = 1 << 3,
        LineContinuation = 1 << 4,
    }

    public enum IniDuplicateElementBehavior
    {
        Error,
        Ignore,
        Replace,
        Merge,
        Continue,
    }

    public delegate IniDuplicateElementBehavior IniDuplicateSectionCallback(IniSection section,
        List<string>? newComments, string? newInlineComment);

    public delegate IniDuplicateElementBehavior IniDuplicatePropertyCallback(IniProperty property, string newValue,
        List<string>? newComments, string? newInlineComment);

    public interface IIniDuplicatePropertyMergeHandler
    {
        // returns true if EndMergeProperty needs to be called
        bool BeginMergeProperty(IniProperty property, StringBuilder valueBuilder);
        void MergeProperty(IniProperty property, StringBuilder valueBuilder, string newValue,
            List<string>? newComments, string? newInlineComment);
        void EndMergeProperty(IniProperty property, StringBuilder valueBuilder);
    }
}
