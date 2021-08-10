using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DornMovieApp.Models
{
    public class MovieList
    {
        public string search_key;
        public string sort_type;
        public IEnumerable<Movie> Movies;
    }
}