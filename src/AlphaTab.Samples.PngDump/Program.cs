using System;
using System.Collections.Generic;
using System.IO;
using AlphaSkia;
using AlphaTab.Importer;
using AlphaTab.Rendering;

namespace AlphaTab.Samples.PngDump;

public static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage AlphaTab.Samples.PngDump.exe Path");
            return;
        }

        // load score
        var score = ScoreLoader.LoadScoreFromBytes(File.ReadAllBytes(args[0]));

        // render score with svg engine and desired rendering width
        var settings = new Settings
        {
            Core =
            {
                Engine = "skia"
            }
        };
        var renderer = new ScoreRenderer(settings)
        {
            Width = 970
        };

        // iterate tracks
        for (var i = 0; i < score.Tracks.Count; i++)
        {
            var track = score.Tracks[i];

            // render track
            Console.WriteLine("Rendering track {0} - {1}", i + 1, track.Name);
            var images = new List<RenderFinishedEventArgs>();
            var totalWidth = 0;
            var totalHeight = 0;
            renderer.PartialRenderFinished.On(r => { images.Add(r); });
            renderer.RenderFinished.On(r =>
            {
                totalWidth = (int)r.TotalWidth;
                totalHeight = (int)r.TotalHeight;
            });
            renderer.RenderScore(score, new List<double> { track.Index });

            // write png
            var info = new FileInfo(args[0]);
            var path = Path.Combine(info.DirectoryName,
                Path.GetFileNameWithoutExtension(info.Name) + "-" + i + ".png");

            using var full = new AlphaSkiaCanvas();
            full.BeginRender(totalWidth, totalHeight);
            
            foreach (var image in images)
            {
                full.DrawImage(((AlphaTab.Platform.Skia.AlphaSkiaBridge.AlphaSkiaImage)image.RenderResult!).Image, 
                    (float)image.X,
                    (float)image.Y,
                    (float)image.Width,
                    (float)image.Height);
            }

            using var fullImage = full.EndRender()!;
            var data = fullImage.ToPng()!;
            using var fileStream =
                new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(data);
        }
    }
}