using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DatabaseApp
{
    public partial class MainWindow : Window
    {
        private string currentConnectionString;
        private SqlConnection sqlConnection;
        private bool isEditing = false; // Флаг для предотвращения рекурсивных вызовов

        public MainWindow()
        {
            InitializeComponent();
            currentConnectionString = "Data Source=DBSRV\\ag2024;Initial Catalog=KolosovDA_2207_g2_lab16;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
            sqlConnection = new SqlConnection(currentConnectionString);
        }

        private void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.ConnectionString = currentConnectionString;
                    sqlConnection.Open();
                    MessageBox.Show("Подключение установлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadTables();
                }
                else
                {
                    MessageBox.Show("Подключение уже установлено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridTableContent_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (isEditing || e.EditAction != DataGridEditAction.Commit || TablesListBox.SelectedItem == null)
                return;

            try
            {
                isEditing = true; // Устанавливаем флаг, чтобы предотвратить рекурсию

                // Применяем изменения
                DataGridTableContent.CommitEdit(DataGridEditingUnit.Row, true);

                var editedRow = (DataRowView)e.Row.Item;
                string tableName = TablesListBox.SelectedItem.ToString();
                string primaryKeyColumn = PrimaryKeyTextBlock.Text.Replace("Первичный ключ: ", "").Split(',')[0].Trim();
                string primaryKeyValue = editedRow[primaryKeyColumn].ToString();

                var updateColumns = string.Join(", ", editedRow.Row.Table.Columns.Cast<DataColumn>()
                    .Where(column => column.ColumnName != primaryKeyColumn)
                    .Select(column => $"{column.ColumnName} = @{column.ColumnName}"));

                string updateQuery = $"UPDATE [{tableName}] SET {updateColumns} WHERE {primaryKeyColumn} = @{primaryKeyColumn}";

                using (SqlCommand command = new SqlCommand(updateQuery, sqlConnection))
                {
                    foreach (DataColumn column in editedRow.Row.Table.Columns)
                    {
                        command.Parameters.AddWithValue($"@{column.ColumnName}", editedRow[column.ColumnName] ?? DBNull.Value);
                    }

                    command.ExecuteNonQuery();
                    MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения изменений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isEditing = false; // Сбрасываем флаг
            }
        }

        private void DeleteSelectedRow_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTableContent.SelectedItem == null || TablesListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите строку и таблицу для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedRow = (DataRowView)DataGridTableContent.SelectedItem;
                string tableName = TablesListBox.SelectedItem.ToString();
                string primaryKeyColumn = PrimaryKeyTextBlock.Text.Replace("Первичный ключ: ", "").Split(',')[0].Trim();
                string primaryKeyValue = selectedRow[primaryKeyColumn].ToString();

                string deleteQuery = $"DELETE FROM [{tableName}] WHERE {primaryKeyColumn} = @{primaryKeyColumn}";

                using (SqlCommand command = new SqlCommand(deleteQuery, sqlConnection))
                {
                    command.Parameters.AddWithValue($"@{primaryKeyColumn}", primaryKeyValue);
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Строка удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTableContent(tableName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления строки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectDatabaseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы хотите подключиться к базе данных lab16? Нажмите 'Да' для lab16 или 'Нет' для lab15.3.", "Выбор базы данных", MessageBoxButton.YesNo, MessageBoxImage.Question);

            currentConnectionString = result == MessageBoxResult.Yes
                ? "Data Source=DBSRV\\ag2024;Initial Catalog=KolosovDA_2207_g2_lab16;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
                : "Data Source=DBSRV\\ag2024;Initial Catalog=KolosovDA_2207_g2_lab15.3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

            if (sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }

            try
            {
                sqlConnection.ConnectionString = currentConnectionString;
                sqlConnection.Open();
                MessageBox.Show($"Подключение к базе данных: {currentConnectionString.Split(';')[1].Split('=')[1]}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTables();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTables()
        {
            if (sqlConnection == null || sqlConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Сначала подключитесь к базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DataTable schemaTable = sqlConnection.GetSchema("Tables");
                TablesListBox.Items.Clear();

                foreach (DataRow row in schemaTable.Rows)
                {
                    string tableName = row["TABLE_NAME"] as string;
                    if (tableName != null)
                    {
                        TablesListBox.Items.Add(tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TablesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesListBox.SelectedItem == null) return;

            string tableName = TablesListBox.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(tableName))
            {
                LoadTableContent(tableName);
            }
        }

        private void LoadTableContent(string tableName)
        {
            if (sqlConnection == null || sqlConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Сначала подключитесь к базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = $"SELECT * FROM [{tableName}]";
                SqlDataAdapter adapter = new SqlDataAdapter(query, sqlConnection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                DataGridTableContent.ItemsSource = null;
                DataGridTableContent.ItemsSource = dataTable.DefaultView;

                LoadTableDetails(tableName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных таблицы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RefreshTablesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadTables();
        }


        private void LoadTableDetails(string tableName)
        {
            try
            {
                DataTable primaryKeyTable = sqlConnection.GetSchema("IndexColumns", new[] { null, null, tableName });
                string primaryKey = string.Join(", ", primaryKeyTable.Rows.Cast<DataRow>().Select(row => row["COLUMN_NAME"].ToString()));
                PrimaryKeyTextBlock.Text = $"Первичный ключ: {primaryKey}";

                IndexesListBox.Items.Clear();
                DataTable indexesTable = sqlConnection.GetSchema("Indexes", new[] { null, null, tableName });
                foreach (DataRow row in indexesTable.Rows)
                {
                    IndexesListBox.Items.Add(row["INDEX_NAME"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения информации о таблице: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sqlConnection != null && sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }

            Application.Current.Shutdown();
        }

        private void DataGridTableContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridTableContent.SelectedItem is DataRowView selectedRow)
            {
                MessageBox.Show($"Вы выбрали строку: {string.Join(", ", selectedRow.Row.ItemArray)}", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
