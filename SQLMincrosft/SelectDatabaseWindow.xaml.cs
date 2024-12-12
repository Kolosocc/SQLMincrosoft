using System;
using System.Data.SqlClient;
using System.Windows;

namespace DatabaseApp
{
    public partial class SelectDatabaseWindow : Window
    {
        private readonly string serverConnectionString = "Data Source=DBSRV\\ag2024;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        public string SelectedConnectionString { get; private set; }

        public SelectDatabaseWindow()
        {
            InitializeComponent();
            LoadDatabases(); // Загружаем базы данных при открытии окна
        }

        private void LoadDatabases()
        {
            try
            {
                // Подключаемся к серверу и получаем список баз данных
                using (SqlConnection connection = new SqlConnection(serverConnectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("SELECT name FROM sys.databases", connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        DatabasesComboBox.Items.Add(reader["name"].ToString());
                    }

                    reader.Close();

                    // Устанавливаем первую базу данных по умолчанию (если есть)
                    if (DatabasesComboBox.Items.Count > 0)
                    {
                        DatabasesComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении баз данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (DatabasesComboBox.SelectedItem != null)
            {
                string selectedDatabase = DatabasesComboBox.SelectedItem.ToString();
                SelectedConnectionString = $"Data Source=DBSRV\\ag2024;Initial Catalog={selectedDatabase};Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите базу данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
