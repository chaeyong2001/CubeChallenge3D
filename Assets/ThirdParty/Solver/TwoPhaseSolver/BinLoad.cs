using System;
using System.IO;
using UnityEngine;

namespace TwoPhaseSolver
{
    static class BinLoad
    {
        public static byte[][] getUdToPerm(string path)
        {
            byte[] bytes = LoadBytes(path);
            byte[][] values = new byte[Constants.N_UD][];

            for (var i = 0; i < Constants.N_UD; i++)
            {
                int offset = i * 2;
                values[i] = new byte[4]
                {
                    (byte)((bytes[offset] & 0xf0) >> 4),
                    (byte)(bytes[offset] & 0x0f),
                    (byte)((bytes[offset + 1] & 0xf0) >> 4),
                    (byte)(bytes[offset + 1] & 0x0f)
                };
            }

            return values;
        }

        public static ushort[,] loadShortTable2D(string path, int chunksize = 18)
        {
            var bytes = LoadBytes(path);
            int len1d = bytes.Length / chunksize / 2;
            ushort[,] values = new ushort[len1d, chunksize];
            int i, j;
            
            for (i = 0; i < len1d; i++)
            {
                for (j = 0; j < chunksize; j++)
                {
                    values[i, j] = (ushort)(
                        (bytes[(chunksize * i + j) * 2] << 8) + 
                        bytes[(chunksize * i + j) * 2 + 1]
                    );
                }
            }

            return values;
        }

        public static PruneTable loadPruneTable(string path)
        {
            return new PruneTable(LoadBytes(path));
        }

        private static byte[] LoadBytes(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith("tables/"))
            {
                normalized = normalized.Substring("tables/".Length);
            }

            TextAsset asset = Resources.Load<TextAsset>("TwoPhaseSolverTables/" + normalized);
            if (asset == null)
            {
                throw new FileNotFoundException("TwoPhaseSolver table resource was not found.", normalized);
            }

            byte[] bytes = asset.bytes;
            Resources.UnloadAsset(asset);
            return bytes;
        }
    }
}
