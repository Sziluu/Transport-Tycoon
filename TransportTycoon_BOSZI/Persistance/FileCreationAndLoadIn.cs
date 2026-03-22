using TransportTycoon_BOSZI.Model;

namespace TransportTycoon_BOSZI.Persistance;

public class FileCreationAndLoadIn(Map map, string filePath)
{
    public void SaveMap(string filePath)
    {
        using (var stream = File.Open(filePath, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write("TTMAP"u8);
            writer.Write((ushort)1);

            writer.Write(map.Size);
            writer.Write(map.Money);

            for (int i = 0; i < map.Size; i++)
            for (int j = 0; j < map.Size; j++)
                writer.Write((byte)map.GetTileType(map.GetMap[i, j]));
        }
    }
    public void LoadMap(string filePath)
    {
        using (var stream = File.Open(filePath, FileMode.Open))
        using (var reader = new BinaryReader(stream))
        {
            var sig = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(5));
            if (sig != "TTMAP") throw new InvalidDataException("Invalid map file.");
            ushort version = reader.ReadUInt16();

            int size  = reader.ReadInt32();
            int money = reader.ReadInt32();

            map = new Map(size, money);
            map.EmptyTiles.Clear();

            for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                map.GetMap[i, j] = map.TileTypeToObject((TileType)reader.ReadByte());
        }
    }
}