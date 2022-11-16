namespace Postgresql.DataMigration
{
    internal static class PizzaServiceMigrationSettings
    {
        public static MigrationSettings CreateMigrationSettings()
        {
            return new MigrationSettings
            {
                SourceConnectionString = "Server=127.0.0.1,55001;User ID=xxxuser;PWD=xxxpass;Database=PizzaService;",
                DestinationConnectionString = "Server=127.0.0.1;Port=5433;User Id=xxxuser;Password=xxpass;Database=pizza_service",
                Tables = new[]
                {
                    CreateToppingSettings(),
                    CreatePizzaSettings()
                }
            };
        }

        private static TableSettings CreateToppingSettings()
        {
            return new TableSettings
            {
                SourceTableName = "Toppings",
                DestinationTableName = "toppings",
                Columns = new Column[]
                {
                    new ColumnMapping(0, 0), // Copy column 0 -> column 0
                    new ColumnMapping(1, 2, ToUtc), // Copy column 2 -> column 1, convert from local time to utc
                    new ColumnMapping(2, 3)
                }
            };
        }

        private static TableSettings CreatePizzaSettings()
        {
            return new TableSettings
            {
                SourceTableName = "Pizzas",
                DestinationTableName = "pizzas",
                Columns = new Column[]
                {
                    new ColumnMapping(0, 5), // Copy column 5 -> column 0
                    new ColumnDefault(1, null), // Set column 1 to null, value does not exist in source
					new ColumnDefault(2, 0)	// Set column 2 to 0, value does not exist in source
                }
            };
        }

        private static object? ToUtc(object? dt)
        {
            if (dt == null || dt is DBNull)
            {
                return null;
            }
            return ((DateTime)dt).ToUniversalTime();
        }
    }
}
