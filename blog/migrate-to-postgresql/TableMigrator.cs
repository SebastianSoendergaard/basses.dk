using Npgsql;
using System.Data.SqlClient;

namespace Postgresql.DataMigration
{
    internal class TableMigrator
    {
        private readonly string _sourceConnectionString;
        private readonly string _destinationConnectionString;
        private readonly Action<string> _logger;

        public TableMigrator(string sourceConnectionString, string destinationConnectionString, Action<string> logger)
        {
            _sourceConnectionString = sourceConnectionString;
            _destinationConnectionString = destinationConnectionString;
            _logger = logger;
        }

        public void Migrate(TableSettings tables, long startId, int chunkSize)
        {
            SqlConnection? sourceConnection = null;
            NpgsqlConnection? destinationConnection = null; 

            try
            {
                _logger($"Start migrating source=<{tables.SourceTableName}>, destination=<{tables.DestinationTableName}>, startId=<{startId}>, chunkSize=<{chunkSize}>");

                long id = startId;

                sourceConnection = new SqlConnection(_sourceConnectionString);
                destinationConnection = new NpgsqlConnection(_destinationConnectionString);

                destinationConnection.Open();
                sourceConnection.Open();

                var tableSize = GetSourceTableSize(sourceConnection, tables.SourceTableName, id);
                var chunkCount = 1;
                var totalChunkCount = (tableSize / chunkSize) + 1;

                _logger($"Source ({tables.SourceTableName}) tableSize=<{tableSize}>, chunkCounts=<{totalChunkCount}>");

                while (chunkCount <= totalChunkCount)
                {
                    _logger($"Migrating ({tables.SourceTableName}) chunk=<{chunkCount}/{totalChunkCount}>, startId=<{id}>");
                    id = MigrateChunk(sourceConnection, destinationConnection, tables, id, chunkSize);
                    id++;
                    chunkCount++;
                }

                _logger($"Reset destination table id sequence, destination=<{tables.DestinationTableName}>");
                ResetDestinationTableIdSequence(destinationConnection, tables.DestinationTableName);
            }
            finally
            {
                sourceConnection?.Close();
                destinationConnection?.Close();
            }
        }

        private int GetSourceTableSize(SqlConnection? connection, string tableName, long startId)
        {
            using var cmd = new SqlCommand($"select count(*) from {tableName} where id >= {startId}", connection);
            using var reader = cmd.ExecuteReader();
            int count = 0;
            while (reader.Read())
            {
                count = reader.GetInt32(0);
            }
            return count;
        }

        private long MigrateChunk(SqlConnection sourceConnection, NpgsqlConnection destinationConnection, TableSettings tables, long startId, int chunkSize)
        {
            using var cmd = new SqlCommand($"select top {chunkSize} * from {tables.SourceTableName} where id >= {startId}", sourceConnection);
            using var reader = cmd.ExecuteReader();
            return BulkInsert(destinationConnection, tables.DestinationTableName, tables.Columns, reader);
        }

        private long BulkInsert(NpgsqlConnection connection, string tableName, IList<Column>? columns, SqlDataReader reader)
        {
            long lastId = 0;
            var mapping = columns?.OfType<ColumnMapping>().ToDictionary(m => m.SourceColumnIndex);
            var defaults = columns?.OfType<ColumnDefault>().ToList();

            var sqlCopy = $"COPY {tableName} FROM STDIN (FORMAT BINARY)";

            using var writer = connection.BeginBinaryImport(sqlCopy);

            while (reader.Read())
            {
                if (mapping != null && defaults != null)
                {
                    WriteMappedFields(writer, reader, mapping, defaults);
                }
                else
                {
                    WriteRawFields(writer, reader);
                }
                lastId = (long)reader["id"];
            }

            writer.Complete();

            return lastId;
        }

        private void WriteRawFields(NpgsqlBinaryImporter writer, SqlDataReader reader)
        {
            writer.StartRow();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.GetValue(i);
                writer.Write(v);
            }
        }

        private void WriteMappedFields(NpgsqlBinaryImporter writer, SqlDataReader reader, IDictionary<int, ColumnMapping> columnMapping, IList<ColumnDefault> columnDefaults)
        {
            var values = new object[columnMapping.Count + columnDefaults.Count];

            foreach (var d in columnDefaults)
            {
                values[d.DestinationColumnIndex] = d.DefaultValue;
            }
            
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.GetValue(i);
                var mapping = columnMapping.TryGetValue(i, out var m) ? m : null;
                if (mapping != null)
                { 
                    values[mapping.DestinationColumnIndex] = mapping.Mapper?.Invoke(v) ?? v;
                }
            }

            writer.WriteRow(values);
        }

        private void ResetDestinationTableIdSequence(NpgsqlConnection connection, string tableName)
        {
            // Sequence name can be found with: SELECT c.relname, * FROM pg_class c WHERE c.relkind = 'S';
            using var cmd = new NpgsqlCommand($"select setval('{tableName}_id_seq', (select max(id) from {tableName}))", connection);
            cmd.ExecuteNonQuery();
        }
    }
}
