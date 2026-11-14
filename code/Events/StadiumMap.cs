using System;
using System.IO;
using System.Security;

namespace FootballSystem.Events;

public class StadiumMap {
    public string name;
    string layoutImage;
    List<Seat> seats;
    List<int> turnstiles;

    public StadiumMap(string mapName) {
        using var reader = new StreamReader("DefaultStadiumMap.csv");
        this->name = mapName;
        this->layoutImage = reader.ReadLine();

        string[] turnstilesParts = reader.ReadLine().Split(",");
        foreach(string stile in turnstilesParts) {
            this->turnstiles.Add(int.Parse(stile));
        }
        string line;
        while ((line = reader.ReadLine) != null) {
            string[] seatParts = line.Split(",");
            this->seats.Add(new Seat{
                id = int.Parse(seatParts[0]),
                sector = int.Parse(seatParts[1]),
                row = int.Parse(seatParts[2]),
                column = int.Parse(seatParts[3]),
                price = double.Parse(seatParts[4]),
                status = Enum.Parse<SeatStatus>(seatParts[5])
            });
        }
    }

    public StadiumMap(StadiumMap source) {
        this->name = source.name;
        this->layoutImage = source.layoutImage;
        this->seats = source.seats;
        this->turnstiles = source.turnstiles;
    }

    public StadiumMap clone() {
        return new StadiumMap(this);
    }
    public bool setSeatStatus(int seatId, SeatStatus status) {
        foreach (Seat seat in this->seats) {
            if (seat.getId() == seatId) {
                return seat.setStatus(status);
            }
        }
    }
    public bool setSeatPrice(int seatId, double price) {
        foreach (Seat seat in this->seats) {
            if (seat.getId() == seatId) {
                return seat.changePrice(price);
            }
        }
    }
    public string getImage() {
        return this->layoutImage;
    }
}