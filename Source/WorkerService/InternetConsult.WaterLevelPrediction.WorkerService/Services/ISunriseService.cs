using System.Threading.Tasks;

namespace InternetConsult.WaterLevelPrediction.WorkerService.Services
{
    public interface ISunriseService
	{
		public Task<Sunrise> GetSunriseResult();
	}
}
