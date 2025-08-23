using System;
using System.Collections.Generic;
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
        public int ID;
        public DateTime Created;
        public DateTime LastModified;
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
        public CardMetaData MetaData;
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
            if (VerifyRawData(data))
                m_rawData = data;
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
            if (MetaData != null && card.MetaData != null)
                return MetaData.ID == card.MetaData.ID;
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
        public string Descripton { get; protected set; }

        public override void Copy(ICard original)
        {
            base.Copy(original);
            if (original is SimpleCard originalCard) {
                Descripton = originalCard.Descripton;
            }
        }

        protected override bool VerifyRawData(string[] data)
        {
            return true;
        }

        public override void Format(ICardFormatter formatter)
        {
            Descripton = formatter.Format(m_rawData[0]);
        }

        public override string ToString()
        {
            return Descripton;
        }
    }

    // Secret Tasks
    public class ScoredCard : SimpleCard
    {
        public int Score { get; protected set; }
        
        protected override bool VerifyRawData(string[] data)
        {
            if (data.Length != 2)
                throw new FormatException($"Expected two fields (Action, Score), but got {data.Length}");
            return true;
        }

        public override void Format(ICardFormatter formatter)
        {
            Descripton = formatter.Format(m_rawData[0]);
            Score = int.Parse(formatter.Format(m_rawData[1]));  // can throw exception
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

        public string Materials { get; protected set; }
        public string Criteria { get; protected set; }
        public List<SimpleCard> Restrictions = new List<SimpleCard>();
        public bool IsTeamTask { get; protected set; }
        
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
            Descripton = formatter.Format(m_rawData[1]);
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
            text += $"Materials: {Materials}\n{Descripton}\n";
            if (Restrictions.Count > 0) {
                text += "Restrictions:\n";
                foreach (SimpleCard restriction in Restrictions)
                    text += $" - {restriction.Descripton}\n";
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
