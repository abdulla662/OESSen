using Microsoft.EntityFrameworkCore;
using OES.Core.DatabaseObjects.CommonInterfaces;
using OES.Infrastructure.Contexts;
using OES.Interface.Interfaces;
using System.Reflection;

namespace OES.API.Extensions
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabaseAsync(this WebApplication webApplication)
        {
            // WARNING: Missing with methods order can lead to errors when applying database objects.

            await ApplyMigrationsViewsSPsIfNotAsync(webApplication);

            await RunSystemSeedersAsync(webApplication);
        }

        private static async Task ApplyMigrationsViewsSPsIfNotAsync(WebApplication webApplication)
        {
            using var scope = webApplication?.Services.CreateScope();

            var dbContext = scope?.ServiceProvider.GetService<AppDbContext>();

            if (dbContext is not null)
            {
                // WARNING: Missing with methods order can lead to errors when applying database objects.

                //await dbContext.Database.MigrateAsync();

                await ApplyViewsInDatabaseAsync(dbContext);

                await ApplyFunctionsInDatabaseAsync(dbContext);

                await ApplyStoredProceduresInDatabaseAsync(dbContext);

                await ApplyEventsInDatabaseAsync(dbContext);
            }
        }

        private static async Task ApplyViewsInDatabaseAsync(AppDbContext dbContext)
        {
            var coreAssembly = Assembly.GetAssembly(typeof(IDatabaseView));

            var availableViewsTypes = coreAssembly?
                .GetTypes()
                .Where(t => typeof(IDatabaseView).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in availableViewsTypes ?? [])
            {
                if (Activator.CreateInstance(type) is IDatabaseView view)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(view.CreateOrReplaceCommand);
                }
            }
        }

        private static async Task ApplyFunctionsInDatabaseAsync(AppDbContext dbContext)
        {
            var coreAssembly = Assembly.GetAssembly(typeof(IDatabaseFunction));

            var availableFunctionTypes = coreAssembly?
                .GetTypes()
                .Where(t => typeof(IDatabaseFunction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in availableFunctionTypes ?? [])
            {
                if (Activator.CreateInstance(type) is IDatabaseFunction fn)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(fn.DropCommand);

                    await dbContext.Database.ExecuteSqlRawAsync(fn.CreateCommand);
                }
            }
        }

        private static async Task ApplyStoredProceduresInDatabaseAsync(AppDbContext dbContext)
        {
            var coreAssembly = Assembly.GetAssembly(typeof(IDatabaseStoredProcedure));

            var availableStoredProceduresTypes = coreAssembly?
                .GetTypes()
                .Where(t => typeof(IDatabaseStoredProcedure).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in availableStoredProceduresTypes ?? [])
            {
                if (Activator.CreateInstance(type) is IDatabaseStoredProcedure sp)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(sp.DropCommand);

                    await dbContext.Database.ExecuteSqlRawAsync(sp.CreateCommand);
                }
            }
        }

        public static async Task ApplyEventsInDatabaseAsync(AppDbContext dbContext)
        {
            var domainAssembly = Assembly.GetAssembly(typeof(IDatabaseEvent));

            var availableEventsTypes = domainAssembly?
                .GetTypes()
                .Where(t => typeof(IDatabaseEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in availableEventsTypes ?? [])
            {
                if (Activator.CreateInstance(type) is IDatabaseEvent _event)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(_event.CreateOrReplaceCommand);
                }
            }
        }

        private static async Task RunSystemSeedersAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var seederService = scope.ServiceProvider.GetRequiredService<ISeederService>();
            var memoryCacheService = scope.ServiceProvider.GetRequiredService<IAPIMemoryCach>();

            await seederService.SeedApiEndpoints();
            await seederService.SeedQuestionTypes();
            await seederService.ApplyQuestionTypeExclusionsAsync();
            await seederService.SeedPredefinedTemplatesAsync();
            await seederService.SeedQuestionLayout();
            await seederService.SeedItemBankLevel();
            await seederService.SeedSuperAdmin();
            await seederService.SeedTemplateTypesAttributesAsync();
            await seederService.SeedDifficultyProfileWithDifficultyLevels();
            await seederService.SeedLanguages();
            await seederService.SeedSubjects();
            await seederService.SeedMediaSettings();
            await seederService.SeedCBTSyncSettingAsync();
            await seederService.SeedPageRolesAsync();
            await seederService.SeedApiEndpointRolesAsync();
            await memoryCacheService.GetEndPointData(true);
        }
    }
}
