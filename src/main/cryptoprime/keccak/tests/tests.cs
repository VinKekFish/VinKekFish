#define CAN_CREATEFILE_FOR_KECCAK

namespace cryptoprime_tests;

using System.Collections.Generic;
using cryptoprime;
using DriverForTestsLib;

/// <summary>Общий класс для задач-наследников AutoSaveTestTask</summary>
public class ParentAutoSaveTask: AutoSaveTestTask
{
    public ParentAutoSaveTask(TaskResultSaver executer_and_saver, TestConstructor constructor, bool canCreateFile = false):
                    base
                    (
                        name:               "",
                        dirForFiles:        getDirectoryPath(),
                        executer_and_saver: executer_and_saver,
                        constructor:        constructor
                    )
    {
        this.executer_and_saver.canCreateFile = canCreateFile;

        this.Name   = this.GetType().FullName ?? throw new System.ArgumentNullException();
        dirForFiles = setDirForFiles();
    }

    public static DirectoryInfo getDirectoryPath(string ProjectDir = "", string DirName = "autotests")
    {
        var pathToFile = new DirectoryInfo(System.AppContext.BaseDirectory)?.Parent ?? throw new Exception();
        var dir        = new DirectoryInfo(Path.Combine(pathToFile.FullName, ProjectDir));
        if (dir == null)
            throw new Exception();

        return new DirectoryInfo(  Path.Combine(dir.FullName, DirName)  );
    }

    public virtual DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath();
    }
}

[TestTagAttribute("fast")]
[TestTagAttribute("mandatory")]
public class Keccak_sha512_test: ParentAutoSaveTask
{
    #if CAN_CREATEFILE_FOR_KECCAK
    public static readonly bool canCreateFile = true;
    #warning CAN_CREATEFILE_FOR_KECCAK
    #else
    public static readonly bool canCreateFile = false;
    #endif

    public Keccak_sha512_test(TestConstructor constructor): base
    (
        executer_and_saver: new Saver(),
        constructor:        constructor,
        canCreateFile:      canCreateFile
    )
    {
        // Console.WriteLine("Keccak_sha512_test test task created");
    }

    public override DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath("src/main/cryptoprime/keccak/tests/");
    }

    protected class Saver: TaskResultSaver
    {
        public Saver()
        {
        }

        public override object ExecuteTest(AutoSaveTestTask task)
        {
            // Console.WriteLine("Keccak_sha512_test start executing");

            // KeccakPrime.Keccak_Input_512(

            // return "";

            return new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
        }
    }
}
