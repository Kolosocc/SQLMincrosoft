using System.Windows;

namespace DatabaseApp
{
    public partial class SelectDatabaseWindow : Window
    {
        public string SelectedConnectionString { get; private set; }

        public SelectDatabaseWindow()
        {
            InitializeComponent();
            DefaultConnectionStringTextBox.Text = "Data Source=DBSRV\\ag2024;Initial Catalog=KolosovDA_2207_g2_lab16;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedConnectionString = DefaultConnectionStringTextBox.Text;
            DialogResult = true;
        }
    }
}
