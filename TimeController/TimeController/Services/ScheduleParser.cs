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
using System.Windows;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.Services
{
    public static class ScheduleParser
    {
        public static List<Course> ParseExcel(string filePath)
        {
            var courses = new List<Course>();
            IWorkbook workbook;

            try
            {

                // 尝试先读取前几个字节判断文件格式
                byte[] header = new byte[8];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, Math.Min(header.Length, (int)fs.Length));
                }

                // 检查文件是否可能是XML格式（常见HTML或XML文件头）
                var headerStr = Encoding.ASCII.GetString(header);
                if (headerStr.StartsWith("<?xml")
                 || headerStr.StartsWith("<!DOCTY")
                 || headerStr.StartsWith("<html>"))
                {
                    MessageBox.Show(
                        "该文件看起来是 XML/HTML 格式，无法作为 Excel 打开。",
                        "格式错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return courses;
                }
                // 2）真正打开流，这里不允许共享写锁，文件被占用时会抛 IOException
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None);

                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext == ".xls")
                {
                    try
                    {
                        workbook = new HSSFWorkbook(stream);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            "此文件不是有效的 Excel .xls 格式，请确认文件格式正确。",
                            "格式错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return courses;
                    }
                }
                else if (ext == ".xlsx")
                {
                    try
                    {
                        workbook = new XSSFWorkbook(stream);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(
                            "此文件不是有效的 Excel .xlsx 格式，请确认文件格式正确。",
                            "格式错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return courses;
                    }
                }
                else
                {
                    MessageBox.Show(
                        "只支持 .xls 和 .xlsx 两种 Excel 文件格式。",
                        "不支持的格式",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return courses;
                }
            }
            catch (IOException ioEx) when ((uint)ioEx.HResult == 0x80070020)
            {
                // 文件正在被占用
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "该 Excel 文件当前正被其他程序占用，请先关闭后重试。",
                        "文件占用",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
                return courses;
            }
            catch (IOException ioEx)
            {
                MessageBox.Show(
                    $"打开 Excel 文件时发生 I/O 错误：{ioEx.Message}",
                    "文件错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return courses;
            }
            catch (Exception ex)
            {
                // 其他未预料的错误继续向上抛
                throw new InvalidOperationException($"Excel 文件打开失败: {ex.Message}", ex);
            }

            // —— 到这里 workbook 已经建立，可以安全读取第一个工作表 —— 
            ISheet sheet = workbook.GetSheetAt(0);
            int rowCount = sheet.LastRowNum;
            if (rowCount < 1)
            {
                MessageBox.Show(
                    "Excel 文件没有数据或格式不正确。",
                    "读取失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return courses;
            }

            // 从第二行开始读（跳过表头）
            for (int i = 1; i <= rowCount; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                try
                {
                    string name = row.GetCell(0)?.ToString().Trim() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;

                    string dayOfWeek = row.GetCell(1)?.ToString().Trim() ?? "";
                    var start = TimeSpan.Parse(row.GetCell(2)?.ToString() ?? "00:00");
                    var end = TimeSpan.Parse(row.GetCell(3)?.ToString() ?? "00:00");
                    string location = row.GetCell(4)?.ToString().Trim() ?? "";
                    string teacher = row.GetCell(5)?.ToString().Trim() ?? "";
                    string weekPattern = row.LastCellNum > 6
                        ? row.GetCell(6)?.ToString().Trim() ?? "1-16"
                        : "1-16";

                    if (start >= end)
                        continue; // 跳过时间非法的行

                    courses.Add(new Course
                    {
                        Name = name,
                        DayOfWeek = dayOfWeek,
                        StartTime = start,
                        EndTime = end,
                        Location = location,
                        Teacher = teacher,
                        WeekPattern = weekPattern
                    });
                }
                catch
                {
                    // 单行解析出错就跳过，继续下一行
                    continue;
                }
            }

            return courses;
        }

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


        public static List<Course> ParseCsv(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath, GetEncoding(filePath));

                var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null
                };

                using var csv = new CsvReader(reader, csvConfig);

                // 读取并映射CSV记录
                var records = new List<Course>();

                // 读取表头
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解析CSV行失败: {ex.Message}");
                    }
                }

                return records;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"CSV文件解析失败: {ex.Message}", ex);
            }
        }

        // 检测文件编码
        private static Encoding GetEncoding(string filePath)
        {
            using var reader = new StreamReader(filePath, Encoding.Default, true);
            reader.Read();
            return reader.CurrentEncoding;
        }

        // URL导入方法保持不变
        public static List<Course> ParseFromUrl(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                // 设置超时和更友好的请求头
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TimeController/1.0");

                // 添加更详细的诊断信息
                Console.WriteLine($"正在请求URL: {url}");

                var response = httpClient.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string content = response.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new InvalidOperationException("服务器返回了空内容");
                }

                Console.WriteLine($"获取到响应内容，长度: {content.Length}");

                return JsonConvert.DeserializeObject<List<Course>>(content)
                    ?? throw new InvalidOperationException("无法将JSON反序列化为课程列表");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"HTTP请求失败: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("请求超时，请检查网络连接或URL是否正确", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("返回内容不是有效的JSON格式", ex);
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"网络连接错误: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"URL解析失败: {ex.GetType().Name}: {ex.Message}", ex);
            }
        }

        public static async Task<List<Course>> ParseFromUrlAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "TimeController/1.0");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();

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
