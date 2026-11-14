using System;
using System.Numerics;
namespace FootballSystem.Events;

public class Match {
    int id;
    public string name;
    public string description;
    double priceCoefficient;
    StadiumMap stadiumMap;
    public Tuple<int,int> matchTime;

    public bool setSeatStatus(int seatId, SeatStatus status) {
        return this.stadiumMap.setSeatStatus(seatId, status);
    }
    public bool setSeatPrice(int seatId, double price) {
        return this.stadiumMap.setSeatPrice(seatId, price);
    }
    public StadiumMap getMap() {
        return this.stadiumMap;
    }
    public int getId() {
        return this.id;
    }

}