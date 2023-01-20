# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1
testTags=$2

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+mandatory +inWork <800 ?"
fi


./build/builder/builder "$configuration" "$testTags"
