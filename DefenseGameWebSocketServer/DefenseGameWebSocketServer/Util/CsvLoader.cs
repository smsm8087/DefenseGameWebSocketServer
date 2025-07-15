using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DefenseGameWebSocketServer.Util
{
    public static class CsvLoader
    {
        public static Dictionary<int, T> Load<T>(string csvPath) where T : class
        {
            var baseDir = AppContext.BaseDirectory;
            var fullPath = Path.Combine(baseDir, csvPath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[Error] CSV 파일 없음: {fullPath}");
                return new Dictionary<int, T>();
            }

            try
            {
                using var reader = new StreamReader(fullPath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,
                });

                // List<float> 등을 위한 TypeConverter 자동 등록
                csv.Context.TypeConverterCache.AddConverter(typeof(List<int>), new ListConverter<int>());
                csv.Context.TypeConverterCache.AddConverter(typeof(List<float>), new ListConverter<float>());
                csv.Context.TypeConverterCache.AddConverter(typeof(List<string>), new ListConverter<string>());

                var records = csv.GetRecords<T>().ToList();

                // id 필드로 Dictionary 구성
                var idProp = typeof(T).GetProperty("id");
                if (idProp == null)
                {
                    Console.WriteLine($"[Error] id 프로퍼티 없음 - {typeof(T).Name}");
                    return new Dictionary<int, T>();
                }

                var dict = new Dictionary<int, T>();
                foreach (var record in records)
                {
                    int id = (int)Convert.ChangeType(idProp.GetValue(record), typeof(int));
                    dict[id] = record;
                }

                Console.WriteLine($"[CsvLoader] {typeof(T).Name} → {dict.Count}개 로드 완료!");
                return dict;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CsvLoader] 로딩 실패: {ex.Message}");
                return new Dictionary<int, T>();
            }
        }
    }

    // 범용 List<T> 파서
    public class ListConverter<TElement> : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<TElement>();

            text = text.Trim('[', ']', '"');
            var parts = text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<TElement>();
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var value = (TElement)Convert.ChangeType(trimmed, typeof(TElement), CultureInfo.InvariantCulture);
                result.Add(value);
            }

            return result;
        }
    }
}