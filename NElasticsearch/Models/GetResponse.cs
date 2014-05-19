namespace NElasticsearch.Models
{
    public class GetResponse<T> : Hit<T>
    {
        public int _version { get; set; }
        public bool found { get; set; }
    }
}
