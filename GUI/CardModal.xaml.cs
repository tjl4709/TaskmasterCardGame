using System;
using System.Windows;
using System.Collections.Generic;
using Backend;

namespace GUI
{
    /// <summary>
    /// Interaction logic for CardModal.xaml
    /// </summary>
    public partial class CardModal : Window
    {
        public CardMetaData MetaData { get; protected set; }
        public SimpleCard EditedCard { get; protected set; }

        protected CardContentEditor m_focusedContentEditor = null;

        public CardModal()
        {
            InitializeComponent();
        }

        public static SimpleCard CreateNewCard(CardType cardType)
        {
            CardMetaData metaData = new CardMetaData(DateTime.Now, cardType);
            SimpleCard card;
            switch (cardType) {
                case CardType.PrizeTask:
                case CardType.Restriction:
                    card = new SimpleCard();
                    break;
                case CardType.SecretTask:
                    card = new ScoredCard();
                    break;
                default:    // Task and FinalTask types
                    card = new TaskCard();
                    break;
            }
            card.MetaData = metaData;
            return new CardModal().SetCard(card);
        }
        public static SimpleCard EditCard(SimpleCard card)
        {
            card.MetaData.LastModified = DateTime.Now;
            return new CardModal().SetCard(card);
        }
        protected SimpleCard SetCard(SimpleCard card)
        {
            MetaData = card.MetaData;

            // display meta data
            if (card.MetaData == null) {
                IdText.Text = "";
                CreatedText.Text = "";
                LastModifiedText.Text = "";
                CardTypeText.Text = "";
            } else {
                IdText.Text = card.MetaData.ID > 0 ? card.MetaData.ID.ToString() : "";
                CreatedText.Text = card.MetaData.Created.ToString("yyyy-MM-dd hh:mm:ss.fff");
                LastModifiedText.Text = card.MetaData.LastModified.ToString("yyyy-MM-dd hh:mm:ss.fff");
                CardTypeText.Text = card.MetaData.CardType.ToString();
            }

            // set content
            MaterialLabel.Visibility = MaterialText.Visibility = ScoreLabel.Visibility =
                ScoreText.Visibility = CriteriaLabel.Visibility = CriteriaText.Visibility =
                IsTeamTaskLabel.Visibility = IsTeamTaskCheck.Visibility = Visibility.Hidden;
            DescriptionText.Text = card.RawDescription;
            if (card is ScoredCard scoredCard) {
                ScoreLabel.Visibility = ScoreText.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(scoredCard.RawScore) && scoredCard.RawScore.StartsWith(ICardFormatter.OPENING)) {
                    int index = scoredCard.RawScore.IndexOf(':');
                    ScoreText.Text = scoredCard.RawScore.Substring(ICardFormatter.OPENING.Length, index - ICardFormatter.OPENING.Length);
                } else {
                    ScoreText.Text = scoredCard.RawScore;
                }
            } else if (card is TaskCard taskCard) {
                MaterialLabel.Visibility = MaterialText.Visibility =
                    CriteriaLabel.Visibility = CriteriaText.Visibility =
                    IsTeamTaskLabel.Visibility = IsTeamTaskCheck.Visibility = Visibility.Visible;
                MaterialText.Text = taskCard.RawMaterials;
                CriteriaText.Text = taskCard.RawCriteria;
                IsTeamTaskCheck.IsChecked = taskCard.IsTeamTask;
            }

            return ShowDialog() == true ? EditedCard : null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string message = "", title = "Invalid Description";
            // check for valid description
            if (DescriptionText.HasError) {
                message = "Please fixe the error in the description.";
            } else if (string.IsNullOrWhiteSpace(DescriptionText.Text)) {
                message = "Please enter a description.";
            }
            // check for valid score
            else if (message.Length == 0 && ScoreText.IsVisible && string.IsNullOrWhiteSpace(ScoreText.Text)) {
                message = "Please enter a score.";
                title = "Invalid Score";
            }
            // check for valid materials
            else if (MaterialText.IsVisible) {
                title = "Invalid Materials";
                if (MaterialText.HasError) {
                    message = "Please fix the error in the materials.";
                } else if (string.IsNullOrWhiteSpace(MaterialText.Text)) {
                    message = "Please enter the required materials.";
                }
            }
            // check for valid criteria
            if (message.Length == 0 && CriteriaText.IsVisible) {
                title = "Invalid Criteria";
                if (CriteriaText.HasError) {
                    message = "Please fix the error in the criteria.";
                } else if (string.IsNullOrWhiteSpace(CriteriaText.Text)) {
                    message = "Please enter the criteria.";
                }
            }

            // display error or save data
            if (message.Length > 0) {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            } else {
                switch (MetaData.CardType) {
                    case CardType.PrizeTask:
                    case CardType.Restriction:
                        EditedCard = ICard.Create<SimpleCard>(new string[] { DescriptionText.Text }, MetaData);
                        break;
                    case CardType.SecretTask:
                        string score = ScoreText.Text;
                        if (!int.TryParse(score, out _)) {
                            score = "<<" + score + ":i>>";
                        }
                        EditedCard = ICard.Create<ScoredCard>(new string[] { DescriptionText.Text, score }, MetaData);
                        break;
                    default:    // Task and FinalTask types
                        List<string> rawData = new List<string>() { MaterialText.Text, DescriptionText.Text, CriteriaText.Text };
                        if (IsTeamTaskCheck.IsChecked == true) {
                            rawData.Add(TaskCard.TEAM_MARK);
                        }
                        EditedCard = ICard.Create<TaskCard>(rawData.ToArray(), MetaData);
                        break;
                }
                DialogResult = true;
            }
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_focusedContentEditor != null) {
                CustomizableModal modal = new CustomizableModal();
                if (modal.ShowDialog() == true) {
                    m_focusedContentEditor.InsertCustomizableAtCursor(modal.Description, modal.InputType);
                }
            }
        }

        private void Control_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is CardContentEditor contentEditor) {
                m_focusedContentEditor = contentEditor;
            } else {
                m_focusedContentEditor = null;
            }
        }
    }
}
