using System;
using LibGit2Sharp;

namespace libgit2sharp.Elasticsearch.Models
{
    public class GitObject
    {
        public void SetData(byte[] data)
        {
            Data = Convert.ToBase64String(data);
        }

        public byte[] GetDataAsByteArray()
        {
            return Convert.FromBase64String(Data);
        }

        /// <summary>
        /// Data as Base64 string
        /// </summary>
        public string Data { get; set; }
        public string Sha { get; set; }
        public long Size { get; set; }
        public ObjectType Type { get; set; }
    }
}
