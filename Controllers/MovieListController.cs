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
        static private JSONDB db;
        static private JArray table;

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (db == null)
                db = new JSONDB(Path.Combine(requestContext.HttpContext.Server.MapPath("~"), "DB\\database.json"));

            //Load table
            if (table == null)
            {
                LoadTable(true);
            }
        }

        private void Wait(int interval, int max)
        {
            max = max / interval;
            int count = 0;
            while ((db.IsLocked || db.IsLockedOutside) && count++ < max)
            {
                Thread.Sleep(interval);
            }
                
        }

        private void LoadTable(bool _readonly = false)
        {
            if (!db.Load(_readonly)) throw new Exception();// Error reading/parsing the json db!
            table = (JArray)db.GetSection("Movies");
            if (table == null)
            {
                table = new JArray();
                db.SaveSection("Movies", table);
            }
        }

        // POST or GET: MovieList()
        public ActionResult Index(FormCollection collection)
        {
            LoadTable(true);
            var search = collection?["search_text"]?.ToLowerInvariant() ?? "";
            var sort = collection?["sort_type"] ?? "";

            return View( new MovieList() { search_key = search, sort_type = sort, 
                    Movies = table
                    .Select(row => (Movie)row.ToObject(typeof(Movie)))
                    .Where(x => x.GetType().GetFields().Any(y => x.GetType().GetField(y.Name).GetValue(x)?.ToString().ToLowerInvariant().Contains(search) ?? false))
                    .OrderByDescending(x => {
                        return x.GetType().GetField(sort)?.GetValue(x);
                    })}
                );
        }

        // GET: MovieList/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            JToken obj = table.SingleOrDefault(row => (int)row["key"] == id);

            if (obj == null)
                return RedirectToAction("Index");

            return View(obj.ToObject(typeof(Movie)));
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
                LoadTable();

                int last_key = table.Select(row => (int?)row["key"])?.Max() ?? 0;

                JObject newrow = new JObject();
                newrow.Add("key", last_key + 1);
                newrow.Add("Name", collection["Name"]);
                newrow.Add("Description", collection["Description"]);

                byte[] image = new byte[image_file.InputStream.Length];
                image_file.InputStream.Read(image, 0, (int)image_file.InputStream.Length);
                newrow.Add("Image", Convert.ToBase64String(image));

                table.Add(newrow);
                db.SaveSection("Movies", table);
                db.Save(true);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: MovieList/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            JToken obj = table.SingleOrDefault(row => (int)row["key"] == id);

            if (obj == null)
                return RedirectToAction("Index");

            return View(obj.ToObject(typeof(Movie)));
        }

        // POST: MovieList/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection, HttpPostedFileBase image_file)
        {
            try
            {
                LoadTable();

                JToken obj = table.Single(row => (int)row["key"] == id);
                obj["Name"] = collection["Name"];
                obj["Description"] = collection["Description"];

                if (image_file != null)
                {
                    byte[] image = new byte[image_file.InputStream.Length];
                    image_file.InputStream.Read(image, 0, (int)image_file.InputStream.Length);
                    obj["Image"] = Convert.ToBase64String(image);
                }

                db.SaveSection("Movies", table);
                db.Save(true);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: MovieList/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            JToken obj = table.SingleOrDefault(row => (int)row["key"] == id);

            if (obj == null)
                return RedirectToAction("Index");

            return View(obj.ToObject(typeof(Movie)));
        }

        // POST: MovieList/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                LoadTable();

                table.Single(row => (int)row["key"] == id).Remove();

                db.SaveSection("Movies", table);
                db.Save(true);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
