using System;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;
using Microsoft.Win32;
using Backend;

namespace GUI
{
    /// <summary>
    /// Interaction logic for CardDatabasePage.xaml
    /// </summary>
    public partial class CardDatabasePage : UserControl, INotifyPropertyChanged
    {
        protected SqlDatabase mDatabase;
        public SqlDatabase Database {
            get => mDatabase;
            set {
                mDatabase = value;
                Properties.Settings.Default.DatabaseFilePath =
                    SelectedDatabasePath.Text = value.FilePath;
                Properties.Settings.Default.Save();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnDatabaseChanged() { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Database")); }

        public event RoutedEventHandler BackButtonClick {
            add { BackButton.Click += value; }
            remove { BackButton.Click -= value; }
        }

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
        protected ICollectionView ActiveView => CollectionViewSource.GetDefaultView(ActiveTable.ItemsSource);

        public CardDatabasePage()
        {
            InitializeComponent();

            
        }

        protected ICollectionView[] GetTableViews()
        {
            return new ICollectionView[] {
                CollectionViewSource.GetDefaultView(PrizeTaskTable.ItemsSource),
                CollectionViewSource.GetDefaultView(SecretTaskTable.ItemsSource),
                CollectionViewSource.GetDefaultView(TaskTable.ItemsSource),
                CollectionViewSource.GetDefaultView(RestrictionTable.ItemsSource),
                CollectionViewSource.GetDefaultView(FinalTaskTable.ItemsSource)
            };
        }
        
        // Event Handlers

        private void DatabaseSelectButton_Click(object sender, RoutedEventArgs e)
        {
            // open file dialog to select database to manage
            var dialog = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = false,
                DefaultExt = "tsv",
                Filter = "SQLite Databases|*.sqlite3",
                InitialDirectory = Path.GetDirectoryName(mDatabase.FilePath),
                Multiselect = false,
                Title = "Select Text/TSV Database(s) to Import Cards From:",
                ValidateNames = true
            };
            
            // load the selected database
            if (dialog.ShowDialog() == true) {
                // using the public property will also update the displayed and persistent file paths
                Database = new SqlDatabase(dialog.FileName);
                OnDatabaseChanged();
                // refresh tables
                var views = GetTableViews();
                for (int i = 0; i < views.Length; ++i)
                    views[i].Refresh();
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            // open file dialog to select file(s) to import from
            var dialog = new OpenFileDialog() {
                AddExtension = true,
                CheckFileExists = true,
                DefaultExt = "tsv",
                Filter = "Tab Separated Value Files|*.tsv",
                InitialDirectory = Path.GetDirectoryName(mDatabase.FilePath),
                Multiselect = true,
                Title = "Select Text/TSV Database(s) to Import Cards From:",
                ValidateNames = true
            };

            if (dialog.ShowDialog() == true) {
                // import the files
                mDatabase.ImportTextDatabases(dialog.FileNames);
                // refresh tables
                var views = GetTableViews();
                for (int i = 0; i < views.Length; ++i)
                    views[i].Refresh();
            }
        }

        private void SearchCriteriaEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            var views = GetTableViews();
            StatusLabel.Content = "Searching...";
            if (string.IsNullOrWhiteSpace(SearchCriteriaEdit.Text)) {
                for (int i = 0; i < views.Length; ++i)
                    views[i].Filter = null;
            } else {
                for (int i = 0; i < views.Length; ++i)
                    views[i].Filter = (card) => ((ICard)card).ContainsIgnoreCase(SearchCriteriaEdit.Text);
            }
            StatusLabel.Content = "";
        }

        // Card CRUD Methods

        private void AddCardButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCard newCard = CardModal.CreateNewCard((CardType)(TableTabControl.SelectedIndex + 1));
            if (newCard != null) {
                mDatabase.AddCard(newCard);
                ActiveView.Refresh();
            }
        }

        private void EditCardButton_Click(object sender, RoutedEventArgs e)
        {
            SimpleCard editedCard = CardModal.EditCard((SimpleCard)ActiveTable.SelectedItem);
            if (editedCard != null) {
                mDatabase.UpdateCard(editedCard);
                ActiveView.Refresh();
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
                mDatabase.AddCard(copiedCard);
                ActiveView.Refresh();
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
                    mDatabase.RemoveCard(card);
                }
                ActiveView.Refresh();
            }
        }
    }
}
