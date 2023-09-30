// TODO: tests
namespace vinkekfish;

using cryptoprime;
using maincrypto.keccak;
using static cryptoprime.BytesBuilderForPointers;
using System.Threading.Tasks;

using static CascadeSponge_1t_20230905.InputRegime;
using System.Text;

// code::docs:rQN6ZzeeepyOpOnTPKAT:
// ::cp:alg:a7L6XjXsuwWGVxwJSN1x.utils:20230930

/// <summary>
/// Это однопоточный эталон для тестирования каскадной губки
/// </summary>
public unsafe partial class CascadeSponge_1t_20230905: IDisposable
{
                                                                    /// <summary>Проверяет, что объект не удалён. Если объект удалён (isDisposed==true), вызывает ObjectDisposedException</summary><param name="message">Сообщение для исключения ObjectDisposedException</param><param name="objectName">Имя объекта для исключения ObjectDisposedException</param>
    public void ObjectDisposedCheck(string message, string objectName = "CascadeSponge_1t")
    {
        if (isDisposed)
            throw new ObjectDisposedException(objectName, message);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"wide = {wide}");
        sb.AppendLine($"tall = {tall}");
        sb.AppendLine($"W    = {W}");
        sb.AppendLine($"Wn   = {Wn}");
        sb.AppendLine($"maxDataLen                 = {this.maxDataLen}");
        sb.AppendLine($"countOfThreeFish_RC        = {this.countOfThreeFish_RC}");
        sb.AppendLine($"countStepsForKeyGeneration = {this.countStepsForKeyGeneration}");
        sb.AppendLine($"strenghtInBytes            = {this.strenghtInBytes} ({this.strenghtInBytes*8} bits)");
        sb.AppendLine($"countOfProcessedSteps      = {this.countOfProcessedSteps}");

        return sb.ToString();
    }
}
