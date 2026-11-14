namespace FootballSystem.Models;

public class Seat
{
		public int ID;
		public int Sector;
		public int Row;
		public int Column;
		public double Price;

		public Seat(int id, int sector, int row, int column, double price) {
				ID = id;
				Sector = sector;
				Row = row;
				Column = column;
				Price = price;
		}
}
