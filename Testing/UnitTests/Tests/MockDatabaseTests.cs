using NUnit.Framework;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using System.Diagnostics;

namespace UnitTests.Tests;

[TestFixture]
public class MockDatabaseTests
{
    private DatabaseService _databaseService;
    private DatabaseServer _databaseServer;

    [OneTimeSetUp]
    public void Initialize()
    {
        var diContainer = DI.GetInstance();
        _databaseService = diContainer.GetService<DatabaseService>();
        _databaseServer = diContainer.GetService<DatabaseServer>();
    }

    [Test]
    public void DatabaseServer_Should_Have_Tables()
    {
        try
        {
            var tables = _databaseServer.GetTables();
            Assert.That(tables, Is.Not.Null, "DatabaseServer.GetTables() returned null");
            Debug.WriteLine("[TEST] Database tables successfully retrieved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST] Failed to get tables: {ex.Message}");
            throw;
        }
    }

    [Test]
    public void DatabaseService_Should_Have_Settings()
    {
        try
        {
            var settings = _databaseService.GetSettings();
            Assert.That(settings, Is.Not.Null, "DatabaseService.GetSettings() returned null");
            Debug.WriteLine("[TEST] Settings successfully retrieved");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST] Failed to get settings: {ex.Message}");
            throw;
        }
    }

    [Test]
    public void DatabaseService_Should_Have_Items()
    {
        try
        {
            var items = _databaseService.GetItems();
            Assert.That(items, Is.Not.Null, "DatabaseService.GetItems() returned null");
            Debug.WriteLine($"[TEST] Items successfully retrieved, count: {items?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST] Failed to get items: {ex.Message}");
            throw;
        }
    }
}
