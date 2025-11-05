using Microsoft.Extensions.DependencyInjection;
using MySvelteApp.Server.Infrastructure.Persistence;
using Xunit;

namespace MySvelteApp.Server.Tests.TestFixtures;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no implementation, it's just a marker for the collection
}
