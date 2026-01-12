namespace ForbiddenOrgChecker.Models
{
    public class Violation
    {
        public string FileName { get; set; } = "";
        public int GlobalBlockNumber { get; set; }
        public List<string> Participants { get; set; } = new();
    }
}