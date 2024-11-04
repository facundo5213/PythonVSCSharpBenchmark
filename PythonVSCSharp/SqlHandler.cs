using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonVSCSharp
{
    public class SqlHandler
    {
        private string _connectionString;
        private string _tableName;

        public SqlHandler(string tableName)
        {
            _connectionString = $"Data Source=Juan;Initial Catalog=PythonSharpBM;Integrated Security=True;TrustServerCertificate=True";
            _tableName = tableName;
        }

        public void CreateTableIfNotExists()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string createTableQuery = $@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{_tableName}')
                CREATE TABLE {_tableName} (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    content_string NVARCHAR(MAX),
                    article_title NVARCHAR(255),
                    full_section_title NVARCHAR(255),
                    block_type NVARCHAR(255),
                    language NVARCHAR(50),
                    last_edit_date NVARCHAR(255),
                    num_tokens INT,
                    unique_id NVARCHAR(255)
                )";
                SqlCommand command = new SqlCommand(createTableQuery, connection);
                command.ExecuteNonQuery();
            }
        }

        public void UploadToSql(DataTable chunk)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);
                bulkCopy.DestinationTableName = _tableName;

                connection.Open();
                try
                {
                    bulkCopy.BulkCopyTimeout = 600;  // Aumentar el tiempo de espera a 10 minutos
                    bulkCopy.WriteToServer(chunk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar el chunk en SQL Server: {ex.Message}");
                }
            }
        }
    }
}
