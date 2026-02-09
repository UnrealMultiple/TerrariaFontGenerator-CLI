using System;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using System.IO;
using System.Reflection;
using ReLogic.Content.Pipeline;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using StbImageWriteSharp;
using Rectangle = System.Drawing.Rectangle;

namespace TerrariaFontGenCLI
{
    public sealed class Generator : Game
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly GraphicsDeviceManager _graphics;
         
        private static void Main()
        {
            using var game = new Generator();
            game.Run();
        }
        

        public Generator()
        {
            ReLogicPipeLineAssembly = typeof(DynamicFontDescription).Assembly;
            _graphics = new GraphicsDeviceManager(this);
            _context = new DfgContext(this);
            _importContext = new DfgImporterContext();
            _importer = (ContentImporter<DynamicFontDescription>)Activator.CreateInstance(ReLogicPipeLineAssembly.GetType("ReLogic.Content.Pipeline.DynamicFontImporter"));
            _processor = new DynamicFontProcessor();
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
            CompileFonts();
        }

        private const string SaveDir = "output"; 

        private void CompileFonts()
        {
            Directory.CreateDirectory(SaveDir);
            
            var descFiles = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.xml").ToList();

            Console.WriteLine("Total font files detected: {0}", descFiles.Count);

            foreach (var descFilePath in descFiles)
            {
                var descFileName = Path.GetFileName(descFilePath);

                Console.WriteLine("* {0}", descFileName);
            }

            Console.WriteLine();

            foreach (var descFilePath in descFiles)
            {
                var descFileName = Path.GetFileName(descFilePath);

                Console.Write("Start loading sample font file file: {0}", descFileName);

                var description = _importer.Import(descFilePath, _importContext);
                Console.WriteLine(" ..Done!");

                var fileName = Path.GetFileNameWithoutExtension(descFileName) + ".txt";

                Console.Write("Start compiling font.");
                var content = _processor.Process(description, _context);
                Console.WriteLine(".Done!");
                
                Console.Write("Start compiling font content file: {0}", fileName);
                
                using (var fs = new FileStream(Path.Combine(SaveDir, fileName), FileMode.Create))
                using (var bw = new BinaryWriter(fs))
                {
                   Serialize(bw);
                   bw.Flush();
                }

                var index = 1;
                foreach (var page in content._pages)
                {
                    SaveTexture2DContentToPng(page.Texture, $"{Path.GetFileNameWithoutExtension(descFileName)}_{index}_A");
                    index++;
                    //InspectTextureContent(page.Texture);
                }

                Console.WriteLine(" ..Done!");
                Console.WriteLine();
                continue;

                void Serialize(BinaryWriter writer)
                {
                    writer.Write(content._spacing);
                    writer.Write(content._pages.Max(page => page.LineSpacing));
                    writer.Write(content._defaultCharacter);
                    writer.Write(content._pages.Count);
                    foreach (var page in content._pages)
                    {
                        writer.Write(page.Glyphs);
                        writer.Write(page.Padding);
                        writer.Write(page.Characters);
                        writer.Write(page.Kerning);
                    }
                }
            }
        }
        
        public static void SaveTexture2DContentToPng(Texture2DContent content, string fileName)
        {
            // 1. 核心步骤：将 DXT3 转换为 RGBA 格式
            // 这一步会调用你反编译代码中的 ConvertBitmapType，内部会进行解压
            Console.WriteLine($"Converting {content.Mipmaps[0].GetType().Name} to RGBA...");
            content.ConvertBitmapType(typeof(PixelBitmapContent<Microsoft.Xna.Framework.Color>));

            BitmapContent bitmapContent = content.Mipmaps[0];
            int width = bitmapContent.Width;
            int height = bitmapContent.Height;
    
            // 此时 rawData 长度应该是 width * height * 4
            var rawData = bitmapContent.GetPixelData();

            // 2. 创建 System.Drawing.Bitmap
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat
                );

                byte[] processedData = new byte[rawData.Length];
                try
                {
                    // 3. 通道转换 (XNA RGBA -> GDI+ BGRA)
                    // DXT 解压后的数据顺序是 R, G, B, A，而 Bitmap 内存需要 B, G, R, A
                    for (var i = 0; i < rawData.Length; i += 4)
                    {
                        processedData[i]     = rawData[i + 2]; // Blue
                        processedData[i + 1] = rawData[i + 1]; // Green
                        processedData[i + 2] = rawData[i];     // Red
                        processedData[i + 3] = rawData[i + 3]; // Alpha
                    }

                    Marshal.Copy(processedData, 0, bmpData.Scan0, processedData.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
                

                bitmap.Save(Path.Combine(SaveDir, $"{fileName}.png"), ImageFormat.Png);
                Console.WriteLine($"Saved: {fileName}.png ({width}x{height})");
            }
        }
        
        public static void InspectTextureContent(Texture2DContent content)
        {
            Console.WriteLine("--- Texture2DContent Inspection ---");
            Console.WriteLine($"Name: {content.Name}");
    
            if (content.Mipmaps == null || content.Mipmaps.Count == 0)
            {
                Console.WriteLine("Warning: No Mipmaps found.");
                return;
            }

            // 访问最顶层的 Mipmap (Level 0)
            BitmapContent bitmapContent = content.Mipmaps[0];
    
            // 1. 打印具体的类名 (例如: Microsoft.Xna.Framework.Content.Pipeline.Graphics.PixelBitmapContent`1[Microsoft.Xna.Framework.Color])
            Console.WriteLine($"Internal Type: {bitmapContent.GetType().FullName}");
    
            // 2. 打印尺寸
            Console.WriteLine($"Dimensions: {bitmapContent.Width}x{bitmapContent.Height}");

            // 3. 获取并打印原始数据信息
            var rawData = bitmapContent.GetPixelData();
            Console.WriteLine($"Raw Data Length: {rawData.Length} bytes");

            // 4. 计算预期的每像素字节数
            if (bitmapContent.Width > 0 && bitmapContent.Height > 0)
            {
                float bytesPerPixel = (float)rawData.Length / (bitmapContent.Width * bitmapContent.Height);
                Console.WriteLine($"Bytes Per Pixel (BPP): {bytesPerPixel}");
            }
    
            Console.WriteLine("-----------------------------------\n");
        }
        

        private readonly DfgContext _context;

        private readonly DfgImporterContext _importContext;

        private readonly ContentImporter<DynamicFontDescription> _importer;

        private readonly DynamicFontProcessor _processor;

        public readonly Assembly ReLogicPipeLineAssembly;
    }
}
