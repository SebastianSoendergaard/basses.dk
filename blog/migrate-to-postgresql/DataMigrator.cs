namespace Postgresql.DataMigration
{
    internal class DataMigrator
    {
        private readonly MigrationSettings _settings;
        private readonly Action<string> _logger;

        public DataMigrator(MigrationSettings settings, Action<string> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public void Migrate()
        {
            var migrator = new TableMigrator(_settings.SourceConnectionString, _settings.DestinationConnectionString, _logger);
            var startTime = DateTime.Now;

            try
            {
                foreach (var table in _settings.Tables)
                {
                    migrator.Migrate(table, table.StartId, table.ChunkSize);
                }

                _logger($"All tables has been migrated, processingTime=<{DateTime.Now - startTime}>");
            }
            catch (Exception ex)
            {
                _logger($"{ex.Message} - {ex.StackTrace}");
            }
        }
    }
}
