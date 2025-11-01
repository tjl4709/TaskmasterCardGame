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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Backend;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected SqlDatabase m_database;

        public MainWindow()
        {
            InitializeComponent();
            m_database = new SqlDatabase(@"C:\Users\7budd\source\repos\TaskmasterCardGame\CardData\test_card_db.sqlite3", new GuiCardFormatter());
            CardManager.Database = m_database;
            CardManager.OnDatabaseChanged();
        }

        private void BuildGameButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 3;  // board settings page
        }

        private void EditCardsButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 1;  // card table page
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 0;  // main menu
        }
        
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
