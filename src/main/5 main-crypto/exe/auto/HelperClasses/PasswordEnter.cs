// TODO: tests
using System.Runtime;

namespace VinKekFish_EXE;

using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using cryptoprime;
using maincrypto.keccak;
using vinkekfish;
using VinKekFish_Utils.ProgramOptions;
using static cryptoprime.BytesBuilderForPointers;
using static vinkekfish.CascadeSponge_1t_20230905;
using static VinKekFish_EXE.AutoCrypt.Command;
using static VinKekFish_Utils.Language;
using static VinKekFish_Utils.Utils;

public unsafe partial class PasswordEnter: IDisposable
{
    // [{( - скобки убираем, проще запоминать просто слово "скобка"; остаётся только квадратная скобка
    // < - меньше и больше легко перепутать, оставляем только один из них
    // ! слишком похож на черту и имеет сложное наименование
    // Ч - похоже на 4. ± - сложное наименование, к тому же, похожее на два занака "+-", идущие подряд
    // "qwertyuiopasdfghjkLzxcvbnm1234567890,.<?;':(+-*/=|&^%$#@ΣΔΨλШЫЭЯ";
    /// <summary>Разрешённые символы для стандартного пароля</summary>
    public static readonly string GrantedSymbols      = "qwertyuiopasdfghjkLzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890[+*/=&^%$#@<";
    public static readonly string GrantedSymbolsLower = "qwertyuiopasdfghjkLzxcvbnm1234567890[+*/=&^%$#@<";

    protected CascadeSponge_mt_20230930  sponge;
    protected VinKekFishBase_KN_20210525 vkf;
    protected CascadeSponge_mt_20230930  spongeForTable;

    protected Record passwordArray;
    /// <summary>x - вертикальная координата, как при индексировании матриц.</summary>
    public readonly int x = 0, y = 0;
    public readonly byte regime = 0;

    public PasswordEnter(CascadeSponge_mt_20230930 sponge, VinKekFishBase_KN_20210525 vkf, byte regime, nint countOfStepsForPermitations = 0, nint ArmoringSteps = 0, bool doErrorMessage = false, bool showNumber = true, bool SimpleKeyboard = true)
    {
        x = numbersH.Length;
        y = numbersV.Length;
        /*
        Console.WriteLine(L("Enter password") + ". " + L("First, enter the vertical character number (row number), then enter the horizontal character number (column number)") + ".\n" + L("Press 'Enter' to begin enter password") + ".");
        Console.ReadLine();
        */

        // На всякий случай переоткрываем поток ввода, т.к. он может быть перенаправлен с помощью SetIn для получения конфигурации
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
        Console.Clear();

        this.sponge = sponge;
        this.vkf = vkf;
        this.regime = regime;
        InitSpongeForTable();

        if (this.vkf.input is null || this.vkf.input.size < this.sponge.maxDataLen)
            throw new ArgumentOutOfRangeException("PasswordEnter: this.vkf.input.size < this.sponge.maxDataLen");

        var len = x * y * 2;
        if (len % GrantedSymbols.Length != 0)
        {
            len += GrantedSymbols.Length - (len % GrantedSymbols.Length);
        }

        passwordArray = Keccak_abstract.allocator.AllocMemory(len);
        for (int i = 0; i < len; i++)
            passwordArray[i] = (byte)GrantedSymbols[i % GrantedSymbols.Length];

        const int maxPasswordLen = 512;
        var  passwd = stackalloc byte[maxPasswordLen];
        nint cur = 0;
        nint pwdLen = 0;
        try
        {
            do
            {
                spongeForTable!.DoRandomPermutationForBytes(len, passwordArray, countOfStepsForPermitations, regime);
                DoShowPasswordTable();
                Console.SetCursorPosition(0, stepsV[y-1] + 2);
                if (showNumber)
                Console.WriteLine(cur);

                var c1 = ReadKey(1);
                if (c1 == -4)
                    continue;

                if (c1 == -1)
                    break;

                if (c1 == -3)
                {
                    ClearPassword(spongeForTable, ref cur, ref pwdLen);
                    continue;
                }

                if (c1 < 0)
                {
                    DisplayErrorMessage(spongeForTable, doErrorMessage);
                    continue;
                }

                Clear();
                var c2 = ReadKey(2);
                if (c2 == -4)
                    continue;

                if (c2 == -1)
                    break;

                if (c2 == -3)
                {
                    ClearPassword(spongeForTable, ref cur, ref pwdLen);
                    continue;
                }

                if (c1 < 0 || c2 < 0)
                {
                    DisplayErrorMessage(spongeForTable, doErrorMessage);
                    continue;
                }

                // Console.WriteLine(c1);
                // Console.WriteLine(c2);
                // Console.WriteLine((char)passwordArray[c2 + c1 * x]);
                passwd[cur] = passwordArray[c2 + c1 * x];
                cur++;
                pwdLen++;
            }
            while (cur < maxPasswordLen);

            Console.WriteLine("\x1b[0m\x1b[0m");
            Console.ResetColor();
            Clear();
            Console.Clear();

            if (cur > 0)
            {
                sponge.Step(ArmoringSteps: ArmoringSteps, regime: regime, data: passwd, dataLen: cur);

                vkf.input!.Add(passwd, cur);
                vkf.DoStepAndFullInput(regime: regime);

                cur = 0;
                BytesBuilder.ToNull(maxPasswordLen, passwd);
            }

            if (SimpleKeyboard)
            {
                start:
                Console.WriteLine(L("Enter password continuation (simple from keyboard)") + ":");
                while (cur < maxPasswordLen)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                        break;
                    if (key.Key == ConsoleKey.Escape)
                    {
                        ClearPassword(spongeForTable, ref cur, ref cur);
                        BytesBuilder.ToNull(maxPasswordLen, passwd);
                        goto start;
                    }

                    var ku      = (ushort) key.KeyChar;
                    passwd[cur] = (byte) ku;
                    cur++;
                    pwdLen++;

                    passwd[cur] = (byte) (ku >> 8);
                    cur++;
                    pwdLen++;
                }
            }

            if (cur > 0)
            {
                sponge.Step(ArmoringSteps: ArmoringSteps, regime: regime, data: passwd, dataLen: cur);

                vkf.input!.Add(passwd, cur);
                vkf.DoStepAndFullInput(regime: regime);

                BytesBuilder.ToNull(maxPasswordLen, passwd);
            }

            if (pwdLen < 6)
            {
                Console.WriteLine(L("Password length is too small") + $": {pwdLen} < 6");
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

            cur = 0;
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
                Console.WriteLine(L("HELP MESSAGE FOR PasswordEnter") + $" {maxPasswordLen/2}. " + L("Press Enter to continue"));
                Console.ReadLine();
            }
        }
    }

    private void InitSpongeForTable()
    {
        spongeForTable = new CascadeSponge_mt_20230930(sponge.strenghtInBytes) { StepTypeForAbsorption = TypeForShortStepForAbsorption.weak };
        spongeForTable.InitEmptyThreeFish((ulong) DateTime.Now.Ticks);

        nint countBytesForInit = sponge.strenghtInBytes;
        while (countBytesForInit > 0)
        {
            sponge.Step(regime: regime);
            spongeForTable.Step(data: sponge.lastOutput, dataLen: sponge.lastOutput.len);
            countBytesForInit -= sponge.lastOutput.len;
        }
        sponge.Step(regime: regime);
        spongeForTable.InitThreeFishByCascade(stepToKeyConst: 1, doCheckSafty: false);

        countBytesForInit = sponge.strenghtInBytes;
        while (countBytesForInit > 0)
        {
            sponge.Step(regime: regime);
            spongeForTable.Step(data: sponge.lastOutput, dataLen: sponge.lastOutput.len);
            countBytesForInit -= sponge.lastOutput.len;
        }
        sponge.Step(regime: regime);
        spongeForTable.InitThreeFishByCascade(stepToKeyConst: 1, doCheckSafty: false);
    }

    public static void Clear(string c = " ")
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

    /// <summary>Прочитать клавишу с клавиатуры (при вводе пароля)</summary>
    /// <returns></returns>
    public int ReadKey(int step)
    {
        if (step == 1)
            step = numbersV.Length;
        else
            step = numbersH.Length;

        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
            return -1;
        if (key.Key == ConsoleKey.Spacebar)
            return -4;

        if (key.Key == ConsoleKey.Escape)
            return -3;

        int c1 = (int) key.Key;
        if (c1 >= 48 && c1 <= 57)
            c1 -= 48;
        else
        if (c1 >= 65 && c1 <= 65 - 11 + step)
            c1 += -65 + 10;
        else
            return -2;

        return c1;
    }


    protected int[]    stepsH   = {4, 6, 8, 11, 13, 15, 18, 20, 22, 25, 27, 29, 32, 34, 36, 38};
    protected int[]    stepsV   = {1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 14, 15, 17, 18, 19, 21, 22, 23};
    protected string[] numbersH = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"};
    protected string[] numbersV = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H"};
    public void DoShowPasswordTable()
    {
        // \x1b[1m\x1b[48;2;255;0;0m\x1b[38;2;0;0;255mTRUECOLOR\x1b[0m\x1b[0m
        Console.WriteLine("\x1b[1m\x1b[48;2;0;0;255m\x1b[38;2;255;0;0m");
        Clear();
        for (int i = 0; i < x; i++)
        {
            Console.SetCursorPosition(stepsH[i], 0);
            Console.Write(numbersH[i]);
        }

        for (int i = 0; i < y; i++)
        {
            Console.SetCursorPosition(0, stepsV[i]);
            Console.Write(numbersV[i]);
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
        TryToDispose(spongeForTable);
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
