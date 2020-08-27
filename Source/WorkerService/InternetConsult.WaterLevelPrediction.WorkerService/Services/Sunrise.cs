using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace InternetConsult.WaterLevelPrediction.WorkerService
{
	public partial class Sunrise
	{
		[JsonProperty("results")]
		public Results Results { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
	}

	public partial class Results
	{
		[JsonProperty("sunrise")]
		public DateTimeOffset Sunrise { get; set; }

		[JsonProperty("sunset")]
		public DateTimeOffset Sunset { get; set; }

		[JsonProperty("solar_noon")]
		public DateTimeOffset SolarNoon { get; set; }

		[JsonProperty("day_length")]
		public long DayLength { get; set; }

		[JsonProperty("civil_twilight_begin")]
		public DateTimeOffset CivilTwilightBegin { get; set; }

		[JsonProperty("civil_twilight_end")]
		public DateTimeOffset CivilTwilightEnd { get; set; }

		[JsonProperty("nautical_twilight_begin")]
		public DateTimeOffset NauticalTwilightBegin { get; set; }

		[JsonProperty("nautical_twilight_end")]
		public DateTimeOffset NauticalTwilightEnd { get; set; }

		[JsonProperty("astronomical_twilight_begin")]
		public DateTimeOffset AstronomicalTwilightBegin { get; set; }

		[JsonProperty("astronomical_twilight_end")]
		public DateTimeOffset AstronomicalTwilightEnd { get; set; }
	}
}
