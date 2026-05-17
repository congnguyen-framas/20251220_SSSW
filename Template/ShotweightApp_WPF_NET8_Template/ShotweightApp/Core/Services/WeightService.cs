
namespace ShotweightApp.Core.Services
{
    public class WeightService
    {
        public bool IsOverWeight(double actual, double std)
        {
            return actual > std;
        }
    }
}
