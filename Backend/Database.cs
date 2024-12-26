using System;
using System.Collections.Generic;
using System.Linq;

namespace Backend
{
    public class Database
    {
        #region ----- Datamembers -----
        // lists of cards for the active game
        protected List<SimpleCard> m_prizeTasks, m_restrictions;
        protected List<ScoredCard> m_secretTasks;
        protected List<TaskCard> m_tasks, m_finalTasks;
        public IReadOnlyList<SimpleCard> PrizeTasks { get { return m_prizeTasks.AsReadOnly(); } }
        public IReadOnlyList<ScoredCard> SecretTasks { get { return m_secretTasks.AsReadOnly(); } }
        public IReadOnlyList<TaskCard> Tasks { get { return m_tasks.AsReadOnly(); } }
        public IReadOnlyList<SimpleCard> Restrictions { get { return m_restrictions.AsReadOnly(); } }
        public IReadOnlyList<TaskCard> FinalTasks { get { return m_finalTasks.AsReadOnly(); } }
        
        // lists of all loaded cards
        protected CardParser<SimpleCard> m_loadedPrizeTasks, m_loadedRestrictions;
        protected CardParser<ScoredCard> m_loadedSecretTasks;
        protected CardParser<TaskCard> m_loadedTasks, m_loadedFinalTasks;
        public IReadOnlyList<SimpleCard> LoadedPrizeTasks { get { return m_loadedPrizeTasks.Cards; } }
        public IReadOnlyList<ScoredCard> LoadedSecretTasks { get { return m_loadedSecretTasks.Cards; } }
        public IReadOnlyList<TaskCard> LoadedTasks { get { return m_loadedTasks.Cards; } }
        public IReadOnlyList<SimpleCard> LoadedRestrictions { get { return m_loadedRestrictions.Cards; } }
        public IReadOnlyList<TaskCard> LoadedFinalTasks { get { return m_loadedFinalTasks.Cards; } }

        // file paths
        public string PrizeTaskFilePath { get; protected set; }
        public string SecretTaskFilePath { get; protected set; }
        public string TaskFilePath { get; protected set; }
        public string RestrictionFilePath { get; protected set; }
        public string FinalTaskFilePath { get; protected set; }

        protected Random m_rand;
        protected ICardFormatter m_formatter;
        #endregion Datamembers

        public Database(string prizeTaskFile, string secretTaskFile, string taskFile,
            string restrictionFile, string finalTaskFile, ICardFormatter formatter)
        {
            m_rand = new Random();
            m_formatter = formatter;

            PrizeTaskFilePath  = prizeTaskFile;
            m_loadedPrizeTasks = new CardParser<SimpleCard>(prizeTaskFile);
            m_prizeTasks       = m_loadedPrizeTasks.Parse();
            
            SecretTaskFilePath  = secretTaskFile;
            m_loadedSecretTasks = new CardParser<ScoredCard>(secretTaskFile);
            m_secretTasks       = m_loadedSecretTasks.Parse();
            
            TaskFilePath  = taskFile;
            m_loadedTasks = new CardParser<TaskCard>(taskFile);
            m_tasks       = m_loadedTasks.Parse();
            
            RestrictionFilePath  = restrictionFile;
            m_loadedRestrictions = new CardParser<SimpleCard>(restrictionFile);
            m_restrictions       = m_loadedRestrictions.Parse();
            
            FinalTaskFilePath  = finalTaskFile;
            m_loadedFinalTasks = new CardParser<TaskCard>(finalTaskFile);
            m_finalTasks       = m_loadedFinalTasks.Parse();
        }

        public void ResetDecks()
        {
            m_prizeTasks   = new List<SimpleCard>(m_loadedPrizeTasks.Cards);
            m_secretTasks  = new List<ScoredCard>(m_loadedSecretTasks.Cards);
            m_tasks        = new List<TaskCard>(m_loadedTasks.Cards);
            m_restrictions = new List<SimpleCard>(m_loadedRestrictions.Cards);
            m_finalTasks   = new List<TaskCard>(m_loadedFinalTasks.Cards);
        }

        #region ----- Draw  Cards -----
        public ScoredCard DrawSecretTask()
        {
            return DrawCard(m_secretTasks);
        }
        public List<ScoredCard> DrawSecretTasks(int numContestants)
        {
            return DrawCards(m_secretTasks, numContestants);
        }

        public SimpleCard DrawPrizeTask()
        {
            return DrawCard(m_prizeTasks);
        }

        public TaskCard DrawTask()
        {
            return DrawCard(m_tasks);
        }
        public List<TaskCard> DrawTasks(int numTasks)
        {
            return DrawCards(m_tasks, numTasks);
        }
        
        public SimpleCard DrawRestriction()
        {
            return DrawCard(m_restrictions);
        }
        public List<SimpleCard> Drawrestrictions(int numRestrictions)
        {
            return DrawCards(m_restrictions, numRestrictions);
        }
        
        public TaskCard DrawFinalTask()
        {
            return DrawCard(m_finalTasks);
        }

        public Game DrawGame(string[] contestants, int numTasks, bool drawSecretTasks, bool drawPrizeTask)
        {
            // draw cards
            ResetDecks();
            Game game = new Game() {
                PrizeTask = drawPrizeTask ? DrawPrizeTask() : null,
                Tasks = DrawTasks(numTasks),
                FinalTask = DrawFinalTask()
            };
            foreach (string name in contestants) {
                game.Contestants.Add(name, new Contestant(name, drawSecretTasks ? DrawSecretTask() : null));
            }

            // verify they were drawn properly
            if (drawPrizeTask && game.PrizeTask == null || game.Tasks == null || game.Tasks.Count != numTasks ||
                game.FinalTask == null || drawSecretTasks && game.Contestants[contestants[contestants.Length-1]].SecretTask == null) {
                game = null;
                ResetDecks();
            }
            return game;
        }
        #endregion Draw Cards

        // protected functions
        protected T DrawCard<T>(List<T> deck) where T : ICard
        {
            List<T> cards = DrawCards(deck, 1);
            return cards == null ? default : cards[0];
        }
        protected List<T> DrawCards<T>(List<T> deck, int num) where T : ICard
        {
            if (deck == null || deck.Count < num)
                return null;

            int index;
            List<T> cards = new List<T>(num);
            while (--num >= 0) {
                index = m_rand.Next(deck.Count);
                deck[index].Format(m_formatter);
                cards.Add(deck[index]);
                deck.RemoveAt(index);
            }
            return cards;
        }
    }
}
