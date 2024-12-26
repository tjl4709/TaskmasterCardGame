using System;
using System.Collections.Generic;
using Backend;

namespace CUI
{
    public class CuiFormatter : ICardFormatter
    {
        public override string Format(string rawText)
        {
            if (rawText.Contains(OPENING))
                Console.WriteLine("Formatting: " + rawText);
            return base.Format(rawText);
        }

        public override string Prompt(string description, FormatTypes type)
        {
            string value;
            do {
                Console.Write($"Please enter {description} ({type}): ");
                value = Console.ReadLine();
            } while (!Verify(value, type));
            return value;
        }
    }

    class Program
    {
        // args: prize task file path, secret task file path, task file path, restriction file path,
        //       final task file path, contestant 1's name, contestant 2's name, ..., contestant N's name
        static void Main(string[] args)
        {
            // draw game
            CuiFormatter formatter = new();
            Database database = new(args[0], args[1], args[2], args[3], args[4], formatter);
            string[] contestantNames = new string[args.Length - 5];
            Array.ConstrainedCopy(args, 5, contestantNames, 0, contestantNames.Length);
            Game game = database.DrawGame(contestantNames, 5, true, true);

            // add restrictions
            TaskCard task;
            SimpleCard restriction;
            for (int i = 0; i <= game.Tasks.Count; ++i) {
                if (i == game.Tasks.Count ) {
                    task = game.FinalTask;
                    Console.WriteLine($"\nFinal Task:\n{task}");
                } else {
                    task = game.Tasks[i];
                    Console.WriteLine($"\nTask {i + 1}:\n{task}");
                }

                // add restrictions to task cards
                while (database.Restrictions.Count > 0) {
                    Console.Write("Would you like to add a restriction? (Y/n): ");
                    if (char.ToLower(Console.ReadLine()[0]) == 'n')
                        break;

                    restriction = database.DrawRestriction();   //TODO add way to reject and re-insert restrictions
                    Console.WriteLine($"Restriction added: {restriction}");
                    task.Restrictions.Add(restriction);
                }
            }

            // display entire game
            Console.Clear();
            Console.WriteLine(game.ToString());
            // keep console open so user can read
            Console.Write("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
