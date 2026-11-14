using System;
namespace FootballSystem.Events;

public class Event {
    int id;
    public string name;
    List<Match> matches;
    public Tuple<int,int> eventTime;

    public Event(int id, string name, Tuple<int, int> eventTime) {
        this->name = name;
        this->eventTime = eventTime;
        this->matches = new List<Match>{};
    }

    public Match findMatchById(int matchId) {
        foreach(Match match in matches) {
            if (match.getId() == matchId)
                return match;
        }
    }
    public bool addMatch(Match match) {
        return this->matches.Add(match);
    }
    public int getId() {
        return this->id;
    }
    public int lastId() {
    int last;
    foreach (Match m in matches) {
        last = m.getId();
    }
    return last;
}

    public void setId(int id) {
        this->id = id;
        return;
    }
}