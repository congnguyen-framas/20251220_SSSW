
namespace ShotweightEnterprise1000Users.Domain.Entities
{
    public class Step
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
        public string Machine { get; set; } = "";
        public string Item { get; set; } = "";
        public double Std { get; set; }
        public double Actual { get; set; }
    }
}
