/* using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.Models;
using System.Collections.Generic;
using System.IO;
using System.Formats.Asn1;
using System.Globalization;
using OfficeOpenXml;
using CsvHelper;
using System.Net.Http;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel; // 用于 .xls
using NPOI.XSSF.UserModel; // 用于 .xlsx

namespace TimeController.Services
{
    public static class ScheduleParser
    {

        public static List<Course> ParseExcel(string filePath)
        {
            var courses = new List<Course>();
            IWorkbook workbook;

            // 根据扩展名选择解析引擎
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (Path.GetExtension(filePath).ToLower() == ".xls")
                {
                    workbook = new HSSFWorkbook(stream); // 旧版 Excel
                }
                else
                {
                    workbook = new XSSFWorkbook(stream); // 新版 Excel
                }
            }

            ISheet sheet = workbook.GetSheetAt(0);
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                IRow currentRow = sheet.GetRow(row);
                if (currentRow == null) continue;

                var course = new Course
                {
                    Name = currentRow.GetCell(0)?.ToString() ?? "",
                    DayOfWeek = currentRow.GetCell(1)?.ToString() ?? "",
                    StartTime = TimeSpan.Parse(currentRow.GetCell(2)?.ToString()),
                    EndTime = TimeSpan.Parse(currentRow.GetCell(3)?.ToString()),
                    Location = currentRow.GetCell(4)?.ToString() ?? "",
                    Teacher = currentRow.GetCell(5)?.ToString() ?? ""
                };
                courses.Add(course);
            }

            return courses;
        }

        public static List<Course> ParseCsv(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    return csv.GetRecords<Course>().ToList(); // 直接返回解析结果
                }
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
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode();
                    string content = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<List<Course>>(content);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("URL 解析失败", ex);
            }
        }
    }
}
*/
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
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".xls")
                    {
                        workbook = new HSSFWorkbook(stream);
                    }
                    else if (ext == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(stream);
                    }
                    else
                    {
                        throw new NotSupportedException("只支持 .xls 和 .xlsx 文件格式");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Excel 文件打开失败", ex);
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
                }
                catch
                {
                    // 这里可以选择记录日志或者忽略格式错误的行
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
                var response = httpClient.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string content = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<Course>>(content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("URL 解析失败", ex);
            }
        }
    }
}

