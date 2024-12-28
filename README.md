# TaskmasterCardGame
This is a set of cards to randomly create a game of Taskmaster at home.

There are five categories of cards: Secret Tasks, Prize Tasks, Tasks, Restrictions, and Final Tasks. You may play with one person as the Taskmaster for the entire game, or the role may rotate for each task, but the Taskmaster will not partake in any tasks they judge and will therefore not receive any points for it. It may be beneficial to have a single person as Taskmaster and have them draw all tasks before starting the game to set everything up beforehand.

## Prize Tasks:
Prize Task cards give a theme or category of item(s) for each contestant to bring, which will be presented at the beginning of the game. The Taskmaster will judge which items they like/fit the Prize Task card the best. The prizes that each contestant brings will be awarded to the contestant with the highest point total at the end of the game. The Prize Task is not required and may be skipped if preferred.

## Secret Tasks:
Each contestant should draw one Secret Task at the beginning of the game. These will be actions the contestant must complete without drawing suspicion from their fellow contestants. If completed successfully, the contestant will be awarded 3-5 bonus points. Some of these will require Taskmaster input, so it is recommended that they only be used when a single person plays Taskmaster for the entire game. The back of each Secret Task card will show 3-5 stars, indicating the difficulty and number of points awarded. You may find it fairer if all contestants get Secret Tasks of the same difficulty.

## Tasks and Restrictions:
Each round, the Taskmaster will draw a Task card which will detail the task each contestant must complete. To make the task harder, the Taskmaster may draw extra Restrictions to apply to the task. Each Task card will contain a list of materials and setup required, the action to be performed, and the criteria for it to be judged on. It is recommended that contestants perform the tasks individually and are recorded so they can be viewed later by all contestants. You may however, have all contestants perform the task at the same time, similar to the Final Tasks, but recording the task is still recommended to verify any secret tasks after they are revealed. It is also recommended to read the tasks to the contestants in the following order: the Action of the Task card, any Restriction cards that were applied, and finally the Criteria of the Task card.

## Final Tasks:
These are meant to be similar to the final, live tasks of each show. All the contestants will perform the Final Task at the same time. These will be formatted the same as the Task cards with Materials, Action, and Criteria, and can also have Restriction cards applied to them. It is recommended to perform a Final Task as the last round of the game.

# Card Formats
Each deck has a Tab-Separated Value file (.tsv) containing all its cards. Each card is on a separate line; blank lines are ignored. For cards with multiple fields, each field is separated by tabs on the same line. The fields for each deck are listed below.

Cards can be customized each time they're drawn by using double angle brackets around a description of what to customize. Formatter types can also be used to control what kind of input to accept for the customization by adding a colon and type character after the description in the double angle brackets. By default, it expects a phrase for the customization. See below for the different formatter types and examples.
Character | Type
----------|-----
i | Integer/Whole Number
d | Decimal/Floating Point Number
l | Letter
w | Word
p | Phrase

Examples:
- Say \<\<enter a phrase or word>> before starting the task
- Perform \<\<enter a number:i>> jumping jacks
- You may only saw words starting with the letter \<\<enter a letter:l>>

## Prize Tasks and Restrictions
The cards in the Prize Task and Restriction decks only contain a single field describing the prize or restriction. Only the first field of each line is used, if there are additional fields, they are ignored. Each card is on a separate line.

## Secret Tasks
The cards in the Secret Task deck contain two fields in the following order: the description of secret task, and the number of points the task is worth. Exactly two fields are required, any more or less will not be accepted. The fields are separated by a tab character and each card is on a separate line.

## Tasks and Final Tasks
The cards in the Final Task deck contain three fields in the following order: the materials and setup required for the task, the task the contestants should perform, and the criteria used to judge the task. Exactly three fields are required. The Task deck contains the same three fields in the same order and an optional fourth field to mark it as a team task. If this field is included, it must have the value "Team". The fields are separated by tabs and each card is on a separate line.


# Version 0.1
Version 0.1 is the Minimal Viable Product (MVP). It has only a CUI with limited functionalty.

## Command Line Utility/Command-line User Interface (CUI)
The CUI will create a full game with a Prize Task, a Secret Task for each contestant, five Tasks, and a Final Task. It will allow the user to add Restrictions to each Task and the Final Task. It expects there to be a single Taskmaster for the entire game and provides only the functionality of generating the game by drawing cards. It provides no during-game functionality such as score tracking.

## Graphical User Interface (GUI)
The GUI is not implemented in this version.