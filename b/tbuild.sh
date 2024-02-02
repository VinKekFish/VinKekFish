# Файл нужно запускать из корневой директории репозитория VinKekFish
# Больше тестов, чем build.sh
configuration=$2
testTags=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+Mandatory +inWork <2000 ?"
fi

# Вызов билда
bash ./b/fbuild.sh "$testTags" "$configuration"
