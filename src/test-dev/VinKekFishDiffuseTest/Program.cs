using vinkekfish;

namespace VinKekFishDiffuseTest;

// Ничего само по себе не делает. Нужно скомпилировать TreeFish с установленным флагом DEBUG_OUTPUT_PRETRANSFORMATION
class Program
{
    static void Main(string[] args)
    {
        var vkf = new VinKekFishBase_KN_20210525();
        vkf.Init1();
        vkf.Init2();
    }
}
