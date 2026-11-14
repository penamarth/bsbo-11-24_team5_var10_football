namespace FootballSystem.Models;

using FootballSystem.Enums;

using System.Collections.Generic;
using System.Linq;

public class Event
{
		public int ID;
		public string Name;
		public List<Match> Matches = new();
		public int StartUnixTimestamp;
		public int EndUnixTimestamp;

		public Event(int id, string name, List<Match> matches, int startUnixTimestamp, int endUnixTimestamp)
		{
				ID = id;
				Name = name;
				Matches = matches;
				StartUnixTimestamp = startUnixTimestamp;
				EndUnixTimestamp = endUnixTimestamp;
		}

		public Match? FindMatchByID(int matchId)
		{
				return Matches.FirstOrDefault(m => m.ID == matchId);
		}

		public void AddMatch(Match match)
		{
				Matches.Add(match);
		}

		public bool SetSeatStatus(int matchId, int seatId, SeatStatus status)
		{
				Match? match = FindMatchByID(matchId);
				if (match == null) {
						return false;
				}
				return match.SetSeatStatus(seatId, status);
		}
}
