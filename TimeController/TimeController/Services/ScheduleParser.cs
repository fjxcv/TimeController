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
                var ext = Path.GetExtension(filePath).ToLower();

                // 尝试先读取前几个字节判断文件格式
                byte[] header = new byte[8];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, Math.Min(8, (int)fs.Length));
                }

                // 检查文件是否可能是 XML 格式（常见 HTML 或 XML 文件头）
                string headerStr = System.Text.Encoding.ASCII.GetString(header);
                if (headerStr.StartsWith("<?xml") || headerStr.StartsWith("<!DOCTY") || headerStr.StartsWith("<html>"))
                {
                    throw new InvalidOperationException("文件似乎是XML或HTML格式，无法作为Excel文件打开");
                }

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (ext == ".xls")
                    {
                        try
                        {
                            workbook = new HSSFWorkbook(stream);
                        }
                        catch (NotOLE2FileException)
                        {
                            throw new InvalidOperationException("此文件不是有效的 Excel .xls 格式，请确认文件格式正确");
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
                            throw new InvalidOperationException("此文件不是有效的 Excel .xlsx 格式，请确认文件格式正确");
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("只支持 .xls 和 .xlsx 文件格式");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Excel 文件打开失败: {ex.Message}", ex);
            }

            ISheet sheet = workbook.GetSheetAt(0);
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                IRow currentRow = sheet.GetRow(row);
                if (currentRow == null) continue;

                try
                {
                    var course = new Course
                    {
                        Name = currentRow.GetCell(0)?.ToString() ?? "",
                        DayOfWeek = currentRow.GetCell(1)?.ToString() ?? "",
                        StartTime = TimeSpan.Parse(currentRow.GetCell(2)?.ToString() ?? "00:00"),
                        EndTime = TimeSpan.Parse(currentRow.GetCell(3)?.ToString() ?? "00:00"),
                        Location = currentRow.GetCell(4)?.ToString() ?? "",
                        Teacher = currentRow.GetCell(5)?.ToString() ?? ""
                    };
                    courses.Add(course);
                    Console.WriteLine($"成功解析课程: {course.Name}, 星期{course.DayOfWeek}, {course.StartTime}-{course.EndTime}");
                }
                catch(Exception ex)
                {
                    // 记录具体错误信息
                    Console.WriteLine($"解析第 {row} 行失败: {ex.Message}");
                    // 可以考虑向用户提供更友好的错误信息
                }
            }

            return courses;
        }

        public static List<Course> ParseCsv(string filePath)
        {
            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                return csv.GetRecords<Course>().ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("CSV 解析失败", ex);
            }
        }

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
                throw new InvalidOperationException($"URL 解析失败: {ex.GetType().Name}: {ex.Message}", ex);
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
                throw new InvalidOperationException("URL 解析失败", ex);
            }
        }


    }
}

