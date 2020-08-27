using System;

namespace InternetConsult.WaterLevelPrediction.Model
{
	public class Prediction
	{
		public double Probability { get; set; }
		public string TagId { get; set; }
		public string TagName { get; set; }
	}
}
