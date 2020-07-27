namespace WebScraping.Models
{
    public class FileRepository
    {
        public string FileName { get; set; }
        public string UrlBlob { get; set; }
        public int CountLines { get; set; }
        public double ValueSize { get; set; }
        public string Type { get; set; }
    }
}
