using System.IO;
using System.Collections.Generic;

namespace Backend
{
    public class TextDatabase : IDatabase
    {
        public TextDatabase(string prizeTaskFile, string secretTaskFile, string taskFile,
            string restrictionFile, string finalTaskFile, ICardFormatter formatter) : base(formatter)
        {
            Initialize(
                File.Exists(prizeTaskFile)   ? new CardParser<SimpleCard>(prizeTaskFile).Parse()   : new List<SimpleCard>(0),
                File.Exists(secretTaskFile)  ? new CardParser<ScoredCard>(secretTaskFile).Parse()  : new List<ScoredCard>(0),
                File.Exists(taskFile)        ? new CardParser<TaskCard>(taskFile).Parse()          : new List<TaskCard>(0),
                File.Exists(restrictionFile) ? new CardParser<SimpleCard>(restrictionFile).Parse() : new List<SimpleCard>(0),
                File.Exists(finalTaskFile)   ? new CardParser<TaskCard>(finalTaskFile).Parse()     : new List<TaskCard>(0)
            );
        }
    }
}
