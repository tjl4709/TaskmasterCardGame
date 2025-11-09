using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Data.Sqlite;
using Backend;

namespace GUI
{
    public class SqlDatabase : IDatabase
    {
        #region --------------- Data Members ---------------
        protected struct ColumnData
        {
            public string Name;
            public string Type;

            public ColumnData(string name, string type)
            {
                Name = name;
                Type = type;
            }

            public static ColumnData Parse(string rawData)
            {
                // columns in format "<name> <type>"
                var colData = rawData.Split(new char[] { ' ' }, 2);
                return new ColumnData(colData[0], colData[1]);
            }

            public override string ToString()
            {
                return $"{Name} {Type}";
            }
        }
        protected struct TableData
        {
            public string Name;
            public List<ColumnData> Columns;

            public TableData(string name, List<ColumnData> columns = null)
            {
                Name = name;
                Columns = columns ?? new List<ColumnData>(0);
            }

            public static TableData Parse(string rawData)
            {
                // table in format "CREATE TABLE <name> (<column data>, ...)"
                int openPerenIdx = rawData.IndexOf('(');
                int nameStartIdx = rawData.LastIndexOf(' ', openPerenIdx - 2);
                string name = rawData.Substring(nameStartIdx, openPerenIdx - nameStartIdx - 1);
                string[] rawColumnData = rawData.Substring(openPerenIdx + 1, rawData.Length - openPerenIdx - 2).Split(new char[] { ',' });
                List<ColumnData> colData = rawColumnData.Select(col => ColumnData.Parse(col.Trim())).ToList();
                return new TableData(name, colData);
            }

            public override string ToString()
            {
                return $"CREATE TABLE {Name} ({string.Join(", ", Columns)})";
            }
        }

        protected SqliteConnection m_connection;
        /// <summary>
        /// this contains the column names and types expected for each table in the database
        /// </summary>
        protected static Dictionary<CardType, TableData> m_TABLES = new Dictionary<CardType, TableData>() {
            {CardType.PrizeTask, new TableData("prize_tasks", new List<ColumnData>() {
                new ColumnData("id", "INTEGER PRIMARY KEY"),
                new ColumnData("created", "INTEGER NOT NULL"),
                new ColumnData("last_modified", "INTEGER NOT NULL"),
                new ColumnData("description", "TEXT NOT NULL")
            })},
            {CardType.SecretTask, new TableData("secret_tasks", new List<ColumnData>() {
                new ColumnData("id", "INTEGER PRIMARY KEY"),
                new ColumnData("created", "INTEGER NOT NULL"),
                new ColumnData("last_modified", "INTEGER NOT NULL"),
                new ColumnData("description", "TEXT NOT NULL"),
                new ColumnData("score", "TEXT NOT NULL")
            })},
            {CardType.Task, new TableData("tasks", new List<ColumnData>() {
                new ColumnData("id", "INTEGER PRIMARY KEY"),
                new ColumnData("created", "INTEGER NOT NULL"),
                new ColumnData("last_modified", "INTEGER NOT NULL"),
                new ColumnData("description", "TEXT NOT NULL"),
                new ColumnData("material", "TEXT NOT NULL"),
                new ColumnData("criteria", "TEXT NOT NULL"),
                new ColumnData("is_team", "TEXT NOT NULL")
            })},
            {CardType.Restriction, new TableData("restrictions", new List<ColumnData>() {
                new ColumnData("id", "INTEGER PRIMARY KEY"),
                new ColumnData("created", "INTEGER NOT NULL"),
                new ColumnData("last_modified", "INTEGER NOT NULL"),
                new ColumnData("description", "TEXT NOT NULL")
            })},
            {CardType.FinalTask, new TableData("final_tasks", new List<ColumnData>() {
                new ColumnData("id", "INTEGER PRIMARY KEY"),
                new ColumnData("created", "INTEGER NOT NULL"),
                new ColumnData("last_modified", "INTEGER NOT NULL"),
                new ColumnData("description", "TEXT NOT NULL"),
                new ColumnData("material", "TEXT NOT NULL"),
                new ColumnData("criteria", "TEXT NOT NULL"),
                new ColumnData("is_team", "TEXT NOT NULL")
            })},
        };
        #endregion

        public SqlDatabase(string filePath, ICardFormatter formatter)
            : base(formatter)
        {
            // initialize lists
            m_loadedPrizeTasks   = new List<SimpleCard>();
            m_loadedSecretTasks  = new List<ScoredCard>();
            m_loadedTasks        = new List<TaskCard>();
            m_loadedRestrictions = new List<SimpleCard>();
            m_loadedFinalTasks   = new List<TaskCard>();

            m_prizeTasks   = new List<SimpleCard>();
            m_secretTasks  = new List<ScoredCard>();
            m_tasks        = new List<TaskCard>();
            m_restrictions = new List<SimpleCard>();
            m_finalTasks   = new List<TaskCard>();

            // open database (if it did not exist, a new one will be created)
            SQLitePCL.Batteries.Init();
            m_connection = new SqliteConnection($"Data Source={filePath}");
            m_connection.Open();
            // Verify all required tables have the required columns
            CheckTables();
            // fill in m_loaded* lists
            ReadAllCards();
        }

        ~SqlDatabase()
        {
            try {
                if (m_connection.State == System.Data.ConnectionState.Open && m_connection.Handle != null
                    && !m_connection.Handle.IsClosed && !m_connection.Handle.IsInvalid)
                    m_connection.Close();
            } catch (Exception e) {
                Console.WriteLine($"Error while closing connection to \"{m_connection.DataSource}\": {e}");
            }
        }

        // initialization functions

        protected void CheckTables()
        {
            // retrieve current tables
            var tableQueryCommand = new SqliteCommand("SELECT name, sql FROM sqlite_schema WHERE type='table';", m_connection);
            var reader = tableQueryCommand.ExecuteReader();
            var currentTables = new Dictionary<string, string>();
            if (reader.HasRows) {
                while (reader.Read()) {
                    currentTables.Add(reader.GetString(0), reader.GetString(1).ToLower().Trim());
                }
            }

            // check for required tables
            bool found, matches = false;
            foreach (TableData requiredTable in m_TABLES.Values) {
                found = false;

                // check if table exists
                foreach (string currentTable in currentTables.Keys) {
                    if (currentTable == requiredTable.Name) {
                        found = true;
                        // check if table has the right columns
                        matches = currentTables[currentTable] == requiredTable.ToString().ToLower();
                        break;
                    }
                }

                if (!found) {
                    // the table was not found, so create it
                    var createTableCommand = m_connection.CreateCommand();
                    createTableCommand.CommandText = requiredTable.ToString();
                    createTableCommand.ExecuteNonQuery();
                } else if (!matches) {
                    // the table was found, but not all the columns matched
                    var currentCols = TableData.Parse(currentTables[requiredTable.Name]).Columns;

                    // check for required columns
                    foreach (ColumnData requiredCol in requiredTable.Columns) {
                        found = false;

                        // check if column exists
                        foreach (ColumnData currentCol in currentCols) {
                            if (requiredCol.Name == currentCol.Name) {
                                found = true;
                                // check if column has right type
                                matches = requiredCol.Type == currentCol.Type;
                                break;
                            }
                        }

                        if (!found || !matches) {
                            // setup command to add missing or mismatched column
                            var columnCommand = new SqliteCommand($"ALTER TABLE {requiredTable.Name} ADD COLUMN {requiredCol};", m_connection);
                            if (found && !matches) {
                                // the column was found, but it doesn't have the right type,
                                // so rename the old column before adding the new one
                                columnCommand.CommandText = $"ALTER TABLE {requiredTable.Name} RENAME COLUMN {requiredCol.Name} TO _{requiredCol.Name};"
                                    + columnCommand.CommandText;
                            }
                            // fill in last parameters and execute command
                            columnCommand.ExecuteNonQuery();
                        }
                    }   // end checking for required columns
                }
            }   // end checking for required tables
        }

        public void ReadAllCards()
        {
            ReadCardTable(CardType.PrizeTask);
            ReadCardTable(CardType.SecretTask);
            ReadCardTable(CardType.Task);
            ReadCardTable(CardType.Restriction);
            ReadCardTable(CardType.FinalTask);
        }

        protected void ReadCardTable(CardType cardType)
        {
            // read all the data from the given table
            var readCardsCommand = new SqliteCommand($"Select * from {m_TABLES[cardType].Name};", m_connection);
            var reader = readCardsCommand.ExecuteReader();
            
            // clear relevant list
            switch (cardType) {
                case CardType.PrizeTask:
                    m_loadedPrizeTasks.Clear();
                    break;
                case CardType.SecretTask:
                    m_loadedSecretTasks.Clear();
                    break;
                case CardType.Task:
                    m_loadedTasks.Clear();
                    break;
                case CardType.Restriction:
                    m_loadedRestrictions.Clear();
                    break;
                case CardType.FinalTask:
                    m_loadedFinalTasks.Clear();
                    break;
                default:
                    throw new ArgumentException("cannot add card of unknown card type", "cardType");
            }

            if (reader.HasRows) {
                // parse each row into a card
                while (reader.Read()) {
                    // id, created, last_modified
                    var metaData = new CardMetaData(reader.GetInt32(0), reader.GetInt64(1), reader.GetInt64(2), cardType);
                    // remaining fields get copied to an array to be parsed by the card type
                    var rawData = new string[reader.FieldCount - 3];
                    for (int i = 0; i < rawData.Length; ++i) {
                        rawData[i] = reader.GetString(i + 3);
                    }

                    // create card and add to relevant list
                    try {
                        switch (cardType) {
                            case CardType.PrizeTask:
                                m_loadedPrizeTasks.Add(ICard.Create<SimpleCard>(rawData, metaData));
                                break;
                            case CardType.SecretTask:
                                m_loadedSecretTasks.Add(ICard.Create<ScoredCard>(rawData, metaData));
                                break;
                            case CardType.Task:
                                m_loadedTasks.Add(ICard.Create<TaskCard>(rawData, metaData));
                                break;
                            case CardType.Restriction:
                                m_loadedRestrictions.Add(ICard.Create<SimpleCard>(rawData, metaData));
                                break;
                            case CardType.FinalTask:
                                m_loadedFinalTasks.Add(ICard.Create<TaskCard>(rawData, metaData));
                                break;
                            default:
                                throw new ArgumentException("cannot add card of unknown card type", "cardType");
                        }
                    } catch (Exception e) {
                        MessageBox.Show(e.Message, $"Error While Loading a {cardType} Card:", MessageBoxButton.OK, MessageBoxImage.Error);
                        // TODO: allow user to edit card?
                    }
                }
            }
        }

        // adjustment functions
        // TODO add error status somewhere so user knows why it failed
        
        public bool AddCard(SimpleCard card) { return AddCard(card, true); }

        protected bool AddCard(SimpleCard card, bool reload)
        {
            if (card.MetaData == null)
                return false;

            // update timestamps
            DateTime zero = DateTime.FromBinary(0), now = DateTime.Now;
            if (card.MetaData.Created == zero)
                card.MetaData.Created = now;
            if (card.MetaData.LastModified == zero)
                card.MetaData.LastModified = now;

            // create insert command with common data
            TableData table = m_TABLES[card.MetaData.CardType];
            string insertColumns = string.Join(", ", table.Columns.Skip(1).Select(col => col.Name));
            var insertCommand = new SqliteCommand($"INSERT INTO {table.Name} ({insertColumns}) VALUES (@created, @last_modified, @description", m_connection);
            insertCommand.Parameters.AddWithValue("@created", card.MetaData.Created.ToBinary());
            insertCommand.Parameters.AddWithValue("@last_modified", card.MetaData.LastModified.ToBinary());
            insertCommand.Parameters.AddWithValue("@description", card.RawDescription);

            // handle specifics based on type of card
            switch (card.MetaData.CardType) {
                case CardType.PrizeTask:
                case CardType.Restriction:
                    break;
                case CardType.SecretTask:
                    ScoredCard scoredCard = (ScoredCard)card;
                    // add extra data to command
                    insertCommand.CommandText += ", @score";
                    insertCommand.Parameters.AddWithValue("@score", scoredCard.RawScore);
                    break;
                case CardType.Task:
                case CardType.FinalTask:
                    TaskCard taskCard = (TaskCard)card;
                    // add extra data to command
                    insertCommand.CommandText += ", @material, @criteria, @is_team";
                    insertCommand.Parameters.AddWithValue("@material", taskCard.RawMaterials);
                    insertCommand.Parameters.AddWithValue("@criteria", taskCard.RawCriteria);
                    insertCommand.Parameters.AddWithValue("@is_team", taskCard.RawIsTeamTask);
                    break;
                default:
                    throw new ArgumentException("cannot add card of unknown card type", "cardType");
            }

            // finalize and send insert command
            insertCommand.CommandText += ");";
            bool success = insertCommand.ExecuteNonQuery() == 1;
            
            // update loaded list
            if (success && reload) ReadCardTable(card.MetaData.CardType);

            return success;
        }

        public bool UpdateCard(SimpleCard card)
        {
            if (card.MetaData == null)
                return false;

            if (card.MetaData.LastModified == DateTime.FromBinary(0))
                card.MetaData.LastModified = DateTime.Now;

            var command = new SqliteCommand($"UPDATE {m_TABLES[card.MetaData.CardType].Name} SET last_modified = @modified, description = @description", m_connection);
            command.Parameters.AddWithValue("@modified", card.MetaData.LastModified.ToBinary());
            command.Parameters.AddWithValue("@description", card.RawDescription);

            switch (card.MetaData.CardType) {
                case CardType.PrizeTask:
                case CardType.Restriction:
                    break;
                case CardType.SecretTask:
                    command.CommandText += ", score = @score";
                    command.Parameters.AddWithValue("@score", ((ScoredCard)card).RawScore);
                    break;
                case CardType.Task:
                case CardType.FinalTask:
                    command.CommandText += ", material = @material, criteria = @critera, is_team = @is_team";
                    TaskCard taskCard = (TaskCard)card;
                    command.Parameters.AddWithValue("@material", taskCard.RawMaterials);
                    command.Parameters.AddWithValue("@critera", taskCard.RawCriteria);
                    command.Parameters.AddWithValue("@is_team", taskCard.RawIsTeamTask);
                    break;
                default:
                    throw new ArgumentException("cannot update card of unknown card type", "cardType");
            }

            command.CommandText += " WHERE id = @id;";
            command.Parameters.AddWithValue("@id", card.MetaData.ID);
            bool success = command.ExecuteNonQuery() == 1;

            // update loaded list
            if (success) ReadCardTable(card.MetaData.CardType);

            return success;
        }

        public bool RemoveCard(SimpleCard card)
        {
            if (card.MetaData == null)
                return false;

            // remove card from database
            var commamd = new SqliteCommand($"DELETE FROM {m_TABLES[card.MetaData.CardType].Name} WHERE id = @id;", m_connection);
            commamd.Parameters.AddWithValue("@id", card.MetaData.ID);
            bool success = commamd.ExecuteNonQuery() == 1;
            
            // update loaded list
            if (success) ReadCardTable(card.MetaData.CardType);

            return success;
        }

        public bool ImportTextDatabase(string prizeTaskFile, string secretTaskFile,
            string taskFile, string restrictionFile, string finalTaskFile)
        {
            var textDatabase = new TextDatabase(prizeTaskFile, secretTaskFile,
                taskFile, restrictionFile, finalTaskFile, m_formatter);
            SqliteTransaction transaction = m_connection.BeginTransaction();
            bool success = true;
            CardMetaData metaData;

            // prize tasks
            if (textDatabase.LoadedPrizeTasks.Count > 0) {
                metaData = new CardMetaData(
                    File.GetCreationTime(prizeTaskFile),
                    File.GetLastWriteTime(prizeTaskFile),
                    CardType.PrizeTask
                );
                foreach (SimpleCard card in textDatabase.LoadedPrizeTasks) {
                    card.MetaData = metaData.Duplicate();
                    if (!AddCard(card, false)) {
                        success = false;
                        break;
                    }
                }
            }

            // secret tasks
            if (success && textDatabase.LoadedSecretTasks.Count > 0) {
                metaData = new CardMetaData(
                    File.GetCreationTime(secretTaskFile),
                    File.GetLastWriteTime(secretTaskFile),
                    CardType.SecretTask
                );
                foreach (SimpleCard card in textDatabase.LoadedSecretTasks) {
                    card.MetaData = metaData.Duplicate();
                    if (!AddCard(card, false)) {
                        success = false;
                        break;
                    }
                }
            }
            
            // tasks
            if (success && textDatabase.LoadedTasks.Count > 0) {
                metaData = new CardMetaData(
                    File.GetCreationTime(taskFile),
                    File.GetLastWriteTime(taskFile),
                    CardType.Task
                );
                foreach (SimpleCard card in textDatabase.LoadedTasks) {
                    card.MetaData = metaData.Duplicate();
                    if (!AddCard(card, false)) {
                        success = false;
                        break;
                    }
                }
            }
            
            // restrictions
            if (success && textDatabase.LoadedRestrictions.Count > 0) {
                metaData = new CardMetaData(
                    File.GetCreationTime(restrictionFile),
                    File.GetLastWriteTime(restrictionFile),
                    CardType.Restriction
                );
                foreach (SimpleCard card in textDatabase.LoadedRestrictions) {
                    card.MetaData = metaData.Duplicate();
                    if (!AddCard(card, false)) {
                        success = false;
                        break;
                    }
                }
            }

            // final tasks
            if (success && textDatabase.LoadedFinalTasks.Count > 0) {
                metaData = new CardMetaData(
                    File.GetCreationTime(finalTaskFile),
                    File.GetLastWriteTime(finalTaskFile),
                    CardType.FinalTask
                );
                foreach (SimpleCard card in textDatabase.LoadedFinalTasks) {
                    card.MetaData = metaData.Duplicate();
                    if (!AddCard(card, false)) {
                        success = false;
                        break;
                    }
                }
            }

            // complete transaction
            if (success) {
                transaction.Commit();
                ReadAllCards();
            } else
                transaction.Rollback();
            transaction.Dispose();

            return success;
        }
    }
}
