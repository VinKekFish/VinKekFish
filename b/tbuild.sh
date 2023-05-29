# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$1
testTags=$2

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+mandatory +inWork <2000 ?"
fi

# Вызов билда
bash ./b/fbuild.sh "$configuration" "$testTags"
