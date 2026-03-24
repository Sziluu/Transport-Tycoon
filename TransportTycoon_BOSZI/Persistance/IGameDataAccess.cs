namespace TransportTycoon_BOSZI.Persistance;

public interface IGameDataAccess
{
    Task SaveGameAsync(string filePath);
    Task LoadGameAsync(string filePath);
    string[] GetSaveFiles(string directory);
}