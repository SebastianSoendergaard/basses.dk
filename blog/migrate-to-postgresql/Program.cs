using Postgresql.DataMigration;

void Logger(string text) => Console.WriteLine($"{DateTime.Now:o}: {text}");
var settings = AppPushNotificationServiceMigrationSettings.CreateMigrationSettings();

new DataMigrator(settings, Logger).Migrate();
