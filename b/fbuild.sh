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
