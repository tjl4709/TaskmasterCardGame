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
    /// Interaction logic for CustomizableModal.xaml
    /// </summary>
    public partial class CustomizableModal : Window
    {
        public string Description { get; protected set; }
        public FormatTypes InputType { get; protected set; }

        public CustomizableModal()
        {
            InitializeComponent();
            InputTypeCombo.ItemsSource = Enum.GetNames(typeof(FormatTypes));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DescriptionText.Text)) {
                MessageBox.Show("Please enter a description.", "No Description", MessageBoxButton.OK, MessageBoxImage.Error);
            } else if (InputTypeCombo.SelectedIndex == -1) {
                MessageBox.Show("Please Select an input type.", "No Input Type", MessageBoxButton.OK, MessageBoxImage.Error);
            } else {
                Description = DescriptionText.Text;
                InputType = (FormatTypes)Enum.Parse(typeof(FormatTypes), (string)InputTypeCombo.SelectedItem);
                DialogResult = true;
            }
        }
    }
}
