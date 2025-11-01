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
        public SimpleCard Card { get; protected set; }
        public bool CreatingNewCard { get; protected set; }

        public CardModal()
        {
            InitializeComponent();
            DescriptionText.SetCustomizing();
        }

        public bool CreateNewCard(CardType cardType)
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
            return SetCard(card, true);
        }
        public bool EditCard(SimpleCard card)
        {
            return SetCard(card, false);
        }
        protected bool SetCard(SimpleCard card, bool creatingNewCard)
        {
            // set class properties
            Card = card;
            CreatingNewCard = creatingNewCard;

            // display meta data
            if (Card.MetaData == null) {
                IdText.Text = "";
                CreatedText.Text = "";
                LastModifiedText.Text = "";
                CardTypeText.Text = "";
            } else {
                IdText.Text = Card.MetaData.ID > 0 ? Card.MetaData.ID.ToString() : "";
                CreatedText.Text = Card.MetaData.Created.ToString("yyyy-MM-dd hh:mm:ss.fff");
                LastModifiedText.Text = Card.MetaData.LastModified.ToString("yyyy-MM-dd hh:mm:ss.fff");
                CardTypeText.Text = Card.MetaData.CardType.ToString();
            }

            // set content
            DescriptionText.Text = Card.Description;
            // TODO handle ScoredCard and TaskCard

            return ShowDialog() ?? false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }
    }
}
