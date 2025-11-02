using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Backend;

namespace GUI
{
    /// <summary>
    /// Interaction logic for CardModal.xaml
    /// </summary>
    public partial class CardModal : Window
    {
        public CardMetaData MetaData { get; protected set; }
        public string Description { get; protected set; }
        public string Materials { get; protected set; }
        public string Critera { get; protected set; }
        public string Score { get; protected set; }
        public bool IsTeamTask { get; protected set; }

        protected CardContentEditor m_focusedContentEditor = null;

        public CardModal()
        {
            InitializeComponent();
        }

        public static bool CreateNewCard(CardType cardType)
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
                default:
                    card = new TaskCard();
                    break;
            }
            card.MetaData = metaData;
            return new CardModal().SetCard(card, true);
        }
        public static bool EditCard(SimpleCard card)
        {
            return new CardModal().SetCard(card, false);
        }
        protected bool SetCard(SimpleCard card, bool creatingNewCard)
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
                ScoreText.Text = scoredCard.RawScore;
            } else if (card is TaskCard taskCard) {
                MaterialLabel.Visibility = MaterialText.Visibility =
                    CriteriaLabel.Visibility = CriteriaText.Visibility =
                    IsTeamTaskLabel.Visibility = IsTeamTaskCheck.Visibility = Visibility.Visible;
                MaterialText.Text = taskCard.RawMaterials;
                CriteriaText.Text = taskCard.RawCriteria;
                IsTeamTaskCheck.IsChecked = taskCard.IsTeamTask;
            }

            return ShowDialog() ?? false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DescriptionText.Text)) {
                MessageBox.Show("Please enter a description.", "No Description", MessageBoxButton.OK, MessageBoxImage.Error);
            } else if (MaterialText.IsVisible && string.IsNullOrWhiteSpace(MaterialText.Text)) {
                MessageBox.Show("Please enter the required materials.", "No Materials", MessageBoxButton.OK, MessageBoxImage.Error);
            } else if (CriteriaText.IsVisible && string.IsNullOrWhiteSpace(CriteriaText.Text)) {
                MessageBox.Show("Please enter the criteria.", "No Criteria", MessageBoxButton.OK, MessageBoxImage.Error);
            } else if (ScoreText.IsVisible && string.IsNullOrWhiteSpace(ScoreText.Text)) {
                MessageBox.Show("Please enter a score.", "No Score", MessageBoxButton.OK, MessageBoxImage.Error);
            } else {
                Description = DescriptionText.Text;
                Materials = MaterialText.Text;
                Critera = CriteriaText.Text;
                Score = ScoreText.Text;
                IsTeamTask = IsTeamTaskCheck.IsChecked == true;
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
