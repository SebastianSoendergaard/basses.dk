namespace Postgresql.DataMigration
{
    internal class MigrationSettings
    {
        public string SourceConnectionString { get; set; } = "";
        public string DestinationConnectionString { get; set; } = "";
        public IList<TableSettings> Tables { get; set; } = new List<TableSettings>();
    }

    internal class TableSettings
    {
        /// <summary>
        /// Name of the SQL Server source table or view
        /// </summary>
        public string SourceTableName { get; set; } = "";
        /// <summary>
        /// Name of the PostgreSQL destination table
        /// </summary>
        public string DestinationTableName { get; set; } = "";
        /// <summary>
        /// Optional:
        /// List of columns that should be mapped from the source to the destination. Only the columns defined will be migrated.
        /// If no mapping is defined all columns in the source are copied raw to the destination in the order defined by the source.
        /// </summary>
        public IList<Column>? Columns { get; set; }
        /// <summary>
        /// ID of the row in source table to start with. This can be used to run again if new rows has been added.
        /// </summary>
        public long StartId { get; set; } = 0;
        /// <summary>
        /// How many rows should be fetched from source and processed at the time. 
        /// If this number is too low performance may be low but if the number is too high we might start seeing memory issues.
        /// </summary>
        public int ChunkSize { get; set; } = 10000;
    }

    internal interface Column
    { 
    
    }

    internal class ColumnMapping : Column
    {
        public ColumnMapping()
        { 
        }

        public ColumnMapping(int destinationColumnIndex, int sourceColumnIndex, Func<object?, object?>? mapper = null)
        {
            DestinationColumnIndex = destinationColumnIndex;
            SourceColumnIndex = sourceColumnIndex;
            Mapper = mapper;
        }

        /// <summary>
        ///  The index of the column in the source table
        /// </summary>
        public int SourceColumnIndex { get; set; }
        /// <summary>
        /// The index of the column at the destination that values should be migrated to
        /// </summary>
        public int DestinationColumnIndex { get; set; }
        /// <summary>
        /// Optional: If values needs special handling before inserting into destination table this can be defined here.
        /// If no mapping is defined the raw source value will be copied to the destination.
        /// </summary>
        public Func<object?, object?>? Mapper { get; set; }
    }

    internal class ColumnDefault : Column
    {
        public ColumnDefault()
        { 
        }

        public ColumnDefault(int destinationColumnIndex, object? defaultValue)
        {
            DestinationColumnIndex = destinationColumnIndex;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// The index of the column at the destination that values should be migrated to
        /// </summary>
        public int DestinationColumnIndex { get; set; }
        /// <summary>
        /// The value that should be written as default
        /// </summary>
        public object? DefaultValue { get; set; }
    }
}
