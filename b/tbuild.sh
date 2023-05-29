# Файл нужно запускать из корневой директории репозитория VinKekFish
configuration=$2
testTags=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+mandatory +inWork <2000 ?"
fi

# Вызов билда
bash ./b/fbuild.sh "$testTags" "$configuration"
