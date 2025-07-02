using System.Collections;
using System.Reflection;

namespace DefenseGameWebSocketServer.Util
{
    public static class CsvLoader
    {
        public static Dictionary<int, T> Load<T>(string csvPath) where T : new()
        {
            var dict = new Dictionary<int, T>();

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[Error] CSV 파일 없음: {csvPath}");
                return dict;
            }

            var lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
            {
                Console.WriteLine($"[Error] CSV 파일 빈 내용: {csvPath}");
                return dict;
            }

            var headers = lines[0].Split(',');
            var type = typeof(T);
            var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < headers.Length) continue;

                T obj = new T();

                for (int j = 0; j < headers.Length; j++)
                {
                    var header = headers[j].Trim();
                    var field = Array.Find(fields, f => f.Name.Equals(header, StringComparison.OrdinalIgnoreCase));
                    if (field == null) continue;

                    try
                    {
                        var rawValue = cols[j].Trim();

                        // List<T> 처리
                        if (field.PropertyType.IsGenericType &&
                            field.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var elementType = field.PropertyType.GetGenericArguments()[0];

                            // [1000,1001,1002] or 1000;1001;1002
                            rawValue = rawValue.Trim('[', ']');
                            var stringValues = rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                            foreach (var val in stringValues)
                            {
                                var converted = Convert.ChangeType(val.Trim(), elementType);
                                list.Add(converted);
                            }

                            field.SetValue(obj, list);
                        }
                        else
                        {
                            // 단일 값
                            object value = Convert.ChangeType(rawValue, field.PropertyType);
                            field.SetValue(obj, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CsvLoader] 변환 오류: {header} / 값: {cols[j]} / {ex.Message}");
                    }
                }

                // 기본 키 "id" 추출
                var keyField = Array.Find(fields, f => f.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
                if (keyField == null)
                {
                    Console.WriteLine($"[Error] 기본키(Id)가 없음 - {typeof(T).Name}");
                    continue;
                }

                int key = (int)Convert.ChangeType(keyField.GetValue(obj), typeof(int));
                dict[key] = obj;
            }

            Console.WriteLine($"[CsvLoader] {typeof(T).Name} → {dict.Count}개 로드 완료!");
            return dict;
        }
    }
}