# Файл нужно запускать из корневой директории репозитория VinKekFish (Crypto/VinKekFish)
configuration=$2
testTags=$1

if [ -z "$configuration" ]
then
    configuration="Release"
fi

if [ -z "$testTags" ]
then
    testTags="+Mandatory +inWork <50 ?"
fi

rm -f tests-*.log

rootDir=`pwd`

mkdir -p ./build/builder

dotnet publish src/builder/builder/ --output ./build/builder -c $configuration --self-contained false --use-current-runtime true /p:PublishSingleFile=false

bash ./b/fbuild.sh "$testTags" "$configuration" "restore"

