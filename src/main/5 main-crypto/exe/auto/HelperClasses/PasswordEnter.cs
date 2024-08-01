// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class PasswordEnter: IDisposable
{
    // [{( - скобки убираем, проще запоминать просто слово "скобка"
    // < - меньше и больше легко перепутать, оставляем только один из них
    // ! слишком похож на черту и имеет сложное наименование
    // Ч - похоже на 4. ± - сложное наименование, к тому же, похожее на два занака "+-", идущие подряд
    // "qwertyuiopasdfghjkLzxcvbnm1234567890,.<?;':(+-*/=|&^%$#@ΣΔΨλШЫЭЯ";
    /// <summary>Разрешённые символы для стандартного пароля</summary>
    public static readonly string GrantedSymbols = "qwertyuiopasdfghjkLzxcvbnm1234567890[+*/=&^%$#@";

    protected CascadeSponge_mt_20230930  sponge;
    protected VinKekFishBase_KN_20210525 vkf;
    protected Record passwordArray;
    /// <summary>x - вертикальная координата, как при индексировании матриц.</summary>
    public readonly int x = 16, y = 16;
    public readonly byte regime = 0;

    public PasswordEnter(CascadeSponge_mt_20230930 sponge, VinKekFishBase_KN_20210525 vkf, byte regime, nint countOfStepsForPermitations = 0, nint ArmoringSteps = 0, bool doErrorMessage = false)
    {
        // На всякий случай переоткрываем поток ввода, т.к. он может быть перенаправлен с помощью SetIn для получения конфигурации
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        Console.Clear();

        this.sponge   = sponge;
        this.vkf      = vkf;
        this.regime   = regime;

        if (this.vkf.input is null || this.vkf.input.size < this.sponge.maxDataLen)
            throw new ArgumentOutOfRangeException("PasswordEnter: this.vkf.input.size < this.sponge.maxDataLen");

        var len = x * y * 2;
        if (len % GrantedSymbols.Length != 0)
        {
            len += GrantedSymbols.Length - (len % GrantedSymbols.Length);
        }

        passwordArray = Keccak_abstract.allocator.AllocMemory(len);
        for (int i = 0; i < len; i++)
            passwordArray[i] = (byte) GrantedSymbols[i % GrantedSymbols.Length];

        var  passwd = stackalloc byte[(int) sponge.maxDataLen];
        nint cur    = 0;
        nint pwdLen = 0;
        try
        {
            do
            {
                sponge.DoRandomPermutationForBytes(len, passwordArray, countOfStepsForPermitations, regime);
                DoShowPasswordTable();

                var c1 = ReadKey();
                if (c1 == -1)
                    break;

                if (c1 == -3)
                {
                    ClearPassword(sponge, ref cur, ref pwdLen);
                    continue;
                }

                if (c1 < 0)
                {
                    DisplayErrorMessage(sponge, doErrorMessage);
                    continue;
                }

                Clear();
                var c2 = ReadKey();
                if (c2 == -1)
                    break;

                if (c2 == -3)
                {
                    ClearPassword(sponge, ref cur, ref pwdLen);
                    continue;
                }

                if (c1 < 0 || c2 < 0)
                {
                    DisplayErrorMessage(sponge, doErrorMessage);
                    continue;
                }

                // Console.WriteLine(c1);
                // Console.WriteLine(c2);
                Console.WriteLine((char) passwordArray[c2 + c1*x]);
                passwd[cur] = passwordArray[c2 + c1*x];
                cur++;
                pwdLen++;
                if (cur >= sponge.maxDataLen)
                {
                    sponge.Step(ArmoringSteps: ArmoringSteps, regime: regime, data: passwd, dataLen: cur);

                    vkf.input!.Add(passwd, cur);
                    while (vkf.input.Count > 0)
                        vkf.DoStepAndIO(regime: regime);

                    cur = 0;
                }
            }
            while (true);

            if (cur > 0)
            {
                sponge.Step(ArmoringSteps: ArmoringSteps, regime: regime, data: passwd, dataLen: cur);

                vkf.input!.Add(passwd, cur);
                while (vkf.input.Count > 0)
                    vkf.DoStepAndIO(regime: regime);
            }

            // Console.WriteLine("\x1b[0m\x1b[0m");
            Console.ResetColor();
            Clear();
            Console.Clear();

            if (pwdLen < 8)
            {
                Console.WriteLine(L("Password length is too small") + $": {pwdLen} < 8");
                throw new Exception("PasswordEnter: " + L("Password length is too small") + $": {pwdLen} < 8");
            }
        }
        finally
        {
            Dispose();
        }

        void ClearPassword(CascadeSponge_mt_20230930 sponge, ref nint cur, ref nint pwdLen)
        {
            Clear();
            if (pwdLen > cur)
            {
                Console.Clear();
                throw new Exception(L("The password cannot be reset. Too many characters have been entered. It is possible to reset the entered password only if the characters are less than") + $" {sponge.maxDataLen}");
            }

            cur    = 0;
            pwdLen = 0;

            Console.SetCursorPosition(0, 0);
            Console.WriteLine(L("The entered password characters have been reset. Start entering the password again.") + " " + L("Press Enter to continue"));
            Console.ReadLine();
        }

        void DisplayErrorMessage(CascadeSponge_mt_20230930 sponge, bool doErrorMessage)
        {
            if (doErrorMessage)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Clear("!");
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(L("HELP MESSAGE FOR PasswordEnter") + $" {sponge.maxDataLen}. " + L("Press Enter to continue"));
                Console.ReadLine();
            }
        }
    }

    public void Clear(string c = " ")
    {
        //for (int i = 0; i < stepsH[stepsH.Length-1]+1; i++)
        //for (int j = 0; j < stepsV[stepsV.Length-1]+1; j++)
        for (int i = 0; i < Console.LargestWindowWidth;  i++)
        for (int j = 0; j < Console.LargestWindowHeight; j++)
        {
            Console.SetCursorPosition(i, j);
            Console.Write(c);
        }
    }

    public int ReadKey()
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar)
            return -1;

        if (key.Key == ConsoleKey.Escape)
            return -3;

        int c1 = (int) key.Key;
        if (c1 >= 48 && c1 <= 57)
            c1 -= 48;
        else
        if (c1 >= 65 && c1 <= 70)
            c1 += -65 + 10;
        else
            return -2;

        return c1;
    }


    protected int[]    stepsH  = {4, 6, 8, 11, 13, 15, 18, 20, 22, 25, 27, 29, 32, 34, 36, 38};
    protected int[]    stepsV  = {1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 17, 18, 19, 20};
    protected string[] numbers = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"};
    public void DoShowPasswordTable()
    {
        // \x1b[1m\x1b[48;2;255;0;0m\x1b[38;2;0;0;255mTRUECOLOR\x1b[0m\x1b[0m
        Console.WriteLine("\x1b[1m\x1b[48;2;0;0;255m\x1b[38;2;255;0;0m");
        Clear();
        for (int i = 0; i < y; i++)
        {
            Console.SetCursorPosition(stepsH[i], 0);
            Console.Write(numbers[i]);
        }

        for (int i = 0; i < x; i++)
        {
            Console.SetCursorPosition(0, stepsV[i]);
            Console.Write(numbers[i]);
        }

        for (int i = 0; i < x; i++)
        for (int j = 0; j < y; j++)
        {
            Console.SetCursorPosition(stepsH[i], stepsV[j]);
            Console.Write((char) passwordArray.array[i + j*x]);
        }
    }

    void IDisposable.Dispose()
    {
        Dispose();
        GC.SuppressFinalize(this);
    }

    ~PasswordEnter()
    {
        Dispose(true);
    }

    public bool isDisposed = false;
    public virtual void Dispose(bool fromDestructor = false)
    {
        if (isDisposed)
            return;

        TryToDispose(passwordArray);
        isDisposed = true;

        if (fromDestructor)
            BytesBuilderForPointers.Record.ErrorsInDispose = true;
    }
}
