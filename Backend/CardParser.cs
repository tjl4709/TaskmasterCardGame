using System;
using System.Collections.Generic;
using System.IO;

namespace Backend
{
    public enum FormatTypes
    {
        WholeNumber,
        DecimalNumber,
        Word,
        Letter,
        Phrase
    }

    public abstract class ICardFormatter
    {
        public const string OPENING = "<<", CLOSING = ">>";

        public virtual string Format(string rawText)
        {
            int closing_index = 0, opening_index;
            string formattedText = "", formatString;
            FormatTypes formatType;
            rawText = CLOSING + rawText;  // so we don't need special logic for start of string
            while ((opening_index = rawText.IndexOf(OPENING, closing_index)) != -1) {
                formattedText += rawText.Substring(closing_index + 2, opening_index - closing_index - 2);
                closing_index = rawText.IndexOf(CLOSING, opening_index);
                if (closing_index == -1)
                    throw new FormatException($"Formatter string at character {opening_index} was not closed.");
                formatString = rawText.Substring(opening_index + 2, closing_index - opening_index - 2);
                formatType = Parse(ref formatString);
                formattedText += Prompt(formatString, formatType);
            }
            return formattedText + rawText.Substring(closing_index + 2);
        }

        public abstract string Prompt(string description, FormatTypes type);

        // <<description:t>>
        protected FormatTypes Parse(ref string formatString)
        {
            FormatTypes type = FormatTypes.Phrase;
            int i = formatString.LastIndexOf(':');
            if (i == formatString.Length - 2) {
                switch (formatString.Substring(i + 1, 1)[0]) {
                    case 'd':
                        type = FormatTypes.DecimalNumber;
                        break;
                    case 'i':
                        type = FormatTypes.WholeNumber;
                        break;
                    case 'w':
                        type = FormatTypes.Word;
                        break;
                    case 'l':
                        type = FormatTypes.Letter;
                        break;
                    case 'p':
                        type = FormatTypes.Phrase;
                        break;
                    default:
                        throw new FormatException($"Unknown formatter type: {formatString.Substring(i, 1)}");
                }
                formatString = formatString.Substring(0, i);
            }
            return type;
        }

        protected bool Verify(string value, FormatTypes type)
        {
            switch (type) {
                case FormatTypes.WholeNumber:
                    return int.TryParse(value, out _);
                case FormatTypes.DecimalNumber:
                    return float.TryParse(value, out _);
                case FormatTypes.Word:
                    return Array.TrueForAll(value.ToCharArray(), char.IsLetter);
                case FormatTypes.Letter:
                    return value.Length == 1 && char.IsLetter(value[0]);
                case FormatTypes.Phrase:
                default:
                    return true;
            }
        }
    }

    public class CardParser<T> where T : ICard, new()
    {
        public readonly char[] DELIMITER = "\t".ToCharArray();
        
        public string FilePath { get; protected set; }
        protected List<T> m_cards;
        public IReadOnlyList<T> Cards { get { return m_cards.AsReadOnly();} }

        public CardParser(string filepath)
        {
            FilePath = filepath;
            m_cards = new List<T>();
        }

        public List<T> Parse()
        {
            if (m_cards.Count > 0)
                m_cards.Clear();
            Exception ex = null;
            TextReader file = null;
            string line;
            string[] fields;
            int i = 0;

            try {
                file = new StreamReader(FilePath);
                for (; (line = file.ReadLine()) != null; ++i) {
                    if (line.Length > 0) {
                        fields = line.Split(DELIMITER);
                        m_cards.Add(ICard.Create<T>(fields));
                    }
                }
            } catch (FormatException e) {
                ex = new FormatException($"\"{FilePath}\" line {i + 1}: {e.Message}");
            } catch (Exception e) {
                ex = e;
            } finally {
                if (file != null)
                    file.Close();
            }

            if (ex != null)
                throw ex;
            return new List<T>(m_cards);
        }
    }
}
