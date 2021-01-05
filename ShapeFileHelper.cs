using System;
using System.Drawing;
using System.IO;
using SharpMap;
using SharpMap.Data.Providers;
using SharpMap.Layers;


namespace WebGISSocket
{
    class ShapeFileHelper
    {
        private static bool initialized = false;
        private static Map map;
        public static MemoryStream ReadShapeFile(int width, int heigth, double x1, double x2, double y1, double y2)
        {
            if (initialized == false)
            {
                map = new Map();
                string path = CONSTANT.ROOT;

                // polygons not in PKU
                VectorLayer notin = new VectorLayer("notin");
                notin.DataSource = new ShapeFile(path + "shp\\pku\\polygon_not_in_pku.shp", true);
                notin.Style.Fill = new SolidBrush(Color.FromArgb(0xcc, 0xcc, 0xcc));
                notin.Style.Outline = new Pen(Color.FromArgb(0xbf, 0xbf, 0xbf));
                notin.Style.EnableOutline = true;

                // Background
                VectorLayer Backlay = new VectorLayer("Background");
                Backlay.DataSource = new ShapeFile(path + "shp\\pku\\Background.shp", true);
                Backlay.Style.Fill = new SolidBrush(Color.FromArgb(0xcc, 0xcc, 0xcc));
                Backlay.Style.Outline = new Pen(Color.FromArgb(0xbf, 0xbf, 0xbf));
                Backlay.Style.EnableOutline = true;

                // grass
                VectorLayer grasslay = new VectorLayer("grass");
                grasslay.DataSource = new ShapeFile(path + "shp\\pku\\grass.shp", true);
                grasslay.Style.Fill = new SolidBrush(Color.FromArgb(0xA6, 0xC4, 0x90));

                // water
                VectorLayer water = new VectorLayer("water");
                water.DataSource = new ShapeFile(path + "shp\\pku\\water.shp", true);
                water.Style.Fill = new SolidBrush(Color.FromArgb(0x97, 0xDB, 0xF2));
                water.Style.Outline = new Pen(Color.FromArgb(0x90, 0xD5, 0xEE));
                water.Style.EnableOutline = true;

                // buildings in PKU
                VectorLayer vlay = new VectorLayer("buildings_in_pku");
                vlay.DataSource = new ShapeFile(path + "shp\\pku\\buildings_in_pku.shp", true);
                vlay.Style.Fill = new SolidBrush(Color.FromArgb(0x72, 0x77, 0x7B));
                vlay.Style.Outline = new Pen(Color.FromArgb(0x5f, 0x64, 0x69));
                vlay.Style.EnableOutline = true;

                // footway
                VectorLayer foot = new VectorLayer("foot");
                foot.DataSource = new ShapeFile(path + "shp\\pku\\footway.shp", true);
                foot.Style.Line = new Pen(Color.Black, 0.5f);

                // secondary road
                VectorLayer sec = new VectorLayer("secondary");
                sec.DataSource = new ShapeFile(path + "shp\\pku\\secondary_road.shp", true);
                sec.Style.Line = new Pen(Color.Black, 1);

                // primary road
                VectorLayer pri = new VectorLayer("primery");
                pri.DataSource = new ShapeFile(path + "shp\\pku\\primary_road.shp", true);
                pri.Style.Line = new Pen(Color.Black, 1.5f);

                // add layers
                map.Layers.Add(notin);
                map.Layers.Add(Backlay);
                map.Layers.Add(grasslay);
                map.Layers.Add(water);
                map.Layers.Add(vlay);
                map.Layers.Add(foot);
                map.Layers.Add(sec);
                map.Layers.Add(pri);

                initialized = true;
            }

            // resize
            map.Size = new Size(width, heigth);
            GeoAPI.Geometries.Envelope en = new GeoAPI.Geometries.Envelope(x1, x2, y1, y2);
            map.ZoomToBox(en);

            // create Bitmap
            Bitmap bm = (Bitmap)map.GetMap();

            // store the Bitmap in memory which is much faster than store it in the disk and then read it
            MemoryStream ms = new MemoryStream();
            bm.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms;
        }
    }
}
