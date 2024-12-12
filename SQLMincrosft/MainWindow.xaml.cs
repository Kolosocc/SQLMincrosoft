using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Data.SqlClient;

namespace DatabaseApp
{
    public partial class MainWindow : Window
    {
        private string currentConnectionString;
        private SqlConnection sqlConnection;
        private bool isEditing = false;

        public MainWindow()
        {
            InitializeComponent();
            currentConnectionString = "Data Source=DBSRV\\ag2024;Initial Catalog=KolosovDA_2207_g2_lab16;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
            sqlConnection = new SqlConnection(currentConnectionString);
        }

        private void LoadTableDetails(string tableName)
        {
            try
            {
                // Загружаем информацию о первичном ключе
                string primaryKeyQuery = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_NAME = @tableName AND CONSTRAINT_NAME LIKE 'PK%'";

                SqlCommand primaryKeyCommand = new SqlCommand(primaryKeyQuery, sqlConnection);
                primaryKeyCommand.Parameters.AddWithValue("@tableName", tableName);

                SqlDataReader reader = primaryKeyCommand.ExecuteReader();
                if (reader.Read())
                {
                    PrimaryKeyTextBlock.Text = "Первичный ключ: " + reader.GetString(0);
                }
                else
                {
                    PrimaryKeyTextBlock.Text = "Первичный ключ: Не найден";
                }

                reader.Close();

                // Загружаем информацию о внешних ключах
                string foreignKeyQuery = @"
            SELECT kcu.COLUMN_NAME, ccu.TABLE_NAME AS REFERENCED_TABLE, ccu.COLUMN_NAME AS REFERENCED_COLUMN
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc ON kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
            JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON rc.UNIQUE_CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
            WHERE kcu.TABLE_NAME = @tableName";

                SqlCommand foreignKeyCommand = new SqlCommand(foreignKeyQuery, sqlConnection);
                foreignKeyCommand.Parameters.AddWithValue("@tableName", tableName);

                SqlDataReader foreignKeyReader = foreignKeyCommand.ExecuteReader();
                List<string> foreignKeys = new List<string>();
                while (foreignKeyReader.Read())
                {
                    string foreignKey = $"Столбец: {foreignKeyReader["COLUMN_NAME"]}, " +
                                        $"Ссылается на таблицу: {foreignKeyReader["REFERENCED_TABLE"]}, " +
                                        $"Столбец: {foreignKeyReader["REFERENCED_COLUMN"]}";
                    foreignKeys.Add(foreignKey);
                }

                // Отображаем внешний ключ
                IndexesListBox.Items.Clear();
                if (foreignKeys.Count > 0)
                {
                    foreach (string foreignKey in foreignKeys)
                    {
                        IndexesListBox.Items.Add(foreignKey);
                    }
                }
                else
                {
                    IndexesListBox.Items.Add("Внешние ключи не найдены");
                }

                foreignKeyReader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о ключах: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void SelectDatabaseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectDatabaseWindow = new SelectDatabaseWindow();
            if (selectDatabaseWindow.ShowDialog() == true)
            {
                currentConnectionString = selectDatabaseWindow.SelectedConnectionString;
                if (sqlConnection.State == ConnectionState.Open)
                {
                    sqlConnection.Close();
                }

                try
                {
                    sqlConnection.ConnectionString = currentConnectionString;
                    sqlConnection.Open();
                    MessageBox.Show("Подключение установлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadTables();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshTablesMenuItem_Click(object sender, RoutedEventArgs e) => LoadTables();

        private void LoadTables()
        {
            if (sqlConnection == null || sqlConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Сначала подключитесь к базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
                    sqlConnection);
                SqlDataReader reader = cmd.ExecuteReader();

                TablesListBox.Items.Clear();
                while (reader.Read())
                {
                    string schema = reader.GetString(0);
                    string tableName = reader.GetString(1);
                    TablesListBox.Items.Add($"{schema}.{tableName}");
                }

                reader.Close();

                if (TablesListBox.Items.Count == 0)
                {
                    MessageBox.Show("В базе данных нет доступных таблиц.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблиц: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Загружаем данные из выбранной таблицы
                string query = $"SELECT * FROM {tableName}";
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, sqlConnection);
                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);

                // Привязываем DataTable к DataGrid
                DataGridTableContent.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных таблицы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateNewItem_Click(object sender, RoutedEventArgs e)
        {
            if (TablesListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите таблицу перед добавлением новой записи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedTable = TablesListBox.SelectedItem.ToString().Split('.').Last();
            var addNewItemWindow = new AddNewItemWindow(selectedTable, sqlConnection);
            addNewItemWindow.ShowDialog();

            // Перезагрузить данные после добавления
            LoadTableContent(selectedTable);
        }

        private void DataGridTableContent_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!isEditing && e.Row.Item is DataRowView rowView)
            {
                isEditing = true;

                // Получаем имя таблицы
                string tableName = TablesListBox.SelectedItem.ToString().Split('.').Last();

                // Получаем имя первого столбца таблицы
                string firstColumnName = GetFirstColumnName(tableName);

                if (firstColumnName == null)
                {
                    MessageBox.Show("Не удалось определить имя первого столбца таблицы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    isEditing = false;
                    return;
                }

                // Получаем ID из строки
                int id = Convert.ToInt32(rowView[firstColumnName]);

                // Пытаемся получить имя столбца, который редактируем
                if (e.Column is DataGridBoundColumn boundColumn)
                {
                    string editedColumnName = ((Binding)boundColumn.Binding).Path.Path;

                    // Получаем новое значение из редактируемого элемента (например, TextBox)
                    object newValue = ((TextBox)e.EditingElement).Text;

                    try
                    {
                        // Обновляем данные в базе данных
                        string query = $"UPDATE {tableName} SET {editedColumnName} = @newValue WHERE {firstColumnName} = @id";
                        SqlCommand command = new SqlCommand(query, sqlConnection);
                        command.Parameters.AddWithValue("@newValue", newValue);
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                isEditing = false;
            }
        }

        private string GetFirstColumnName(string tableName)
        {
            try
            {
                // Запрос для получения имени первого столбца
                string query = $@"
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = @tableName 
            ORDER BY ORDINAL_POSITION 
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

                SqlCommand command = new SqlCommand(query, sqlConnection);
                command.Parameters.AddWithValue("@tableName", tableName);

                // Выполняем запрос и получаем имя первого столбца
                object result = command.ExecuteScalar();

                return result?.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении первого столбца: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        private void DeleteSelectedRow_Click(object sender, RoutedEventArgs e)
        {
            if (TablesListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите таблицу перед удалением записи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataGridTableContent.SelectedItem is DataRowView rowView)
            {
                string tableName = TablesListBox.SelectedItem.ToString().Split('.').Last();

                // Получаем имя первого столбца, который будет использоваться как ID
                string firstColumnName = GetFirstColumnName(tableName);

                if (firstColumnName == null)
                {
                    MessageBox.Show("Не удалось определить имя первого столбца таблицы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    // Получаем значение ID из первой колонки
                    int id = Convert.ToInt32(rowView[firstColumnName]);

                    // Формируем SQL запрос для удаления записи
                    string query = $"DELETE FROM {tableName} WHERE {firstColumnName} = @id";
                    SqlCommand command = new SqlCommand(query, sqlConnection);
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();

                    MessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Перезагружаем таблицу после удаления записи
                    LoadTableContent(tableName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TablesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesListBox.SelectedItem != null)
            {
                string selectedTable = TablesListBox.SelectedItem.ToString().Split('.').Last();
                LoadTableContent(selectedTable);
                LoadTableDetails(selectedTable);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}
