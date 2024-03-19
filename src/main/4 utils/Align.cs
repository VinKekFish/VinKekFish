namespace VinKekFish_Utils;

/// <summary>Класс для временного переопределения параметров консоли</summary>
public static class AlignUtils
{
    /// <summary>Выравнивает величину на значение, кратное mod, но не менее чем min</summary>
    /// <param name="val">Значение для выравнивания</param>
    /// <param name="mod">Кратность</param>
    /// <param name="min">Минимальное значение</param>
    /// <returns>Выравненное значение</returns>
    public static nint Align(nint val, nint mod, nint min)
    {
        if (val < min)
            val = min;
        var m = val % mod;
        if (m == 0)
            return val;

        return val  + (mod - m);
    }

    /// <summary>Выравнивает величину на значение, полученное путём возведения в степень mod и домножения на min. То есть значение не менее min, далее mod*min, mod*mod*min и т.п.</summary>
    /// <param name="val">Значение для выравнивания</param>
    /// <param name="mod">Кратность</param>
    /// <param name="min">Минимальное значение</param>
    /// <returns>Выравненное значение</returns>
    public static nint AlignDegree(nint val, nint mod, nint min)
    {
        if (val <= min)
        {
            return min;
        }

        nint d = 1;
        checked
        {
            do
            {
                d *= mod;
            }
            while (min*d >= val);
        }

        return min*d;
    }

}
