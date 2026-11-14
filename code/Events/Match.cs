namespace FootballSystem.Models;

using FootballSystem.Enums;

using System.Collections.Generic;

public class Match
{
		public int ID;
		public string Name;
		public double PriceCoefficient = 1.0;
		public StadiumMap StadiumMap;
		public int StartUnixTimestamp;
		public int EndUnixTimestamp;
		public Dictionary<int, SeatStatus> SeatStatuses = new();
		
		public Match(int id, string name, double priceCoefficient, StadiumMap map, int start, int end)
		{
				ID = id;
				Name = name;
				PriceCoefficient = priceCoefficient;
				StadiumMap = map;
				StartUnixTimestamp = start;
				EndUnixTimestamp = end;
		}

		public bool SetSeatStatus(int seatId, SeatStatus status)
		{
				if (SeatStatuses.TryGetValue(seatId, out SeatStatus current) && current == status)
				{
						return false;
				}

				SeatStatuses[seatId] = status;
				return true;
		}

		public SeatStatus GetSeatStatus(int seatId)
		{
				return SeatStatuses.GetValueOrDefault(seatId, SeatStatus.Free);
		}
}
