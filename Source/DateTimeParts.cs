using System;

namespace PlanningNode {

	/// <summary>
	/// An object that holds the parts of a time span,
	/// broken down into years, days, hours, minutes, and seconds.
	/// </summary>
	public class DateTimeParts {
		private const double
			minutesPerHour   =  60,
			secondsPerMinute =  60,
			hoursPerDayEarth =  24,
			daysPerYearEarth = 365;

		private static readonly double
			hoursPerDay = solarDayLength(FlightGlobals.GetHomeBody()) / secondsPerMinute / minutesPerHour,
			daysPerYear = FlightGlobals.GetHomeBody().GetOrbit().period / secondsPerMinute / minutesPerHour / hoursPerDay;

		/// <summary>
		/// https://en.wikipedia.org/wiki/Sidereal_time#Sidereal_days_compared_to_solar_days_on_other_planets
		/// </summary>
		private static double solarDayLength(CelestialBody b)
		{
			if (b.rotationPeriod == b.GetOrbit().period) {
				// Tidally locked, don't divide by zero
				return 0;
			} else {
				return b.rotationPeriod / (1 - (b.rotationPeriod / b.GetOrbit().period));
			}
		}

		private static int mod(double numerator, double denominator)
		{
			return (int)Math.Floor(numerator % denominator);
		}

		/// <summary>
		/// Construct an object for the given timestamp.
		/// </summary>
		/// <param name="UT">Seconds since game start</param>
		public DateTimeParts(double UT)
		{
			if (UT == double.PositiveInfinity) {
				Infinite = true;
			} else if (UT == double.NaN) {
				Invalid = true;
			} else {
				totalSeconds = UT;
				seconds = mod(UT, secondsPerMinute);
				UT /= secondsPerMinute;
				totalMinutes = (int)Math.Floor(UT);
				minutes = mod(UT, minutesPerHour);
				UT /= minutesPerHour;
				if (GameSettings.KERBIN_TIME) {
					hours = mod(UT, hoursPerDay);
					UT /= hoursPerDay;
					days = mod(UT, daysPerYear);
					UT /= daysPerYear;
				} else {
					hours = mod(UT, hoursPerDayEarth);
					UT /= hoursPerDayEarth;
					days = mod(UT, daysPerYearEarth);
					UT /= daysPerYearEarth;
				}
				years = (int)Math.Floor(UT);
			}
		}

		/// <summary>
		/// Whether the time represented is infinite,
		/// e.g. for burn duration when we don't have enough delta V
		/// </summary>
		public bool Infinite { get; private set; }

		/// <summary>
		/// Whether the time represented is invalid,
		/// e.g. for burn duration without a vessel
		/// </summary>
		public bool Invalid  { get; private set; }

		/// <summary>
		/// The year component of the given time.
		/// </summary>
		public int years   { get; private set; }

		/// <summary>
		/// The day component of the given time.
		/// </summary>
		public int days    { get; private set; }

		/// <summary>
		/// The hour component of the given time.
		/// </summary>
		public int hours   { get; private set; }

		/// <summary>
		/// The minute component of the given time.
		/// </summary>
		public int minutes { get; private set; }

		/// <summary>
		/// Total time in minutes (includes hours, days, etc.)
		/// </summary>
		public int totalMinutes { get; private set; }

		/// <summary>
		/// Seconds including fraction
		/// </summary>
		public double totalSeconds { get; private set; }

		/// <summary>
		/// The second component of the given time.
		/// </summary>
		public int seconds { get; private set; }

		/// <returns>
		/// True if years must be displayed to correctly represent this time.
		/// </returns>
		public bool needYears   { get { return years   > 0; } }

		/// <returns>
		/// True if days must be displayed to correctly represent this time.
		/// </returns>
		public bool needDays    { get { return days    > 0 || needYears; } }

		/// <returns>
		/// True if hours must be displayed to accurately represent this time.
		/// </returns>
		public bool needHours   { get { return hours   > 0 || needDays; } }

		/// <returns>
		/// True if minutes must be displayed to accurately represent this time.
		/// </returns>
		public bool needMinutes { get { return minutes > 0 || needHours; } }

		// (Seconds must always be displayed.)
	}

}
