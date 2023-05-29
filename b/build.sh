# Файл нужно запускать из корневой директории репозитория VinKekFish (Crypto/VinKekFish)
configuration=$1
testTags=$2

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+mandatory +inWork <50 ?"
fi

rm -f tests-*.log

rootDir=`pwd`

mkdir -p ./build/builder

dotnet publish src/builder/builder/ --output ./build/builder -c $configuration --self-contained false --use-current-runtime true /p:PublishSingleFile=false

bash ./b/fbuild.sh "$configuration" "$testTags" "restore"
