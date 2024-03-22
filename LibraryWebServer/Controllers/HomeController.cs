using LibraryWebServer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo( "TestProject1" )]
namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {

        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).
        private static string user = "";
        private static int card = -1;

        private readonly ILogger<HomeController> _logger;


        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin( string name, int cardnum )
        {

            //flag variable to determine login success
            bool loginSuccessful = false;

            using (Team4LibraryContext db = new Team4LibraryContext())
            {
                //pull a query matching the name and card num
                var query = from patron in db.Patrons
                            where patron.Name == name && patron.CardNum == cardnum
                            select patron;

                //if a query is returned, we flag it as a success
                if (query.Any())
                {
                    loginSuccessful = true;
                }
                
            }


            //if not success
            if ( !loginSuccessful )
            {
                return Json( new { success = false } ); // model will deal with the rest
            }
            else //if success
            {
                //set our globals accordinly
                user = name;
                card = cardnum;
                return Json( new { success = true } ); //and let the model know
            }
        }


        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";
            card = -1;
            return Json( new { success = true } );
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {

            JsonResult jsonQuery;

            using (Team4LibraryContext db = new Team4LibraryContext())
            {

                //we merge titles, inventory, checkedout, and patrons into one query
                var query = from t in db.Titles
                            join i in db.Inventory on t.Isbn equals i.Isbn into inv
                            from j1 in inv.DefaultIfEmpty()
                            join c in db.CheckedOut on j1.Serial equals c.Serial into chkOut
                            from j2 in chkOut.DefaultIfEmpty()
                            join p in db.Patrons on j2.CardNum equals p.CardNum into patrons
                            from j3 in patrons.DefaultIfEmpty()
                            select new
                            {
                                isbn = t.Isbn,
                                title = t.Title,
                                author = t.Author,
                                serial = j1 == null ? null : (uint?)j1.Serial, //and handle null serials
                                name = j3 == null ? "" : j3.Name ?? "" //as well as null names
                            };

                jsonQuery = Json(query.ToArray()); //we give the json array to the model

            }


            return jsonQuery;

        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {

            JsonResult jsonQuery;


            using (Team4LibraryContext db = new Team4LibraryContext())
            {
                //similar query to above, we just need to ensure that the cardnum matches the logged in card user
                var query =
                    from t in db.Titles
                    join i in db.Inventory on t.Isbn equals i.Isbn into inv
                    from j1 in inv.DefaultIfEmpty()
                    join c in db.CheckedOut on j1.Serial equals c.Serial into chkOut
                    from j2 in chkOut.DefaultIfEmpty()
                    join p in db.Patrons on j2.CardNum equals p.CardNum into patrons
                    from j3 in patrons.DefaultIfEmpty()
                    where j3.CardNum == card
                    select new
                    {
                        title = t.Title,
                        author = t.Author,
                        serial = j1.Serial,
                    };

                jsonQuery = Json(query.ToArray());

            }

            return jsonQuery;
        }


        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook( int serial )
        {
            using (Team4LibraryContext db = new Team4LibraryContext())
            {
                //all we do here is just created a checkedout object
                CheckedOut checkOut = new CheckedOut();
                checkOut.CardNum = (uint)card;
                checkOut.Serial = (uint)serial;

                //and add it into the checkedout table
                db.CheckedOut.Add(checkOut);

                db.SaveChanges();   

            }
                
            return Json( new { success = true } );
        }

        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook( int serial )
        {
            using (Team4LibraryContext db = new Team4LibraryContext())
            {
                //we extract a range with the card num and the serial number of the desired book to be removed 
                var query =
                    from c in db.CheckedOut
                    where c.Serial == serial && c.CardNum == (uint)card
                    select c;
                    
                //and remove it
                db.CheckedOut.RemoveRange(query);

                db.SaveChanges();
            }

                return Json( new { success = true } );
        }


        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/


        public IActionResult Index()
        {
            if ( user == "" && card == -1 )
                return View( "Login" );

            return View();
        }


        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if ( user == "" && card == -1 )
                return View( "Login" );

            return View();
        }

        public HomeController( ILogger<HomeController> logger )
        {
            _logger = logger;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache( Duration = 0, Location = ResponseCacheLocation.None, NoStore = true )]
        public IActionResult Error()
        {
            return View( new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier } );
        }
    }
}