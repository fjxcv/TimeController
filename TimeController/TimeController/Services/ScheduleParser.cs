using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using CsvHelper;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel; // .xls
using NPOI.XSSF.UserModel; // .xlsx
using TimeController.Models;
using NPOI.POIFS.FileSystem;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace TimeController.Services
{
    public static class ScheduleParser
    {

        // 获取单元格字符串值，处理不同类型的单元格
        private static string GetCellStringValue(ICell cell)
        {
            if (cell == null) return string.Empty;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    // 处理日期格式和数字
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        var date = cell.DateCellValue;
                        return string.Format(new CultureInfo("en-US"), "{0:HH:mm}", date);
                    }

                    return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    try
                    {
                        // 尝试获取公式计算结果
                        return cell.StringCellValue;
                    }
                    catch
                    {
                        try
                        {
                            return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    }
                default:
                    return string.Empty;
            }
        }


        // 从文件导入课程
        public static async Task<List<Course>> ParseExcelAsync(string filePath, IProgress<(int current, int total, string message)> progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report((0, 100, "正在打开Excel文件..."));

                    var courses = new List<Course>();
                    IWorkbook workbook;

                    // 打开Excel文件的代码
                    var ext = Path.GetExtension(filePath).ToLower();

                    // 尝试先读取前几个字节判断文件格式
                    byte[] header = new byte[8];
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(header, 0, Math.Min(8, (int)fs.Length));
                    }

                    // 检查文件是否可能是XML格式（常见HTML或XML文件头）
                    string headerStr = Encoding.ASCII.GetString(header);
                    if (headerStr.StartsWith("<?xml") || headerStr.StartsWith("<!DOCTY") || headerStr.StartsWith("<html>"))
                    {
                        throw new InvalidOperationException("文件似乎是XML或HTML格式，无法作为Excel文件打开");
                    }

                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        progress?.Report((10, 100, "正在加载工作表..."));

                        if (ext == ".xls")
                        {
                            try
                            {
                                workbook = new HSSFWorkbook(stream);
                            }
                            catch (NotOLE2FileException)
                            {
                                throw new InvalidOperationException("此文件不是有效的Excel .xls格式，请确认文件格式正确");
                            }
                        }
                        else if (ext == ".xlsx")
                        {
                            try
                            {
                                workbook = new XSSFWorkbook(stream);
                            }
                            catch
                            {
                                throw new InvalidOperationException("此文件不是有效的Excel .xlsx格式，请确认文件格式正确");
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("只支持.xls和.xlsx文件格式");
                        }
                    }

                    ISheet sheet = workbook.GetSheetAt(0);
                    int rowCount = sheet.LastRowNum;

                    // 确保至少有表头行
                    if (rowCount < 0)
                    {
                        throw new InvalidOperationException("Excel文件为空或格式不正确");
                    }

                    progress?.Report((20, 100, $"共发现{rowCount}行数据，开始解析..."));

                    // 从第二行开始（跳过表头）读取数据
                    for (int row = 1; row <= rowCount; row++)
                    {
                        // 定期更新进度
                        if (row % 5 == 0 || row == rowCount)
                        {
                            int percentage = 20 + (row * 70 / rowCount); // 20%~90%的进度范围
                            progress?.Report((percentage, 100, $"正在解析第 {row}/{rowCount} 行..."));
                        }

                        IRow currentRow = sheet.GetRow(row);
                        if (currentRow == null) continue;

                        try
                        {
                            // 检查课程名称是否为空
                            var nameCell = currentRow.GetCell(0);
                            if (nameCell == null || string.IsNullOrWhiteSpace(nameCell.ToString()))
                            {
                                continue;
                            }

                            // 读取并解析数据
                            string name = GetCellStringValue(currentRow.GetCell(0));
                            string dayOfWeek = GetCellStringValue(currentRow.GetCell(1));
                            string startTimeStr = GetCellStringValue(currentRow.GetCell(2));
                            string endTimeStr = GetCellStringValue(currentRow.GetCell(3));
                            string location = GetCellStringValue(currentRow.GetCell(4));
                            string teacher = GetCellStringValue(currentRow.GetCell(5));

                            // 读取周次模式（如果存在）
                            string weekPattern = currentRow.LastCellNum > 6 ?
                                GetCellStringValue(currentRow.GetCell(6)) : "1-16";

                            // 确保周次模式不为空
                            if (string.IsNullOrWhiteSpace(weekPattern))
                            {
                                weekPattern = "1-16";
                            }

                            // 解析时间
                            TimeSpan startTime = ParseTimeSpan(startTimeStr);
                            TimeSpan endTime = ParseTimeSpan(endTimeStr);

                            // 验证时间
                            if (startTime >= endTime)
                            {
                                continue;
                            }

                            var course = new Course
                            {
                                Name = name,
                                DayOfWeek = dayOfWeek,
                                StartTime = startTime,
                                EndTime = endTime,
                                Location = location,
                                Teacher = teacher,
                                WeekPattern = weekPattern
                            };

                            courses.Add(course);
                        }
                        catch (Exception)
                        {
                            // 记录错误但继续处理下一行
                            continue;
                        }
                    }

                    progress?.Report((95, 100, "解析完成，准备返回数据..."));

                    return courses;
                }
                catch (Exception ex)
                {
                    progress?.Report((100, 100, $"解析出错: {ex.Message}"));
                    throw;
                }
            });
        }

        // 从CSV文件导入课程
        public static async Task<List<Course>> ParseCsvAsync(string filePath, IProgress<(int current, int total, string message)> progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    progress?.Report((0, 100, "正在打开CSV文件..."));

                    using var reader = new StreamReader(filePath, GetEncoding(filePath));

                    var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        MissingFieldFound = null,
                        BadDataFound = null
                    };

                    progress?.Report((20, 100, "正在读取CSV数据..."));

                    using var csv = new CsvReader(reader, csvConfig);

                    // 读取并映射CSV记录
                    var records = new List<Course>();

                    // 读取表头
                    csv.Read();
                    csv.ReadHeader();

                    progress?.Report((30, 100, "开始解析记录..."));

                    int rowIndex = 0;
                    int estimatedRowCount = 100; // 预估行数，CSV无法提前知道总行数

                    while (csv.Read())
                    {
                        rowIndex++;

                        // 每解析10行更新一次进度
                        if (rowIndex % 10 == 0)
                        {
                            int progressValue = 30 + Math.Min(60, (rowIndex * 60 / estimatedRowCount));
                            progress?.Report((progressValue, 100, $"已解析 {rowIndex} 行..."));

                            // 动态调整估计总行数
                            if (rowIndex > estimatedRowCount)
                            {
                                estimatedRowCount = rowIndex * 2;
                            }
                        }

                        try
                        {
                            string name = csv.GetField("课程名称") ?? csv.GetField(0) ?? "";

                            // 如果课程名称为空，跳过
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            string dayOfWeek = csv.GetField("星期几") ?? csv.GetField(1) ?? "";
                            string startTimeStr = csv.GetField("开始时间") ?? csv.GetField(2) ?? "";
                            string endTimeStr = csv.GetField("结束时间") ?? csv.GetField(3) ?? "";
                            string location = csv.GetField("上课地点") ?? csv.GetField(4) ?? "";
                            string teacher = csv.GetField("教师姓名") ?? csv.GetField(5) ?? "";
                            string weekPattern = csv.GetField("周次") ?? csv.GetField(6) ?? "1-16";

                            // 解析时间
                            TimeSpan startTime = ParseTimeSpan(startTimeStr);
                            TimeSpan endTime = ParseTimeSpan(endTimeStr);

                            // 验证时间
                            if (startTime >= endTime) continue;

                            var course = new Course
                            {
                                Name = name,
                                DayOfWeek = dayOfWeek,
                                StartTime = startTime,
                                EndTime = endTime,
                                Location = location,
                                Teacher = teacher,
                                WeekPattern = weekPattern
                            };

                            records.Add(course);
                        }
                        catch (Exception)
                        {
                            // 记录错误但继续处理下一行
                            continue;
                        }
                    }

                    progress?.Report((95, 100, $"CSV解析完成，共解析{rowIndex}行，成功导入{records.Count}门课程"));

                    return records;
                }
                catch (Exception ex)
                {
                    progress?.Report((100, 100, $"解析出错: {ex.Message}"));
                    throw new InvalidOperationException($"CSV文件解析失败: {ex.Message}", ex);
                }
            });
        }


        // 解析各种格式的时间字符串为TimeSpan
        private static TimeSpan ParseTimeSpan(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
            {
                throw new ArgumentException("时间字符串不能为空");
            }

            // 尝试直接解析为TimeSpan
            if (TimeSpan.TryParse(timeStr, out TimeSpan result))
            {
                return result;
            }

            // 尝试解析为日期时间格式，提取时间部分
            string[] formats = { "H:mm", "HH:mm", "h:mm tt", "hh:mm tt", "H.mm", "HH.mm" };
            if (DateTime.TryParseExact(timeStr, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime.TimeOfDay;
            }

            // 尝试解析小时数（如果只有一个数字）
            if (int.TryParse(timeStr, out int hourValue))
            {
                if (hourValue >= 0 && hourValue < 24)
                {
                    return new TimeSpan(hourValue, 0, 0);
                }
            }

            // 尝试解析"8点30分"这样的中文格式
            if (timeStr.Contains("点") || timeStr.Contains("分"))
            {
                int chineseHour = 0, chineseMinute = 0;

                int pointIndex = timeStr.IndexOf("点");
                if (pointIndex > 0 && int.TryParse(timeStr.Substring(0, pointIndex), out int h))
                {
                    chineseHour = h;

                    int minuteIndex = timeStr.IndexOf("分");
                    if (minuteIndex > pointIndex && int.TryParse(
                        timeStr.Substring(pointIndex + 1, minuteIndex - pointIndex - 1), out int m))
                    {
                        chineseMinute = m;
                    }

                    return new TimeSpan(chineseHour, chineseMinute, 0);
                }
            }

            throw new FormatException($"无法解析时间格式: {timeStr}");
        }

        // 检测文件编码
        private static Encoding GetEncoding(string filePath)
        {
            using var reader = new StreamReader(filePath, Encoding.Default, true);
            reader.Read();
            return reader.CurrentEncoding;
        }

        // 从URL解析课表
        public static async Task<List<Course>> ParseFromUrlAsync(string url, IProgress<string> progress = null)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TimeController/1.0");

                progress?.Report("正在请求数据...");
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content))
                    throw new InvalidOperationException("服务器返回了空内容");

                progress?.Report("正在解析JSON数据...");
                return JsonConvert.DeserializeObject<List<Course>>(content)
                    ?? new List<Course>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"URL解析失败: {ex.Message}", ex);
            }
        }

        // 使用NPOI创建Excel模板文件
        public static void CreateExcelTemplate(string filePath)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("课表模板");

            // 创建表头样式
            ICellStyle headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            IFont headerFont = workbook.CreateFont();
            headerFont.Boldweight = (short)FontBoldWeight.Bold;
            headerStyle.SetFont(headerFont);

            // 创建表头行
            IRow headerRow = sheet.CreateRow(0);
            string[] headers = { "课程名称", "星期几", "开始时间", "结束时间", "上课地点", "教师姓名", "周次模式" };

            for (int i = 0; i < headers.Length; i++)
            {
                ICell cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
                sheet.AutoSizeColumn(i);
            }

            // 创建示例数据行
            IRow exampleRow1 = sheet.CreateRow(1);
            exampleRow1.CreateCell(0).SetCellValue("高等数学");
            exampleRow1.CreateCell(1).SetCellValue("周一");
            exampleRow1.CreateCell(2).SetCellValue("08:00");
            exampleRow1.CreateCell(3).SetCellValue("09:40");
            exampleRow1.CreateCell(4).SetCellValue("教学楼A101");
            exampleRow1.CreateCell(5).SetCellValue("张教授");
            exampleRow1.CreateCell(6).SetCellValue("1-16");

            IRow exampleRow2 = sheet.CreateRow(2);
            exampleRow2.CreateCell(0).SetCellValue("数据结构");
            exampleRow2.CreateCell(1).SetCellValue("周三");
            exampleRow2.CreateCell(2).SetCellValue("14:00");
            exampleRow2.CreateCell(3).SetCellValue("15:40");
            exampleRow2.CreateCell(4).SetCellValue("计算机楼204");
            exampleRow2.CreateCell(5).SetCellValue("李老师");
            exampleRow2.CreateCell(6).SetCellValue("1,3,5,7,9,11,13,15");

            // 添加说明
            IRow noteRow1 = sheet.CreateRow(4);
            noteRow1.CreateCell(0).SetCellValue("填写说明:");

            string[] notes = {
                "1. 课程名称必填",
                "2. 星期几可填写: 周一/星期一/一/1等格式",
                "3. 时间格式为24小时制: HH:MM",
                "4. 周次模式可填写: 1-16(表示1到16周), 1,3,5(表示1,3,5周)"
            };

            for (int i = 0; i < notes.Length; i++)
            {
                IRow noteRow = sheet.CreateRow(5 + i);
                noteRow.CreateCell(0).SetCellValue(notes[i]);
            }

            // 调整列宽
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // 保存文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }
    }
}
