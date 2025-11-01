using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Backend
{
    public enum CardType
    {
        Unknown,
        PrizeTask,
        SecretTask,
        Task,
        Restriction,
        FinalTask
    }

    public class CardMetaData
    {
        public int ID { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public CardType CardType;

        public CardMetaData(int id, DateTime created, DateTime modified, CardType cardType = CardType.Unknown)
        {
            ID = id;
            Created = created;
            LastModified = modified;
            CardType = cardType;
        }

        // used when constructing the meta data from the SQL database
        public CardMetaData(int id, long created, long modifed, CardType cardType = CardType.Unknown)
            : this(id, DateTime.FromBinary(created), DateTime.FromBinary(modifed), cardType)
        { }
        
        // used when constructing the meta data from the text database
        public CardMetaData(DateTime created, DateTime modifed, CardType cardType = CardType.Unknown)
            : this(0, created, modifed, cardType)
        { }

        // used when constructing the meta data for a new card
        public CardMetaData(DateTime created, CardType cardType)
            : this(0, created, created, cardType)
        { }

        // used when coping/duplicating meta data
        public CardMetaData(CardMetaData original)
            : this(original.ID, original.Created, original.LastModified, original.CardType)
        { }

        public CardMetaData Duplicate()
        {
            return new CardMetaData(this);
        }
    }

    public abstract class ICard
    {
        public CardMetaData MetaData { get; set; }
        protected string[] m_rawData;


        public static T Create<T>(string[] rawData, CardMetaData metaData = null) where T : ICard, new()
        {
            T card = new T();
            card.Assign(rawData);
            card.MetaData = metaData;
            return card;
        }
        public static T Create<T>(T originalCard, CardMetaData metaData = null) where T : ICard, new()
        {
            T newCard = new T();
            newCard.Copy(originalCard);
            if (metaData != null)
                newCard.MetaData = metaData;
            return newCard;
        }

        public void Assign(string[] data)
        {
            if (VerifyRawData(data)) {
                m_rawData = data;
            }
        }

        protected abstract bool VerifyRawData(string[] data);
        public abstract void Format(ICardFormatter formatter);

        public virtual void Copy(ICard original)
        {
            Assign(original.m_rawData);
            MetaData = original.MetaData?.Duplicate();
        }

        public override bool Equals(object obj)
        {
            if (!obj.GetType().Equals(GetType()))
                return false;
            var card = obj as ICard;
            // if both cards have meta data, they are the same if their IDs match
            if (MetaData != null && card.MetaData != null)
                return MetaData.ID == card.MetaData.ID;
            // other wise, compare the raw data: if they're both null, then they match,
            // if only one is null, they don't match, otherwise compare the arrays
            if (m_rawData == null)
                return card.m_rawData == null;
            if (card.m_rawData == null)  // m_rawData is not null but card.m_rawData is
                return false;
            // compare raw data values
            return m_rawData.SequenceEqual(card.m_rawData);
        }

        public override int GetHashCode()
        {
            return MetaData == null ? m_rawData.GetHashCode() : MetaData.ID.GetHashCode();
        }
    }

    // Prize Tasks, and Restrictions
    public class SimpleCard : ICard
    {
        protected int m_DESCRIPTION_INDEX = 0;
        public string Description { get; protected set; }
        public string RawDescription { get { return m_rawData == null ? "" : m_rawData[m_DESCRIPTION_INDEX]; } }

        public override void Copy(ICard original)
        {
            base.Copy(original);
            if (original is SimpleCard originalCard) {
                Description = originalCard.Description;
            }
        }

        protected override bool VerifyRawData(string[] data)
        {
            return true;
        }

        public override void Format(ICardFormatter formatter)
        {
            Description = formatter.Format(RawDescription);
        }

        public override string ToString()
        {
            return Description;
        }
    }

    // Secret Tasks
    public class ScoredCard : SimpleCard
    {
        protected int m_SCORE_INDEX = 1;
        public int Score { get; protected set; }
        public string RawScore { get { return m_rawData == null ? "" : m_rawData[m_SCORE_INDEX]; } }
        
        protected override bool VerifyRawData(string[] data)
        {
            if (data.Length != 2)
                throw new FormatException($"Expected two fields (Action, Score), but got {data.Length}");
            return true;
        }

        public override void Format(ICardFormatter formatter)
        {
            Description = formatter.Format(RawDescription);
            Score = int.Parse(formatter.Format(RawScore));  // can throw exception
        }
        
        public override void Copy(ICard original)
        {
            base.Copy(original);
            if (original is ScoredCard originalCard) {
                Score = originalCard.Score;
            }
        }

        public override string ToString()
        {
            return base.ToString() + $" (worth {Score} points)";
        }
    }

    // Tasks and Final Tasks
    public class TaskCard : SimpleCard
    {
        public const string TEAM_MARK = "team";
        protected int m_MATERIALS_INDEX = 0,
            m_CRITERIA_INDEX = 2,
            m_IS_TEAM_TASK_INDEX = 3;

        public string Materials { get; protected set; }
        public string RawMaterials { get { return m_rawData == null ? "" : m_rawData[m_MATERIALS_INDEX]; } }
        public string Criteria { get; protected set; }
        public string RawCriteria { get { return m_rawData == null ? "" : m_rawData[m_CRITERIA_INDEX]; } }
        public List<SimpleCard> Restrictions;
        public bool IsTeamTask { get; protected set; }
        public string RawIsTeamTask { get { return m_rawData == null ? "" : m_rawData[m_IS_TEAM_TASK_INDEX]; } }
        

        public TaskCard()
        {
            m_DESCRIPTION_INDEX = 1;
            Restrictions = new List<SimpleCard>();
        }

        protected override bool VerifyRawData(string[] data)
        {
            if (data.Length != 3 && data.Length != 4)
                throw new FormatException($"Expected three fields (Materials, Action, Criteria[, Team]), but got {data.Length}");
            if (data.Length == 4 && data[3].ToLower() != TEAM_MARK)
                throw new FormatException($"Must use the \"{TEAM_MARK}\" mark in order to register this as a team task");
            return true;
        }

        public override void Format(ICardFormatter formatter)
        {
            Materials = formatter.Format(m_rawData[0]);
            Description = formatter.Format(m_rawData[1]);
            Criteria = formatter.Format(m_rawData[2]);
            IsTeamTask = m_rawData.Length == 4;
        }
        
        public override void Copy(ICard original)
        {
            base.Copy(original);
            if (original is TaskCard originalCard) {
                Materials = originalCard.Materials;
                Criteria = originalCard.Criteria;
                Restrictions = new List<SimpleCard>(originalCard.Restrictions);
                IsTeamTask = originalCard.IsTeamTask;
            }
        }

        public override string ToString()
        {
            string text = IsTeamTask ? "* Team Task *\n" : "";
            text += $"Materials: {Materials}\n{Description}\n";
            if (Restrictions.Count > 0) {
                text += "Restrictions:\n";
                foreach (SimpleCard restriction in Restrictions)
                    text += $" - {restriction.Description}\n";
            }
            text += Criteria;
            return text;
        }
    }

    // Information about each contestant
    public struct Contestant
    {
        public string Name;
        public int Score;
        public ScoredCard SecretTask;

        public Contestant(string name, ScoredCard secretTask = null)
        {
            Name = name;
            Score = 0;
            SecretTask = secretTask;
        }
    }

    // Information about the game
    public class Game
    {
        public SimpleCard PrizeTask;
        public Dictionary<string, Contestant> Contestants = new Dictionary<string, Contestant>();
        public List<TaskCard> Tasks = new List<TaskCard>();
        public TaskCard FinalTask;

        public override string ToString()
        {
            string text = PrizeTask == null ? "" : $"Prize Task:\n{PrizeTask}\n";
            var enumerator = Contestants.Values.GetEnumerator();
            _ = enumerator.MoveNext();
            if (enumerator.Current.SecretTask != null) {
                foreach (string name in Contestants.Keys)
                    text += $"\n{name}'s Secret Task:\n{Contestants[name].SecretTask}\n";
            }
            for (int i = 0; i < Tasks.Count; ++i)
                text += $"\nTask {i + 1}:\n{Tasks[i]}\n";
            text += $"\nFinal Task:\n{FinalTask}";
            return text;
        }
    }
}
