namespace FootballSystem.Models;

using FootballSystem.Enums;

using System.Collections.Generic;
using System.Linq;

public class StadiumMap
{
		public string Name;
		public string LayoutImage;
		public List<Seat> Seats = new();
		public List<int> Turnstiles = new();
}
