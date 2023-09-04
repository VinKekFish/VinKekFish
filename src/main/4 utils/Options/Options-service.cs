// TODO: tests
using System.Text;
// TODO: сделать опции для сервиса здесь и переписать получение опций в сервисе через этот класс
namespace VinKekFish_Utils.Options;

public class Options_Service
{
    public readonly Options options;
    public Options_Service(Options options)
    {
        this.options = options;
        Analize();
    }

    protected virtual void Analize()
    {
        
    }

    public override string ToString()
    {
        return options.ToString();
    }
}
