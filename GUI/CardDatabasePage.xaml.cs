using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using Backend;

namespace GUI
{
    /// <summary>
    /// Interaction logic for CardDatabasePage.xaml
    /// </summary>
    public partial class CardDatabasePage : UserControl, INotifyPropertyChanged
    {
        public SqlDatabase Database { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnDatabaseChanged() { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Database")); }

        protected DataGrid ActiveTable {
            get {
                switch (TableTabControl.SelectedIndex) {
                    case 0:
                        return PrizeTaskTable;
                    case 1:
                        return SecretTaskTable;
                    case 2:
                        return TaskTable;
                    case 3:
                        return RestrictionTable;
                    case 4:
                        return FinalTaskTable;
                    default:
                        throw new IndexOutOfRangeException($"Cannot find ActiveTable: index {TableTabControl.SelectedIndex} cannot be linked to a table");
                };
            }
        }

        public CardDatabasePage()
        {
            InitializeComponent();
        }

        private void AddCardButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCard newCard = CardModal.CreateNewCard((CardType)(TableTabControl.SelectedIndex + 1));
            if (newCard != null) {
                Database.AddCard(newCard);
                CollectionViewSource.GetDefaultView(ActiveTable.ItemsSource).Refresh();
            }
        }

        private void EditCardButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCard editedCard = CardModal.EditCard((SimpleCard)ActiveTable.SelectedItem);
            if (editedCard != null) {
                Database.UpdateCard(editedCard);
                CollectionViewSource.GetDefaultView(ActiveTable.ItemsSource).Refresh();
            }
        }

        private void CopyCardButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCard copiedCard, originalCard = (SimpleCard)ActiveTable.SelectedItem;
            switch (originalCard.MetaData.CardType) {
                case CardType.PrizeTask:
                case CardType.Restriction:
                    copiedCard = ICard.Create(originalCard);
                    break;
                case CardType.SecretTask:
                    copiedCard = ICard.Create((ScoredCard)originalCard);
                    break;
                default:    // Task and FinalTask
                    copiedCard = ICard.Create((TaskCard)originalCard);
                    break;
            }
            copiedCard.MetaData.ID = 0;
            copiedCard = CardModal.EditCard(copiedCard);
            if (copiedCard != null) {
                Database.AddCard(copiedCard);
                CollectionViewSource.GetDefaultView(ActiveTable.ItemsSource).Refresh();
            }
        }

        private void DeleteCardButton_Click(object sender, RoutedEventArgs e)
        {
            DataGrid activeTable = ActiveTable;
            var cardsToRemove = activeTable.SelectedItems;
            if (cardsToRemove.Count > 0 && MessageBox.Show(
                    $"You are about to delete {cardsToRemove.Count} {((SimpleCard)cardsToRemove[0]).MetaData.CardType} card(s).",
                    "Verify Removal", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation
                ) == MessageBoxResult.OK)
            {
                foreach (SimpleCard card in cardsToRemove) {
                    Database.RemoveCard(card);
                }
                CollectionViewSource.GetDefaultView(activeTable.ItemsSource).Refresh();
            }
        }
    }
}
