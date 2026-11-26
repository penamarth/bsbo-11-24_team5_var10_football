namespace FootballSystem.Events;

public interface IPrototype<T>
{
    T clone();
}
