using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DornMovieApp
{
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

        /// <summary>
        /// Instantiate and give the file path for Load and Save of the Intermediate Storage data
        /// </summary>
        /// <param name="path"></param>
        public JSONDB(string path)
        {
            filepath = path;
            if (IsLockedOutside)
                throw new Exception("Database error. Database is currently locked by outside application.");
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

            throw new Exception("Cannot GetSection using an Null or Whitespace string.");
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
            throw new Exception("Cannot GetSection using an Null or Whitespace string.");
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
                throw new Exception("Cannot SaveSection using an Null or Whitespace string.");
        }

        /// <summary>
        /// Saves a custom section back (not saved to disk)
        /// </summary>
        /// <param name="table"></param>
        public void SaveSection(string name, object obj)
        {
            if (!string.IsNullOrWhiteSpace(name))
                data[name] = JsonConvert.SerializeObject(obj);
            else
                throw new Exception("Cannot SaveSection using an Null or Whitespace string.");
        }

        private bool _readonly;
        /// <summary>
        /// Load from json string in file
        /// </summary>
        /// <param name="path"></param>
        public bool Load(bool read_only = false)
        {
            if (read_only || TryLock())
            {
                _readonly = read_only;
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
        private bool TryLock()
        {
            try
            {
                mutex.WaitOne();
                lockfile = new FileStream(filepath + ".lock", FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
                IsLocked = true;
            }
            catch (Exception ex)
            {
                IsLocked = false;
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
            if (lockfile != null)
                lockfile.Close();
            if (File.Exists(filepath + ".lock"))
                File.Delete(filepath + ".lock");
        }

        public void Dispose()
        {
            lockfile.Close();
            if (File.Exists(filepath + ".lock"))
                File.Delete(filepath + ".lock");
        }

    }
}
