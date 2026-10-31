namespace FootballSystem.Storages;

using FootballSystem.Models;
using FootballSystem.Enums;

using System.Collections.Generic;
using System.Linq;

public class EventStorage
{
		private List<Event> _events;

		public List<Event> GetEvents()
		{
				return _events;
		}

		public Event? FindEventByID(int id)
		{
				return _events.FirstOrDefault(e => e.ID == id);
		}

		public Match? FindMatchByID(int id)
		{
			return _events
				.Select(e => e.FindMatchByID(id))
				.FirstOrDefault(match => match != null);
		}

		public void CreateEvent(Event e)
		{
				e.ID = _events.Count;
				_events.Add(e);
		}

		public bool CreateMatch(int eventId, Match match)
		{
				Event? e = FindEventByID(eventId);
				if (e == null)
				{
						return false;
				}
				match.ID = _events
						.SelectMany(e => e.Matches)
						.Max(m => m.ID);
				e.AddMatch(match);
				return true;
		}

		public bool ReserveSeat(int matchId, int seatId)
		{
				Match? match = FindMatchByID(matchId);
				if (match == null)
				{
						return false;
				}

				return match.SetSeatStatus(seatId, SeatStatus.Booked);
		}

		public bool UpdateEvent(int id, Event newEvent)
		{
			Event? e = FindEventByID(id);
			if (e == null)
			{
				return false;
			}

			e.ID = id;
			e.Name = newEvent.Name;
			e.Matches = newEvent.Matches;
			e.StartUnixTimestamp = newEvent.StartUnixTimestamp;
			e.EndUnixTimestamp = newEvent.EndUnixTimestamp;

			return true;
		}

		public bool UpdateMatch(int id, Match newMatch)
		{
			Match? match = FindMatchByID(id);
			if (match == null)
			{
				return false;
			}

			match.ID = id;
			match.Name = newMatch.Name;
			match.PriceCoefficient = newMatch.PriceCoefficient;
			match.StadiumMap = newMatch.StadiumMap;
			match.StartUnixTimestamp = newMatch.StartUnixTimestamp;
			match.EndUnixTimestamp = newMatch.EndUnixTimestamp;
			match.SeatStatuses = newMatch.SeatStatuses;

			return true;
		}
}
