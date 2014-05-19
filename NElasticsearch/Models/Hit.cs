using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NElasticsearch.Models
{
    /// <summary>
    /// Individual hit response from ElasticSearch.
    /// </summary>
    [DebuggerDisplay("{_type} in {_index} id {_id}")]
    public class Hit<T>
    {
        public string _index { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public double? _score { get; set; }

        public T _source { get; set; }
        //public Dictionary<String, JToken> fields = new Dictionary<string, JToken>();
    }
}
