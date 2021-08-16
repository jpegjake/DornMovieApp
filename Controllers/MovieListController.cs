using DornMovieApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DornMovieApp.Controllers
{
    public class MovieListController : Controller
    {
        private DornMovieDBModel movieDB;
        private IEnumerable<Movie> movies_table;
        //private JArray table;

        protected override void Initialize(RequestContext requestContext)
        {            
            base.Initialize(requestContext);
            movieDB = DornMovieDBModel.GetInstance(Path.Combine(requestContext.HttpContext.Server.MapPath("~"), "DB\\database.json"));

            try
            {
                //Load table
                movies_table = movieDB.Movies.LoadTable();
            }
            catch (JSONDB.JSONDBException e)
            {
                ModelState.AddModelError("general", "Database deadlock timeout error. Please try again.");
                throw;
            }
            catch (Exception e)
            {
                ModelState.AddModelError("general", "Unknown database error occurred.");
                throw;
            }
        }

        // POST or GET: MovieList()
        public ActionResult Index(FormCollection collection)
        {            
            var moviesList = new MovieList();
            var search = collection?["search_text"]?.ToLowerInvariant() ?? "";
            var sort = collection?["sort_type"] ?? "";

            moviesList.search_key = search;
            moviesList.sort_type = sort;
            moviesList.Movies = new List<Movie>();

            if (ModelState.IsValidField("general"))
            {
                try
                {
                    moviesList.Movies = movies_table?
                            .Where(x => x.GetType().GetFields().Any(y => x.GetType().GetField(y.Name).GetValue(x)?.ToString().ToLowerInvariant().Contains(search) ?? false))
                            .OrderByDescending(x =>
                            {
                                return x.GetType().GetField(sort)?.GetValue(x);
                            });

                    if (moviesList.Movies == null)
                        moviesList.Movies = new List<Movie>();

                }
                catch (Exception e)
                {
                    ModelState.AddModelError("general", "Unknown database error occurred.");
                }
            }
            return View(moviesList);
        }

        // GET: MovieList/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                ModelState.AddModelError("general", "Movie key not found!");
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValidField("general"))
            {
                return View(id);
            }
            Movie obj = movieDB.Movies.Table.SingleOrDefault(row => row.key == id);

            if (obj == null)
            {
                ModelState.AddModelError("general", "Movie key not found!");
                return RedirectToAction("Index");
            }

            return View(obj);
        }

        // GET: MovieList/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: MovieList/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection, HttpPostedFileBase image_file)
        {
            try
            {
                Movie newrow = new Movie() {
                    Name= collection["Name"],
                    Description= collection["Description"]};

                byte[] image = new byte[image_file.InputStream.Length];
                image_file.InputStream.Read(image, 0, (int)image_file.InputStream.Length);
                newrow.Image = Convert.ToBase64String(image);

                movieDB.Movies.Add<Movie>(newrow,"key");
                movieDB.Commit();

                return RedirectToAction("Index");
            }
            catch (JSONDB.JSONDBException e)
            {
                ModelState.AddModelError("general", "Database deadlock timeout error. Please try again.");
                return View();
            }
            catch (Exception e)
            {
                ModelState.AddModelError("general", "Unknown database error occurred.");
                return View();
            }
        }

        // GET: MovieList/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                ModelState.AddModelError("general", "Movie key not found!");
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValidField("general"))
            {
                return View(id);
            }
            Movie obj = movies_table.SingleOrDefault(row => row.key == id);

            if (obj == null)
            {
                ModelState.AddModelError("general", "Movie key not found!");
                return RedirectToAction("Index");
            }

            return View(obj);
        }

        // POST: MovieList/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection, HttpPostedFileBase image_file)
        {
            try
            {                Movie obj = movieDB.Movies.Table.Single(row => row.key == id);
                obj.Name = collection["Name"];
                obj.Description = collection["Description"];

                if (image_file != null)
                {
                    byte[] image = new byte[image_file.InputStream.Length];
                    image_file.InputStream.Read(image, 0, (int)image_file.InputStream.Length);
                    obj.Image = Convert.ToBase64String(image);
                }

                movieDB.Movies.Edit("key", obj);
                movieDB.Commit();

                return RedirectToAction("Index");
            }
            catch (JSONDB.JSONDBException e)
            {
                ModelState.AddModelError("general", "Database deadlock timeout error. Please try again.");
                return View();
            }
            catch (Exception e)
            {
                ModelState.AddModelError("general", "Unknown database error occurred.");
                return View();
            }
        }

        // GET: MovieList/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                ModelState.AddModelError("general", "Movie key not found!");
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValidField("general"))
            {
                return View(id);
            }
            Movie obj = movies_table.SingleOrDefault(row => row.key == id);

            if (obj == null)
            {
                ModelState.AddModelError("general", "Movie key not found!");
                return RedirectToAction("Index");
            }

            return View(obj);
        }

        // POST: MovieList/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                movieDB.Movies.Delete("key",movies_table.Single(row => row.key == id));

                movieDB.Commit();

                return RedirectToAction("Index");
            }
            catch (JSONDB.JSONDBException e)
            {
                ModelState.AddModelError("general", e.Message);
                return View();
            }
            catch (Exception e)
            {
                ModelState.AddModelError("general", "Unknown database error occurred.");
                return View();
            }
        }
    }
}
