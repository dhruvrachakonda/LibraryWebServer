using LibraryWebServer.Controllers;
using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject1
{
    public class UnitTest1
    {
        // Uncomment this once you have scaffolded your library, 
        // then replace instances of 'X' below with your team number

        Team4LibraryContext MakeTinyDB()
        {
            var contextOptions = new DbContextOptionsBuilder<Team4LibraryContext>()
            .UseInMemoryDatabase( "LibraryControllerTest" )
            .ConfigureWarnings( b => b.Ignore( InMemoryEventId.TransactionIgnoredWarning ) )
            .UseApplicationServiceProvider( NewServiceProvider() )
           .Options;

            var db = new Team4LibraryContext(contextOptions);

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            Titles t = new Titles();
            t.Author = "Fake author";
            t.Title = "Fake title";
            t.Isbn = "123-5678901234";

            db.Titles.Add( t );
            db.SaveChanges();

             return db;
        }


        


        private static ServiceProvider NewServiceProvider()
        {
            var serviceProvider = new ServiceCollection()
          .AddEntityFrameworkInMemoryDatabase()
          .BuildServiceProvider();

            return serviceProvider;
        }
    }
}