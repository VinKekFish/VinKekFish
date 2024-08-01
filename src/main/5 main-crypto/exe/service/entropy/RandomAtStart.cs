// TODO: tests
namespace VinKekFish_EXE;

public partial class Regime_Service
{
    /// <summary>Представляет файлы для сбора энтропии</summary>
    public class RandomAtFolder_Current
    {
        public List<FileInfo> current = new(countOfFiles);
        public const int countOfFiles = 4;

        public readonly Regime_Service service;
        public RandomAtFolder_Current(Regime_Service service)
        {
            this.service = service;
            for (int i = 0; i < countOfFiles; i++)
            {
                var path = Path.Combine(service.RandomAtFolder!.FullName, $"current.{i}");
                current.Add(  new FileInfo(path)  );
            }
        }

        public void Refresh()
        {
            for (int i = 0; i < countOfFiles; i++)
            {
                current[i].Refresh();
            }
        }

        /// <summary>Возвращает описатель файла, который не существует. А если все существуют, файла, который менее, чем установленный размер. Если такие файлы не найдены, возвращает null.</summary>
        public FileInfo? GetFirstNotExists()
        {
            for (int i = 0; i < countOfFiles; i++)
            {
                var cur = current[i];
                cur.Refresh();
                if (!cur.Exists)
                    return cur;
            }

            for (int i = 0; i < countOfFiles; i++)
            {
                var cur = current[i];

                if (cur.Length < Regime_Service.OutputStrenght)
                    return cur;
            }

            return null;
        }

        /// <summary>Возвращает описатель самого старого файла.</summary>
        public FileInfo GetOldestFile()
        {
            var result = current[0];
            for (int i = 1; i < countOfFiles; i++)
            {
                var cur = current[i];

                if (result.LastWriteTime > cur.LastWriteTime)
                    result = cur;
            }

            return result;
        }
    }
}
