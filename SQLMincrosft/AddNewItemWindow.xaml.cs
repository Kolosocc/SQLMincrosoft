using System;
using System.Windows;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Windows.Controls;

namespace DatabaseApp
{
    public partial class AddNewItemWindow : Window
    {
        private readonly string tableName;
        private readonly SqlConnection sqlConnection;
        private List<TextBox> textBoxes = new List<TextBox>();

        public AddNewItemWindow(string tableName, SqlConnection sqlConnection)
        {
            InitializeComponent();
            this.tableName = tableName;
            this.sqlConnection = sqlConnection;
            LoadColumns();
        }

        private void LoadColumns()
        {
            try
            {
                string query = $@"
                    SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @tableName";

                SqlCommand command = new SqlCommand(query, sqlConnection);
                command.Parameters.AddWithValue("@tableName", tableName);

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string columnName = reader.GetString(0);
                    CreateInputField(columnName);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке столбцов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateInputField(string columnName)
        {
            TextBox textBox = new TextBox
            {
                Name = columnName,
                Margin = new Thickness(0, 0, 0, 5),
                Width = 200
            };

            TextBlock label = new TextBlock
            {
                Text = columnName,
                Margin = new Thickness(0, 0, 0, 5)
            };

            StackPanel.Children.Add(label);
            StackPanel.Children.Add(textBox);
            textBoxes.Add(textBox); // Добавляем текстбокс в список для дальнейшего использования
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Строим строку для добавления значений
                string columns = string.Join(", ", GetColumnNames());
                string values = string.Join(", ", GetParameterNames());

                string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
                SqlCommand command = new SqlCommand(query, sqlConnection);

                // Добавляем параметры для каждого TextBox
                for (int i = 0; i < textBoxes.Count; i++)
                {
                    command.Parameters.AddWithValue(GetParameterNames()[i], textBoxes[i].Text);
                }

                command.ExecuteNonQuery();

                MessageBox.Show("Запись добавлена успешно.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для получения имён столбцов
        private List<string> GetColumnNames()
        {
            List<string> columnNames = new List<string>();
            foreach (TextBox textBox in textBoxes)
            {
                columnNames.Add(textBox.Name); // Используем имя TextBox как имя столбца
            }
            return columnNames;
        }

        // Метод для получения параметров для запроса
        private List<string> GetParameterNames()
        {
            List<string> parameterNames = new List<string>();
            foreach (TextBox textBox in textBoxes)
            {
                parameterNames.Add("@" + textBox.Name); // Используем имя TextBox с добавленным "@"
            }
            return parameterNames;
        }
    }
}
