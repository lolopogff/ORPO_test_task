namespace ForbiddenOrgChecker.Models
{
    public class AlgorithmResult
    {
        public string Name { get; set; } = "";
        public long ExecutionTimeMs { get; set; }
        public int TotalBlocks { get; set; }
        public int BadBlocksFound { get; set; }
        public int AlgorithmNumber { get; set; }
        public List<Violation> Violations { get; set; } = new();

        public double BlocksPerSecond => TotalBlocks > 0 ? TotalBlocks / (ExecutionTimeMs / 1000.0) : 0;
        public double TimePerBlock => TotalBlocks > 0 ? ExecutionTimeMs / (double)TotalBlocks : 0;
    }
}