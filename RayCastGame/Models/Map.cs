using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RayCastGame.Models
{
    public class Map
    {
        public string Name { get; set; } = $"newMap{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int[,] World { get; private set; }
        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            World = new int[Height, Width];

            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    if (i == 0 || i == Height - 1 || j == 0 || j == Width - 1 || new Random().Next(10) < 1)
                    {
                        World[i, j] = 1;
                        continue;
                    }
                    World[i, j] = 0;
                }
            }

            //if (spawns.Any(s => World[(int)s.X, (int)s.Y] != 0))
            //    throw new Exception("A spawnpoint was placed in an impossible place.");
        }
        public Map(string fileName)
        {
            var json = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(fileName));
            Name = json["Name"];
            Width = json["Width"];
            Height = json["Height"];
            World = json.World.ToObject<int[,]>();
        }

        public void Save()
        {
            Directory.CreateDirectory("Levels");

            var amount = Directory.GetFiles("Levels", $"{Name}*").Length;

            var json = JsonConvert.SerializeObject(this);
            File.WriteAllText($"Levels/{Name + (amount > 0 ? amount.ToString() : string.Empty)}.json", json);
        }

        public static void Delete(string mapName)
        {
            File.Delete(@"Levels\" + mapName);
        }
        public static List<Map> GetMaps()
        {
            List<Map> output = new List<Map>();
            var files = Directory.GetFiles("Levels");
            foreach (var item in files)
            {
                output.Add(new Map(item));
            }
            return output;
        }
    }
}
