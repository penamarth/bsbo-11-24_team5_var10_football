using System;
namespace FootballSystem.Events;

public class Event {
    int id;
    public string name;
    List<Match> matches;
    public Tuple<int,int> eventTime;

    public Event(int id, string name, Tuple<int, int> eventTime) {
        this.name = name;
        this.eventTime = eventTime;
        this.matches = new List<Match>{};
    }

    public Match findMatchById(int matchId) {
        foreach(Match match in matches) {
            if (match.getId() == matchId)
                return match;
        }
        return null;
    }
    public void addMatch(Match match) {
        this.matches.Add(match);
    }
    public int getId() {
        return this.id;
    }
    public int lastId() {
    int last = 1;
    foreach (Match m in matches) {
        last = m.getId();
    }
    return last;
}

    public void setId(int id) {
        this.id = id;
        return;
    }
}