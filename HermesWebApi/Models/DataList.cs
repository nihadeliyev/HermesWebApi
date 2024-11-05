namespace HermesWebApi.Models
{
    public class DataList<T>
    {
        public int RowCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get { return (int)Math.Ceiling(RowCount / (double)PageSize); }  }
        public List<T> Data { get; set; }
    }
}
