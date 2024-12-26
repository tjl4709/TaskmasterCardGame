using System;
using System.Collections.Generic;

namespace Backend
{
    public abstract class ICard
    {
        protected string[] m_rawData;

        public static T Create<T>(string[] data) where T : ICard, new()
        {
            T card = new T();
            card.Assign(data);
            return card;
        }

        public void Assign(string[] data)
        {
            if (VerifyRawData(data))
                m_rawData = data;
        }

        protected abstract bool VerifyRawData(string[] data);
        public abstract void Format(ICardFormatter formatter);
    }

    // Prize Tasks, and Restrictions
    public class SimpleCard : ICard
    {
        public string Descripton { get; protected set; }

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
