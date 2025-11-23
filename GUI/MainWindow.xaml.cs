using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
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
            // upon first execution of the program, this will default to a subfoler of %APPDATA%, so we need to expand the envorinmant variable
            Properties.Settings.Default.DatabaseFilePath = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.DatabaseFilePath);

            InitializeComponent();
            MainTabControl.SelectedIndex = 0;  // make sure to start on the main menu

            // setup card database and manager
            m_database = new SqlDatabase(Properties.Settings.Default.DatabaseFilePath);
            CardManager.BackButtonClick += BackButton_Click;
            CardManager.Database = m_database;
            CardManager.OnDatabaseChanged();
        }

        private void BuildGameButton_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 3;  // game settings page
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
