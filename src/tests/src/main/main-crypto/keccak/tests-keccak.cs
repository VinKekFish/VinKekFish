#define CAN_CREATEFILE_FOR_keccak

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

    public override DirectoryInfo setDirForFiles()
    {
        return getDirectoryPath("src/tests/src/main/main-crypto/keccak/");
    }

    protected SaverParent parentSaver;
    protected abstract class SaverParent: TaskResultSaver
    {}
}
