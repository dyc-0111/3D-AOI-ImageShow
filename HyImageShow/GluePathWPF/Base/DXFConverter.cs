using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using System.Collections.Generic;
using System.Linq;

namespace HyImageShow
{
    public class DXFConverter
    {
        public DXFConverter()
        {
        }

        //Note: 存讀的資料都用mm
        #region 多層
        public static void SaveDxfFile(string fileName, List<LineLayer> layers)
        {
            var dxf = new DxfDocument();

            List<EntityObject> dxfObjectss = new List<EntityObject>();

            int i = 0;

            foreach (var layer in layers)
            {
                foreach (var linePoint in layer.Lines)
                {
                    if (linePoint.LinePointType == ELinePointType.Line)
                    {
                        var line = new Line(
                            new Vector2(linePoint.StartXInMm, linePoint.StartYInMm),
                            new Vector2(linePoint.EndXInMm, linePoint.EndYInMm));

                        line.Layer = new Layer(i.ToString());

                        dxfObjectss.Add(line);
                    }
                    else if (linePoint.LinePointType == ELinePointType.Point
                            || linePoint.LinePointType == ELinePointType.Align)
                    {
                        var point = new Point(
                            new Vector2(linePoint.StartXInMm, linePoint.StartYInMm));

                        point.IsVisible = linePoint.LinePointType == ELinePointType.Point;

                        point.Layer = new Layer(i.ToString());

                        dxfObjectss.Add(point);
                    }
                }

                i++;
            }

            dxf.Entities.Add(dxfObjectss);

            dxf.Save(fileName);
        }
        #endregion

        #region 單層
        public static void SaveDxfFile(string fileName, int layerIndex, List<LinePoint> lines)
        {
            var dxf = ToDxfDocument(layerIndex, lines);

            dxf.Save(fileName);
        }

        private static DxfDocument ToDxfDocument(int layerIndex, List<LinePoint> lines)
        {
            var dxf = new DxfDocument();

            var dxfLines = ToDxfLines(layerIndex, lines);

            dxf.Entities.Add(dxfLines);

            return dxf;
        }

        /// <summary>
        /// 回傳單個Layer線段
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private static List<Line> ToDxfLines(int layerIndex, List<LinePoint> lines)
        {
            List<Line> dxfLines = new List<Line>();

            foreach (var linePoint in lines)
            {
                var line = new Line(
                    new Vector2(linePoint.StartXInMm, linePoint.StartYInMm),
                    new Vector2(linePoint.EndXInMm, linePoint.EndYInMm));

                line.Layer = new Layer(layerIndex.ToString());

                dxfLines.Add(line);
            }

            return dxfLines;
        }

        public static List<EntityObject> LoadDxfFile(string fileName)
        {
            var dxf = DxfDocument.Load(fileName);

            var allEntities = dxf.Entities.All;

            return allEntities.ToList();
        }
        #endregion
    }
}
