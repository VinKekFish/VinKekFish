# Файл нужно запускать из корневой директории репозитория VinKekFish
# Можно запускать без параметров
# Пример запуска с параметрами:
# ./b/fbuild.sh Release 'inWork'
configuration=$2
testTags=$1
flags=$3

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+Mandatory +inWork <10 ?"
fi



./build/builder/builder "$configuration" "$testTags" "$flags"

if [ $? -ne 0 ]
then
    echo "Ошибка: команда завершилась с кодом $?"
    exit 1
fi

# dotnet ./build/SecureCompare.dll

function CopyBuild()
{
    arcDir=$1
    build=$2
    top=`dirname $1`
    rm -rf $top
    mkdir -p  $arcDir
    cp -fvur $build/locales       $arcDir/locales
    cp -fvu  $build/vkf           $arcDir
    cp -fvu  $build/vkf           $arcDir
    cp -fvu  ./build/*.options     $arcDir
    cp -fvu  ./build/*.service     $arcDir
    cp -fvu  ./build/*.sh          $arcDir
    chmod a+x $arcDir/*.sh
}

CopyBuild './build/arcs/dotnet7/exe' './build'
rm -f ./build/arcs/vkf-dotnet7.7z
7z a -y -t7z -m0=lzma2 -mx=9 -bb0 -bd -ssc -ssw ./build/arcs/vkf-dotnet7.7z './build/arcs/dotnet7/exe'  > /dev/null

CopyBuild './build/arcs/linux/exe' './build.manual'
rm -f ./build/arcs/vkf-linux.7z
7z a -y -t7z -m0=lzma2 -mx=9 -bb0 -bd -ssc -ssw ./build/arcs/vkf-linux.7z './build/arcs/linux/exe'  > /dev/null

makeself --zstd --complevel 19 './build/arcs/dotnet7' ./build/arcs/vkf-dotnet7.sh 'vkf - VinKekFish for dotnet7' './exe/install.sh'
makeself --zstd --complevel 19 './build/arcs/linux' ./build/arcs/vkf-linux.sh 'vkf - VinKekFish with dotnet for linux' './exe/install.sh'

