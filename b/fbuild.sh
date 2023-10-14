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
    testTags="+mandatory +inWork <10 ?"
fi



./build/builder/builder "$configuration" "$testTags" "$flags"

# dotnet ./build/SecureCompare.dll

arcDir=./build/arcs/dotnet7/exe
rm -rf ./build/arcs/dotnet7/
mkdir -p  $arcDir
cp -fvur ./build/locales       $arcDir/locales
cp -fvu  ./build/vkf           $arcDir
cp -fvu  ./build/vkf           $arcDir
cp -fvu  ./build/*.options     $arcDir
cp -fvu  ./build/*.service     $arcDir
cp -fvu  ./build/*.sh          $arcDir

rm -f ./build/arcs/vkf-dotnet7.7z
7z a -y -t7z -m0=lzma2 -mx=9 -bb0 -bd -ssc -ssw ./build/arcs/vkf-dotnet7.7z "$arcDir"  > /dev/null


