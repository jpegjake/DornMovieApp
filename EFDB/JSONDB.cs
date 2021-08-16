using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DornMovieApp
{
    public class DornMovieDBModel
    {
        private static DornMovieDBModel singleInstance;
        private JSONDB db;
        private JSONDBTable<DornMovieApp.Models.Movie> _movies;

        private DornMovieDBModel(string path)
        {
            db = new JSONDB(path);
        }

        public static DornMovieDBModel GetInstance(string path)
        {
            if (singleInstance != null && singleInstance.Path == path)
                return singleInstance;
            return (singleInstance = new DornMovieDBModel(path));
        }

        public JSONDBTable<DornMovieApp.Models.Movie> Movies 
        {
            get { return _movies ?? 
                    (_movies = new JSONDBTable<Models.Movie>(db, "Movies"));
            }
        }

        public string Path
        {
            get { return db.Path; }
        }

        public void Commit()
        {
            _movies.SaveChanges();
        }
    }

    public class JSONDBTable<TEntity> : IDisposable where TEntity : class
    {
        private JSONDB db;
        private IEnumerable<TEntity> table;
        private bool disposedValue;
        string _tableName;

        public JSONDBTable(JSONDB db, string tableName)
        {
            this.db = db;
            _tableName = tableName;
            //table = LoadTable();
        }

        public void ChangeTableName(string tableName)
        {
            _tableName = tableName;
        }

        public void SaveChanges()
        {
            db.SaveSection(_tableName, table);
            db.Save(true);
        }

        public IEnumerable<TEntity> Table
        {
            get { return table; }
        }

        public IEnumerable<TEntity> LoadTable(bool lockDB = false)
        {
            table = this.SelectAll(lockDB);
            return table;
        }

        private List<TEntity> SelectAll(bool lockDB)
        {
            return LoadTableJson(lockDB).Select(row => (TEntity)row.ToObject(typeof(TEntity))).ToList();
        }

        public JArray LoadTableJson(bool lockDB = false)
        {
            if (!db.Load(lockDB)) throw new Exception("Error reading database.");// Error reading/parsing the json db!
            JArray table = (JArray)db.GetSection(_tableName);
            if (table == null)
            {
                table = new JArray();
                db.SaveSection(_tableName, table);
            }
            return table;
        }

        public void Edit(string keyFieldName, TEntity objToEdit)
        {
            db.TryLock();

            object keyToEdit = objToEdit.GetType().GetField(keyFieldName).GetValue(objToEdit);

            //change the data
            table = table.Select(row =>
            {
                object keyval = row.GetType().GetField(keyFieldName).GetValue(row);
                
                if (keyToEdit.Equals(keyval))
                    return objToEdit;
                else return row;
            }
            );
        }

        public void EditRange(string keyFieldName, IEnumerable<TEntity> objsToEdit)
        {
            db.TryLock();

            IEnumerable<object> keysToEdit = objsToEdit.Select(x => x.GetType().GetField(keyFieldName).GetValue(x));

            //change the data
            table = table.Select(row =>
            {
                object keyval = row.GetType().GetField(keyFieldName).GetValue(row);
                if (keysToEdit.Contains(keyval)) return objsToEdit.Single(x => keyval == x.GetType().GetField(keyFieldName).GetValue(x));
                else return row;
            }
            );
        }

        public void Delete(string keyFieldName, TEntity objToDel)
        {
            db.TryLock();

            object keyToDel = objToDel.GetType().GetField(keyFieldName).GetValue(objToDel);

            // keep only the non-matches
            table = table.Where(row => !keyToDel.Equals(row.GetType().GetField(keyFieldName).GetValue(row)) );
        }
        public void DeleteRange(string keyFieldName, IEnumerable<TEntity> objsToDel)
        {
            db.TryLock();

            IEnumerable<object> keysToDel = objsToDel.Select(x => x.GetType().GetField(keyFieldName).GetValue(x));

            // keep only the non-matches
            table = table.Where(row => !keysToDel.Contains(row.GetType().GetField(keyFieldName).GetValue(row)));
        }

        public void Add<TEnityt>(TEntity obj, string autoIncrementKeyName = null)
        {
            db.TryLock();

            if (autoIncrementKeyName != null)
            {
                int last_key = 0;
                if (table != null && table.Count() > 0)
                    last_key = table?.Max(row => (int)(row.GetType().GetField(autoIncrementKeyName).GetValue(row))) ?? 0;
                obj.GetType().GetField(autoIncrementKeyName).SetValue(obj, last_key + 1);
            }

            table = table.Concat(new TEntity[] { obj });
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// This is a JSON and file based DB class
    /// Supply a file path to load from and save to
    /// Table structure can contain JToken's or any string values
    /// </summary>
    public class JSONDB : IDisposable
    {
        public JObject data;
        private string filepath;
        private FileStream lockfile;

        /// <summary>
        /// Returns a boolean to indicate if the lock has been obtained on the disk for reading and writing the Intermediate Storage data
        /// </summary>
        public bool IsLocked
        { get; private set; }

        public string Path
        { get { return filepath; } }

        /// <summary>
        /// Instantiate and give the file path for Load and Save of the Intermediate Storage data
        /// </summary>
        /// <param name="path"></param>
        public JSONDB(string path)
        {
            filepath = path;
        }

        /// <summary>
        /// Retrieves a custom section, or creates it and returns the JToken
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public JToken GetSection(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    return JToken.FromObject(data[name]);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            throw new JSONDBException("JSONDB Error: Cannot GetSection using an Null or Whitespace string.");
        }
        /// <summary>
        /// Retrieves a custom section, or creates it and returns the JToken
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetSection<T>(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(data[name].ToString());
                }
                catch (Exception ex)
                {
                    return default(T);
                }
            }
            throw new JSONDBException("JSONDB Error: Cannot GetSection using an Null or Whitespace string.");
        }

        /// <summary>
        /// Saves a custom section back (not saved to disk)
        /// </summary>
        /// <param name="table"></param>
        public void SaveSection(string name, JToken table)
        {
            if (!string.IsNullOrWhiteSpace(name))
                data[name] = table;
            else
                throw new JSONDBException("JSONDB Error: Cannot SaveSection using an Null or Whitespace string.");
        }

        /// <summary>
        /// Saves a custom section back (not saved to disk)
        /// </summary>
        /// <param name="table"></param>
        public void SaveSection(string name, object obj)
        {
            if (!string.IsNullOrWhiteSpace(name))
                data[name] = JToken.FromObject(obj);
            else
                throw new JSONDBException("JSONDB Error: Cannot SaveSection using an Null or Whitespace string.");
        }

        private bool _readonly;
        /// <summary>
        /// Load from json string in file
        /// </summary>
        /// <param name="path"></param>
        public bool Load(bool lockDB = false)
        {
            if (!lockDB || TryLock())
            {
                _readonly = lockDB;
                string data;
                if (!File.Exists(filepath))
                    File.Create(filepath);
                using (var sr = new StreamReader(File.Open(filepath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)))
                    data = sr.ReadToEnd();

                JObject temp;
                try
                {
                    if (!string.IsNullOrWhiteSpace(data))
                        temp = JObject.Parse(data);
                    else
                        temp = new JObject();
                }
                catch
                {
                    temp = new JObject();
                }

                this.data = temp;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try to create a lock file or wait for access to be granted when the previous user is done.
        /// </summary>
        /// <returns>boolean result of whether the database was successfully locked</returns>
        /// 
        Mutex mutex = new Mutex(false, "DBlock");
        internal bool TryLock()
        {
            if (IsLocked)
                return true;
            try
            {
                if (IsLockedOutside)
                    throw new JSONDBException("JSONDB Error: Database is currently locked by third-party.");
                else if (mutex.WaitOne(5000))
                {
                    lockfile = new FileStream(filepath + ".lock", FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
                    IsLocked = true;
                    _readonly = false;
                }
                else
                    throw new JSONDBTimeOutException();
            }
            catch (Exception ex)
            {
                IsLocked = false;
                throw;
            }
            return IsLocked;
        }

        /// <summary>
        /// Unlock the database
        /// </summary>
        public void UnLock()
        {
            lockfile?.Close();
            if (File.Exists(filepath + ".lock"))
                File.Delete(filepath + ".lock");
            IsLocked = false;
            try { mutex.ReleaseMutex(); }
            catch { }
        }

        /// <summary>
        /// Detect outside locked database
        /// </summary>
        public bool IsLockedOutside
        {
            get
            {
                if (!IsLocked && File.Exists(filepath + ".lock"))
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Serialize the data to file path
        /// </summary>
        /// <param name="path"></param>
        public void Save(bool unlock = true)
        {
            if (!_readonly)
            {
                using (var sw = new StreamWriter(File.Open(filepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
                    sw.Flush();
                    sw.Close();
                }
                if (unlock) UnLock();
            }
        }

        ~JSONDB()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (lockfile != null)
                lockfile.Close();
            if (IsLocked && File.Exists(filepath + ".lock"))
                File.Delete(filepath + ".lock");
        }

        [Serializable]
        public class JSONDBTimeOutException : JSONDBException
        {
            public JSONDBTimeOutException()
            {
            }

            public JSONDBTimeOutException(string message) : base(message)
            {
            }

            public JSONDBTimeOutException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected JSONDBTimeOutException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        [Serializable]
        public class JSONDBException : Exception
        {
            public JSONDBException()
            {
            }

            public JSONDBException(string message) : base(message)
            {
            }

            public JSONDBException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected JSONDBException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
