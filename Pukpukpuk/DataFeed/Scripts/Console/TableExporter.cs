using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Pukpukpuk.DataFeed.Console.Entries;
using Pukpukpuk.DataFeed.Shared;
using Pukpukpuk.DataFeed.Utils;
using UnityEditor;
using UnityEngine;

namespace Pukpukpuk.DataFeed.Console
{
#if UNITY_EDITOR
    public static class TableExporter
    {
        private static readonly string[] Headers = { "Layer", "Message", "Time", "Stacktrace", "Tag" };
        private const string CsvHeaders = "Layer;Message;Time";

        private static XSSFCellStyle DefaultStyle;
        private static XSSFCellStyle DefaultStyleCentered;
        private static readonly Dictionary<(Color, int), XSSFCellStyle> CachedStyles = new();

        #region CSV Export

        public static void CreateCSVFile(List<LogEntry> log)
        {
            using StreamWriter sw = File.CreateText(GetPathForFile(".csv"));

            sw.WriteLine(CsvHeaders);
            for (int i = 0; i < log.Count; i++)
            {
                var entry = log[i];
                sw.WriteLine($"{entry.LayerText};{entry.MessageWithoutTags};{entry.TimeText}");

                if (ShouldTimeBetweenBeDrawn(log, i, out var elapsedTime))
                {
                    DrawTimeBetweenEntry(sw, elapsedTime);
                }
            }
        }
        
        #endregion
        
        public static void CreateXLSXFile(List<LogEntry> log)
        {
            ResetCachedStyles();
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            FillHeaderRow(workbook, sheet);

            var verticalShift = 1;
            var maxLengths = new int[Headers.Length];
            for (int i = 0; i < log.Count; i++)
            {
                var timeBetweenLength = 0;
                if (ShouldTimeBetweenBeDrawn(log, i, out var elapsedTime))
                {
                    timeBetweenLength = DrawTimeBetweenEntry(workbook, sheet, i + verticalShift, elapsedTime);
                    verticalShift++;
                }
                
                var row = sheet.CreateRow(i + verticalShift);
                var lengths = DrawLogEntry(log[i], row, workbook);
                lengths[1] = Math.Max(lengths[1], timeBetweenLength);
                UpdateMax(maxLengths, lengths);
                
                EditorUtility.DisplayProgressBar("Exporting log to .xlsx", $"Already exported {i} entries out of {log.Count}", (float)i / log.Count);
            }
            
            EditorUtility.ClearProgressBar();
            
            for (int i = 0; i < maxLengths.Length; i++)
            {
                var width = Math.Clamp(maxLengths[i], 0, 128) * 305;
                sheet.SetColumnWidth(i, width);
            }

            SaveXLSXFile(workbook);
        }

        private static void FillHeaderRow(IWorkbook workbook, ISheet sheet)
        {
            var style = CreateStyle(workbook, bottomBorder:true);
            
            var titleRow = sheet.CreateRow(0);
            for (int i = 0; i < Headers.Length; i++)
            {
                var cell = titleRow.CreateCell(i);
                cell.SetCellValue(Headers[i]);
                cell.CellStyle = style;
            }
        }

        private static void SaveXLSXFile(IWorkbook workbook)
        {
            using var fs = new FileStream(GetPathForFile(".xlsx"), FileMode.Create, FileAccess.Write);
            workbook.Write(fs);
        }

        #region Time Between Entry

        private static bool ShouldTimeBetweenBeDrawn(List<LogEntry> log, int index, out float elapsedTime)
        {
            elapsedTime = -1;
            if (index < 1) return false;

            var previous = log[index - 1];
            var current = log[index];

            elapsedTime = PauseEntry.GetElapsedTime(previous, current);
            if (elapsedTime < PauseEntry.MinElapsedTimeBetweenEntries) return false;
            if (!ConsoleWindow.GetConfig().AlsoAddTimeBetweenEntries) return false;

            return true;
        }
        
        private static void DrawTimeBetweenEntry(StreamWriter sw, float elapsedTime)
        {
            sw.WriteLine($"; > {GetTextForTimeBetween(elapsedTime)};");
        }
        
        private static int DrawTimeBetweenEntry(IWorkbook workbook, ISheet sheet, int rownum, float elapsedTime)
        {
            var row = sheet.CreateRow(rownum);

            for (int i = 0; i < 5; i++)
            {
                row.CreateCell(i).CellStyle = GetDefaultStyle(workbook);
            }
            
            var text = $"--- {GetTextForTimeBetween(elapsedTime)} ---";
            var cell = row.GetCell(1);
            cell.SetCellValue(text);
            cell.CellStyle = GetDefaultStyle(workbook, true);

            return text.Length;
        }
        
        private static string GetTextForTimeBetween(float elapsedTime)
        {
            return $"Time Between: {elapsedTime} seconds";
        }

        #endregion
        
        #region Entry Row Drawing

        private static int[] DrawLogEntry(LogEntry entry, IRow row, IWorkbook workbook)
        {
            DrawLayerCell(entry, row, workbook);
            DrawMessageCell(entry, row, workbook);
            DrawTimeCell(entry, row, workbook);
            DrawStacktraceCell(entry, row, workbook);
            DrawTagCell(entry, row, workbook);
            
            row.HeightInPoints = 14;
            
            var lengths = new int[Headers.Length];
            for (int i = 0; i < lengths.Length; i++)
            {
                var cell = row.GetCell(i, MissingCellPolicy.RETURN_NULL_AND_BLANK);
                lengths[i] = cell == null ? 0 : cell.StringCellValue.Length;
            }

            return lengths;
        }

        private static void DrawLayerCell(LogEntry entry, IRow row, IWorkbook workbook)
        {
            var cell = row.CreateCell(0);
            cell.SetCellValue(entry.LayerText);
            
            var color = GetColorForLayerCell(entry);
            cell.CellStyle = GetStyle(workbook, color, 0);
        }
        
        private static void DrawMessageCell(LogEntry entry, IRow row, IWorkbook workbook)
        {
            var cell = row.CreateCell(1);
            cell.SetCellValue(entry.MessageWithoutTags);
            
            var color = GetColorForLayerCell(entry);
            cell.CellStyle = IsWarningOrError(entry)
                ? GetStyle(workbook, color, 1) 
                : GetDefaultStyle(workbook);
        }

        private static void DrawTimeCell(LogEntry entry, IRow row, IWorkbook workbook)
        {
            var cell = row.CreateCell(2);
            cell.SetCellValue(entry.TimeText);
            cell.CellStyle = GetDefaultStyle(workbook);
        }
        
        private static void DrawStacktraceCell(LogEntry entry, IRow row, IWorkbook workbook)
        {
            var cell = row.CreateCell(3);
            cell.SetCellValue(entry.Stack);
            cell.CellStyle = GetDefaultStyle(workbook);
        }
        
        private static void DrawTagCell(LogEntry entry, IRow row, IWorkbook workbook)
        {
            var cell = row.CreateCell(4);
            cell.SetCellValue(entry.Tag);
            cell.CellStyle = GetDefaultStyle(workbook);
        }
        
        #endregion
        
        #region Style Generation

        private static void ResetCachedStyles()
        {
            DefaultStyle = null;
            DefaultStyleCentered = null;
            CachedStyles.Clear();
        }
        
        private static XSSFCellStyle GetDefaultStyle(IWorkbook workbook, bool centered = false)
        {
            if (centered) return DefaultStyleCentered ??= CreateStyle(workbook, textCentered:true);
            return DefaultStyle ??= CreateStyle(workbook);
        }
        
        private static XSSFCellStyle GetStyle(IWorkbook workbook, Color baseColor, int columnIndex)
        {
            var key = (baseColor, columnIndex);
            if (CachedStyles.ContainsKey(key)) return CachedStyles[key];
            
            var colors = BlendColor(baseColor, columnIndex == 0);
            var style = CreateStyle(workbook, colors.foregroundColor, colors.textColor);
            CachedStyles.Add(key, style);
            return style;
        }

        private static (Color foregroundColor, Color textColor) BlendColor(Color baseColor, bool bright)
        {
            var foregroundPercent = bright ? .25f : .12f;
            var textPercent = bright ? .35f : .22f;
            
            var foregroundColor = Color.Lerp(Color.white, baseColor, foregroundPercent);
            var textColor = Color.Lerp(Color.black, baseColor, textPercent);

            return (foregroundColor, textColor);
        }
        
        private static XSSFCellStyle CreateStyle(
            IWorkbook workbook,
            Color? foregroundColor = null, 
            Color? textColor = null, 
            bool bottomBorder = false,
            bool textCentered = false)
        {
            var style = (XSSFCellStyle)workbook.CreateCellStyle();
            if (foregroundColor.HasValue)
            {
                style.SetFillForegroundColor(foregroundColor.Value.ToXSSFColor());
                style.FillPattern = FillPattern.SolidForeground;
            }

            var font = (XSSFFont)workbook.CreateFont();
            font.FontName = ConsoleWindow.GetConfig().ExportTableFont;
            
            if (textColor.HasValue) font.SetColor(textColor.Value.ToXSSFColor());
            style.SetFont(font);

            style.WrapText = true;
            style.VerticalAlignment = VerticalAlignment.Top;
            if (textCentered) style.Alignment = HorizontalAlignment.Center;
            
            style.BorderLeft = BorderStyle.Thick;
            style.BorderRight = BorderStyle.Thick;
            if (bottomBorder) style.BorderBottom = BorderStyle.Thick;

            return style;
        }

        #endregion
        
        #region Utils
        
        private static string GetPathForFile(string fileExtension)
        {
            var fileName = GetNameForLogFile() + fileExtension;
            return Path.Combine(Application.dataPath, "../Logs", fileName);
        }
        
        private static string GetNameForLogFile() 
        {
            var projectName = GetProjectName();
            var date = DateTime.Now.ToString("dd.MM.yyyy-HH-mm-ss");
            
            return $"DF-{projectName}-{date}";
        }
        
        private static string GetProjectName()
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[^2];
            return projectName;
        }
        
        private static bool IsWarningOrError(LogEntry entry)
        {
            return entry.MessageType is LogMessageType.Warning or LogMessageType.Error;
        }
        
        private static Color GetColorForLayerCell(LogEntry entry)
        {
            return IsWarningOrError(entry) 
                ? entry.MessageType.GetColor() 
                : entry.Layer.GetColor();
        }

        private static XSSFColor ToXSSFColor(this Color color)
        {
            var bytes = new byte[3];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(color[i] * 255f);
            }

            return new XSSFColor(bytes);
        }

        private static void UpdateMax(int[] source, int[] additional)
        {
            var length = Math.Min(source.Length, additional.Length);
            for (int j = 0; j < length; j++)
            {
                source[j] = Math.Max(source[j], additional[j]);
            }
        }
        
        #endregion
    }
#endif
}