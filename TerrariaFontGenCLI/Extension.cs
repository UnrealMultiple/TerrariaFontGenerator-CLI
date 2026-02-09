using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace TerrariaFontGenCLI
{
    public static class Extension
    {

            public static void Write(this BinaryWriter bw, Rectangle rect)
            {
                bw.Write(rect.X);
                bw.Write(rect.Y);
                bw.Write(rect.Width);
                bw.Write(rect.Height);
            }
        
            public static void Write(this BinaryWriter bw, Vector3 vec)
            {
                bw.Write(vec.X);
                bw.Write(vec.Y);
                bw.Write(vec.Z);
            }
            
            public static void Write(this BinaryWriter bw, List<Rectangle> value)
            {
                bw.Write(value.Count);
                foreach (var item in value)
                {
                    bw.Write(item);
                }
            }
            
            public static void Write(this BinaryWriter bw, List<Vector3> value)
            {
                bw.Write(value.Count);
                foreach (var item in value)
                {
                    bw.Write(item);
                }
            }
            
            public static void Write(this BinaryWriter bw, List<char> value)
            {
                bw.Write(value.Count);
                foreach (var item in value)
                {
                    bw.Write(item);
                }
            }
    }
}