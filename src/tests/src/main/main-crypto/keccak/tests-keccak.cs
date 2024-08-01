// #define CAN_CREATEFILE_FOR_keccak

namespace cryptoprime_tests;

using System.Text;
using cryptoprime;
using DriverForTestsLib;

public class Keccak_test_parent: ParentAutoSaveTask
{
    #if CAN_CREATEFILE_FOR_keccak
    public static readonly bool canCreateFile = true;
    #warning CAN_CREATEFILE_FOR_keccak
    #else
    public static readonly bool canCreateFile = false;
    #endif

    protected Keccak_test_parent(TestConstructor constructor, SaverParent parentSaver): base
    (
        executer_and_saver: parentSaver,
        constructor:        constructor,
        canCreateFile:      canCreateFile
    )
    {
        this.parentSaver = parentSaver;
    }

    public override DirectoryInfo SetDirForFiles()
    {
        return GetDirectoryPath("src/tests/src/main/main-crypto/keccak/");
    }

    protected SaverParent parentSaver;
    protected abstract class SaverParent: TaskResultSaver
    {}

    /// <summary>Вычисление функции erfc (дополнительная функция ошибок "эрфик")</summary>
    /// <param name="x">Аргумент функции</param>
    /// <param name="torelance">Точность</param>
    /// <returns>erfc(x)</returns>
    // Для проверки https://statpages.info/scicalc.html или http://www.mhtl.uwaterloo.ca/old/courses/me303/calc/func_calc.html
    public static decimal erfc(decimal x, decimal tolerance = 1e-26m)
    {
        /*
        Формула для приближения:
        сумма от 0 до бесконечности по n
        2/sqrt(pi)*summ(x/(2n+1)*P[i=1;n](-x^2/i))
        */
        decimal K = (decimal)(  2 / sqrt(PI)  );
        decimal R = 0;
        decimal A = 0;

        decimal n = 0;
        do
        {
            A  = x / (2 * n + 1) * P(n, x);
            R += A;
            n++;

            if (n > 128)
                //throw new ArgumentException($"Very high tolerance: {tolerance:E}; x = {x:E}, erfc(x)={1m-R:E}", "tolerance");
                break;
        }
        while (Math.Abs(R) < tolerance || Math.Abs(A/R) > tolerance);

        decimal result = (decimal)(  R * K  );
        return 1m - result;

        decimal P(decimal n, decimal x)
        {
            decimal result = 1;
            for (int i = 1; i <= n; i++)
                result *= -1*x*x/i;

            return result;
        }
    }

    // https://habr.com/ru/companies/securitycode/articles/237695/
    // Частотный побитовый тест
    // Вычисление erfc(deviation/sqrt(2*n)) должно быть более 0,01 (обычный диапазон - 0,001-0,01)
    // n - количество битов
    /// <summary>Для частотного побитового теста. https://habr.com/ru/companies/securitycode/articles/237695/
    /// Вычисляет P-значение (вероятность) того, что для отклонения x на последовательности длиной nBits битов, наш генератор произвёл значение не хуже, чем абсолютно случайный генератор</summary>
    /// <param name="x">Отклонение количества установленных битов от идеального значения</param>
    /// <param name="nBits">Длина массива битов (в битах)</param>
    /// <param name="tolerance">Точность расчёта. Можно не указывать</param>
    public static decimal erfc_2N(decimal x, int nBits, decimal tolerance = 1e-26m)
    {
        return erfc(x / sqrt(nBits) / sqrt2);
    }

    /// <summary>Вычисляет отклонение количества установленных битов от идеально случайного (половина битов установлена)</summary>
    /// <param name="S">Массив битов</param>
    /// <param name="len">Длина массива битов в битах! (не в байтах!)</param>
    /// <returns>Отклонение, которое можно использовать в функции erfc_2N</returns>
    public unsafe static int GetDeviationOfBits(byte * S, int len)
    {
        int cnt = GetCountsOfSettedBits(S, len);

        var etalonCnt = len >> 1;
        var deviation = etalonCnt - cnt;
        if (deviation < 0)
            deviation *= -1;

        return deviation;
    }

    /// <summary>Вычисляет количество установленных битов в массиве</summary>
    /// <param name="S">Массив</param>
    /// <param name="len">Длина массива в битах! Не в байтах!</param>
    public unsafe static int GetCountsOfSettedBits(byte * S, int len)
    {
        var cnt = 0;
        for (int i = 0; i < len; i++)
            cnt += BitToBytes.GetBit(S, i) ? 1 : 0;

        return cnt;
    }

    public static readonly decimal PI    = 3.1415926535_8979323846_2643383279_5028841971m;
    public static readonly decimal sqrt2 = 1.41421356237309504880168872420969807856967187537694807317667m;

    public static decimal sqrt(decimal x, decimal tolerance = 1e-27m)
    {
        if (x == 0)
            return 0;
        if (x < 0)
            throw new ArgumentOutOfRangeException();
        
        decimal max, min, avg, xx;
        if (x <= 1.0m)
        {
            max = 1.0m;
            min = x;
        }
        else
        {
            max = x;
            min = 1m;
        }

        do
        {
            avg = (max + min) / 2m;
            xx  = avg * avg;
            if (xx > x)
                max = avg;
            else
                min = avg;
        }
        while (Math.Abs((xx - x)/x) > tolerance);

        return (max + min) / 2m;
    }
}
