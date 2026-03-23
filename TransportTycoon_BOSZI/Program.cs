using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;

namespace TransportTycoon_BOSZI;

class Program
{
    static void Main(string[] args)
    {
        Map myMap = new Map(50, 10);
        MapObject[,] realmap = myMap.createMap();
        for (int i = 0;i<50; ++i)
        {
            for (int j = 0; j < 50; ++j)
            {
                if (realmap[i, j] is House)
                {
                    Console.Write("H");
                }
                else
                {
                    Console.Write("E");
                }
            }
            Console.WriteLine();
        }
        Console.WriteLine("done:)");
    }
}