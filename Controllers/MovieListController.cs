using DornMovieApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;

namespace DornMovieApp.Controllers
{
    public class MovieListController : AsyncController
    {
        private DornMovieDBModel movieDB;
        private IEnumerable<Movie> movies_table;
        //private JArray table;
        private Task init;
        private Task<ActionResult> req;

        public MovieListController() : base()
        {
            try
            { 
                movieDB = DornMovieDBModel.GetInstance(Path.Combine(HostingEnvironment.MapPath("~"), "DB\\database.json"));
                init = Task.Run(() => movieDB.Movies.LoadTableAsync()).ContinueWith((task) =>
                {
                    movies_table = task.Result;
                });

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
        public async Task<ActionResult> Index(FormCollection collection)
        {
            return await Task.Run(() =>
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
                        init?.Wait();
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
            });
        } 

        // GET: MovieList/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            return await Task.Run(() =>
            {
                if (id == null)
                {
                    ModelState.AddModelError("general", "Movie key not found!");
                    return (ActionResult)RedirectToAction("Index");
                }

                if (!ModelState.IsValidField("general"))
                {
                    return View(id);
                }

                init?.Wait();
                Movie obj = movieDB.Movies.Table.SingleOrDefault(row => row.key == id);

                if (obj == null)
                {
                    ModelState.AddModelError("general", "Movie key not found!");
                    return RedirectToAction("Index");
                }

                return View(obj);
            });
        }

        // GET: MovieList/Create
        public async Task<ActionResult> Create()
        {
            return await Task.Run(() => { return View(); });
        }

        // POST: MovieList/Create
        [HttpPost]
        public async Task<ActionResult> Create(FormCollection collection, HttpPostedFileBase image_file)
        {

            return await Task.Run(() =>
            {
                try
                {
                    Movie newrow = new Movie()
                    {
                        Name = collection["Name"],
                        Description = collection["Description"]
                    };

                    byte[] image = new byte[image_file.InputStream.Length];
                    image_file.InputStream.Read(image, 0, (int)image_file.InputStream.Length);
                    newrow.Image = Convert.ToBase64String(image);

                    init?.Wait();
                    movieDB.Movies.Add<Movie>(newrow, "key");
                    movieDB.Commit();

                    return (ActionResult)RedirectToAction("Index");
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
            });
        }

        // GET: MovieList/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {

            return await Task.Run(() =>
            {
                if (id == null)
                {
                    ModelState.AddModelError("general", "Movie key not found!");
                    return (ActionResult)RedirectToAction("Index");
                }

                if (!ModelState.IsValidField("general"))
                {
                    return View(id);
                }

                init?.Wait();
                Movie obj = movies_table.SingleOrDefault(row => row.key == id);

                if (obj == null)
                {
                    ModelState.AddModelError("general", "Movie key not found!");
                    return RedirectToAction("Index");
                }

                return View(obj);
            });
        }

        // POST: MovieList/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(int id, FormCollection collection, HttpPostedFileBase image_file)
        {

            return await Task.Run(() =>
            {
                try
                {
                    Movie obj = movieDB.Movies.Table.Single(row => row.key == id);
                    obj.Name = collection["Name"];
                    obj.Description = collection["Description"];

                    if (image_file != null)
                    {
                        byte[] image = new byte[image_file.InputStream.Length];
                        image_file.InputStream.Read(image, 0, (int)image_file.InputStream.Length);
                        obj.Image = Convert.ToBase64String(image);
                    }

                    init?.Wait();
                    movieDB.Movies.Edit("key", obj);
                    movieDB.Commit();

                    return (ActionResult)RedirectToAction("Index");
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
            });
        }

        // GET: MovieList/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {

            return await Task.Run(() =>
            {
                if (id == null)
                {
                    ModelState.AddModelError("general", "Movie key not found!");
                    return (ActionResult)RedirectToAction("Index");
                }

                if (!ModelState.IsValidField("general"))
                {
                    return View(id);
                }

                init?.Wait();
                Movie obj = movies_table.SingleOrDefault(row => row.key == id);

                if (obj == null)
                {
                    ModelState.AddModelError("general", "Movie key not found!");
                    return RedirectToAction("Index");
                }

                return View(obj);
            });
        }

        // POST: MovieList/Delete/5
        [HttpPost]
        public async Task<ActionResult> Delete(int id, FormCollection collection)
        {
            return await Task.Run(() =>
            {
                try
                {

                    init?.Wait();
                    movieDB.Movies.Delete("key", movies_table.Single(row => row.key == id));

                    movieDB.Commit();

                    return (ActionResult)RedirectToAction("Index");
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
            });
        }
    }
}
