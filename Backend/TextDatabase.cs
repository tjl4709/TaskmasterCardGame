namespace Backend
{
    public class TextDatabase : IDatabase
    {
        public TextDatabase(string prizeTaskFile, string secretTaskFile, string taskFile,
            string restrictionFile, string finalTaskFile, ICardFormatter formatter) : base(formatter)
        {
            Initialize(
                new CardParser<SimpleCard>(prizeTaskFile).Parse(),
                new CardParser<ScoredCard>(secretTaskFile).Parse(),
                new CardParser<TaskCard>(taskFile).Parse(),
                new CardParser<SimpleCard>(restrictionFile).Parse(),
                new CardParser<TaskCard>(finalTaskFile).Parse()
            );
        }
    }
}
