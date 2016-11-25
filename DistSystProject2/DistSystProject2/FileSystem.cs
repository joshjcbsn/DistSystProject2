using System;
using System.Collections.Generic;
using System.Web;

namespace Server
{
    public class FileSystem
    {
        public Dictionary<string, string> files;

        public FileSystem()
        {
            files = new Dictionary<string, string>();
        }

        public void AddFile(string filename)
        {
            files.Add(filename,"");
        }

        public void DeleteFile(string filename)
        {
            if (ContainsFile(filename))
            {
                files.Remove(filename);
            }
        }

        public void AppendFile(string filename, string text)
        {
            if (ContainsFile(filename))
            {
                files[filename] += text;
            }
        }

        public string ReadFile(string filename)
        {
            if (ContainsFile(filename))
            {
                return files[filename];
            }
            else
            {
                return "File does not exist";
            }
        }
        public bool ContainsFile(string filename)
        {
            return files.ContainsKey(filename);
        }
    }
}