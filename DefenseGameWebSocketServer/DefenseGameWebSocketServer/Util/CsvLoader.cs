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

            // 첫 줄: 헤더
            var headers = lines[0].Split(',');

            // 필드 매칭
            var type = typeof(T);
            var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');

                if (cols.Length < headers.Length)
                    continue;

                T obj = new T();

                for (int j = 0; j < headers.Length; j++)
                {
                    var header = headers[j].Trim();
                    var field = Array.Find(fields, f => f.Name.Equals(header, StringComparison.OrdinalIgnoreCase));

                    if (field == null)
                        continue;

                    try
                    {
                        object value = Convert.ChangeType(cols[j], field.PropertyType);
                        field.SetValue(obj, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CsvLoader] 변환 오류: {header} / 값: {cols[j]} / {ex.Message}");
                    }
                }

                var keyField = Array.Find(fields, f => f.Name.Equals("Id"));
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
