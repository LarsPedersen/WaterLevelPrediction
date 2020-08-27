using System;
using System.Collections.Generic;

namespace InternetConsult.WaterLevelPrediction.Model
{
    public class PredictionResults
	{
		public string Id { get; set; }
		public string Project { get; set; }
		public string Iteration { get; set; }
		public DateTime Created { get; set; }
		public IList<Prediction> Predictions { get; set; }
	}
}
